using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using NuGet.Packaging;

namespace LocalNugetFeed.Core.Services
{
	public class PackageService : IPackageService
	{
		private readonly IPackageFileStorageService _storageService;
		private readonly IPackageSessionService _sessionService;

		public PackageService(IPackageFileStorageService storageService, IPackageSessionService sessionService)
		{
			_storageService = storageService;
			_sessionService = sessionService;
		}

		/// <summary>
		/// Pushes package to the local feed
		/// </summary>
		/// <param name="nuspecReader">nuspec reader</param>
		/// <param name="packageFileStream">package file stream</param>
		/// <returns>Package</returns>
		public async Task<Package> Push(NuspecReader nuspecReader, Stream packageFileStream)
		{
			// step 1. Save package physically on the drive	
			var savedPackage = await _storageService.Save(nuspecReader, packageFileStream);

			// step 2. add new package to Session
			_sessionService.Set(savedPackage);

			return savedPackage;
		}

		/// <summary>
		/// Get packages from session or file system (if session is empty)
		/// </summary>
		/// <returns>packages</returns>
		public async Task<IReadOnlyList<Package>> GetPackages()
		{
			// step 1. try to get packages from session first
			var sessionFeedPackages = _sessionService.Get();
			if (sessionFeedPackages != null && sessionFeedPackages.Any())
			{
				return sessionFeedPackages;
			}

			// step 2. if we're here, it means that session is empty and now we should to load packages from file system
			var packages = await Task.FromResult(_storageService.Read());
			if (packages.Any())
			{
				//update packages in session storage
				_sessionService.Set(packages);
			}

			return packages;
		}
	}
}