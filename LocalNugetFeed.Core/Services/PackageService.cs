using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using NuGet.Packaging;
using NuGet.Versioning;

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
		/// Get packages from local feed
		/// </summary>
		/// <param name="onlyLastVersion">Boolean flag which is determines - return all versions of an each package or only last</param>
		/// <returns>packages</returns>
		public async Task<IReadOnlyList<Package>> GetPackages(bool onlyLastVersion = false)
		{
			// step 1. try to get packages from session first
			var sessionFeedPackages = _sessionService.Get();
			if (sessionFeedPackages != null && sessionFeedPackages.Any())
			{
				return onlyLastVersion ? GetDistinctPackages(sessionFeedPackages) : sessionFeedPackages;
			}

			// step 2. if we're here, it means that session is empty and now we should to load packages from file system and populate session by data
			var packages = await Task.FromResult(_storageService.Read());
			if (packages.Any())
			{
				//update packages in session storage
				_sessionService.Set(packages);
			}

			return onlyLastVersion ? GetDistinctPackages(packages) : packages;
		}

		/// <summary>
		/// Get package versions
		/// </summary>
		/// <returns>packages</returns>
		public async Task<IReadOnlyCollection<Package>> GetPackageVersions(string id)
		{
			var packages = new HashSet<Package>(await GetPackages());
			
			packages.RemoveWhere(x => !x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

			return packages;
		}

		/// <summary>
		/// Search packages by query
		/// </summary>
		/// <param name="query">search query (required)</param>
		/// <returns>packages</returns>		
		public async Task<IReadOnlyCollection<Package>> Search(string query)
		{
			var packages = new HashSet<Package>(await GetPackages());
			
			packages.RemoveWhere(x => !x.Id.Contains(query, StringComparison.OrdinalIgnoreCase) && !x.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
			
			return GetDistinctPackages(packages);
		}

		/// <summary>
		/// Checks that package exists or not
		/// </summary>
		/// <param name="id">package id</param>
		/// <param name="version">package version</param>
		/// <returns>true/false</returns>		
		public async Task<bool> PackageExists(string id, string version)
		{
			var packages = new HashSet<Package>(await GetPackages(), new PackageComparer());

			return packages.Count != 0 && packages.Contains(new Package() {Id = id, Version = version});
		}
		
		/// <summary>
		/// Removes package duplicates from list
		/// </summary>
		/// <param name="allPackages">packages</param>
		/// <returns>distinct packages</returns>
		private IReadOnlyList<Package> GetDistinctPackages(IEnumerable<Package> allPackages)
		{
			return allPackages.OrderByDescending(s => new NuGetVersion(s.Version))
				.GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase)
				.Select(z => z.First()).ToList();
		}
	}
}