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

		///<inheritdoc cref="IPackageService.Push"/>
		public async Task<Package> Push(NuspecReader nuspecReader, Stream packageFileStream)
		{
			// step 1. Save package physically on the drive	
			var savedPackage = await _storageService.Save(nuspecReader, packageFileStream);

			// step 2. add new package to Session
			_sessionService.Set(savedPackage);

			return savedPackage;
		}

		///<inheritdoc cref="IPackageService.GetPackages"/>	
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
				_sessionService.SetRange(packages);
			}

			return onlyLastVersion ? GetDistinctPackages(packages) : packages;
		}

		///<inheritdoc cref="IPackageService.GetPackageVersions"/>	
		public async Task<IReadOnlyCollection<Package>> GetPackageVersions(string id)
		{
			var packages = new HashSet<Package>(await GetPackages());

			packages.RemoveWhere(x => !id.Equals(x.Id, StringComparison.OrdinalIgnoreCase));

			return packages;
		}

		///<inheritdoc cref="IPackageService.Search"/>	
		public async Task<IReadOnlyCollection<Package>> Search(string query)
		{
			var packages = new HashSet<Package>(
				await GetPackages()); // we can pass here the true value for `onlyLastVersion` boolean flag, but it will decrease search performance in this case 

			packages.RemoveWhere(x =>
				!x.Id.Contains(query, StringComparison.OrdinalIgnoreCase) && !x.Description.Contains(query, StringComparison.OrdinalIgnoreCase));

			return GetDistinctPackages(packages);
		}

		///<inheritdoc cref="IPackageService.PackageExists"/>	
		public async Task<bool> PackageExists(string id, string version)
		{
			var packages = new HashSet<Package>(await GetPackages(), new PackageExistsComparer());

			return packages.Count != 0 && packages.Contains(new Package() {Id = id, Version = version});
		}

		/// <summary>
		/// Sorts packages by highest version and removes all other package versions which are precedes last version
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