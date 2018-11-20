using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Extensions;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;
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
		/// Push package to the local feed
		/// </summary>
		/// <param name="packageFile">nuget package</param>
		/// <returns>result of request</returns>
		public async Task<ResponseModel> Push(IFormFile packageFile)
		{
			if (packageFile == null)
			{
				throw new ArgumentNullException("Package file not found");
			}

			using (var sourceFileStream = packageFile.OpenReadStream())
			{
				using (var reader = new PackageArchiveReader(sourceFileStream))
				{
					var packageNuspec = reader.NuspecReader;

					// step 1. we should make sure that package doesn't exists in local feed
					var getPackageResult = await GetPackage(packageNuspec.PackageId(), packageNuspec.PackageVersion());
					if (getPackageResult.Success && getPackageResult.Data != null)
					{
						return new ResponseModel(HttpStatusCode.Conflict,
							$"Package {getPackageResult.Data.Id} v{getPackageResult.Data.Version} already exists in feed");
					}

					// step 2. Save package locally to the feed			
					var savedPackage = await _storageService.Save(packageNuspec, sourceFileStream);

					// add new package to Session
					_sessionService.Set(savedPackage);

					return new ResponseModel(HttpStatusCode.OK);
				}
			}
		}


		/// <summary>
		/// Get package by id and version
		/// </summary>
		/// <param name="id">package id</param>
		/// <param name="version">package version</param>
		/// <returns>response with result</returns>		
		public async Task<ResponseModel<Package>> GetPackage(string id, string version)
		{
			if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException("Package id and version are required");
			}
			
			var packages = await GetPackages();

			var package = packages.FirstOrDefault(x =>
				x.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
				x.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

			return package != null
				? new ResponseModel<Package>(package)
				: new ResponseModel<Package>(HttpStatusCode.NotFound, $"Package [{id}] not found");
		}

		/// <summary>
		/// Get package(s) by id
		/// </summary>
		/// <param name="id">package id</param>
		/// <returns>response with result</returns>		
		public async Task<ResponseModel<IReadOnlyList<Package>>> PackageVersions(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentNullException("Package id is required");
			}

			var packages = await GetPackages();

			var packageVersions = packages.Where(x =>
				x.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).OrderByDescending(z => new NuGetVersion(z.Version)).ToList();

			return packageVersions.Any()
				? new ResponseModel<IReadOnlyList<Package>>(packageVersions)
				: new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound, $"Package [{id}] not found");
		}

		/// <summary>
		/// Search packages by query in local feed (session/file system)
		/// </summary>
		/// <param name="query">search query (optional)</param>
		/// <returns>response with result</returns>		
		public async Task<ResponseModel<IReadOnlyList<Package>>> Search(string query = null)
		{
			var packages = await GetPackages();
			if (!string.IsNullOrEmpty(query))
			{
				packages = packages.Where(x =>
					x.Id.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
				if (!packages.Any())
				{
					return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound, "No any packages matching to your request");
				}
			}

			packages = packages.OrderByDescending(s => new NuGetVersion(s.Version))
				.GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase)
				.Select(z => z.First()).ToList();
			return new ResponseModel<IReadOnlyList<Package>>(packages);
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