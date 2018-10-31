using System;
using System.Collections.Generic;
using System.IO;
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
				return new ResponseModel(HttpStatusCode.BadRequest, "Package file not found");
			}

			try
			{
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
						var savePackageToFileResult = await _storageService.Save(reader, sourceFileStream);

						if (!savePackageToFileResult.Success)
						{
							return new ResponseModel(savePackageToFileResult.StatusCode, savePackageToFileResult.Message);
						}

						// add new package to Session
						_sessionService.Set(savePackageToFileResult.Data);

						return new ResponseModel(HttpStatusCode.OK);
					}
				}
			}
			catch (InvalidDataException e)
			{
				return new ResponseModel(HttpStatusCode.UnsupportedMediaType, "Invalid NuGet package file", e);
			}
			catch (Exception e)
			{
				return new ResponseModel(HttpStatusCode.InternalServerError, "Server error. Unable to push package file", e);
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
			var localFeedPackagesResult = await GetPackages();

			if (!localFeedPackagesResult.Success)
			{
				return new ResponseModel<Package>(localFeedPackagesResult.StatusCode, localFeedPackagesResult.Message);
			}

			var package = localFeedPackagesResult.Data.FirstOrDefault(x =>
				x.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
				x.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

			return package == null ? new ResponseModel<Package>(HttpStatusCode.NotFound) : new ResponseModel<Package>(HttpStatusCode.OK, package);
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
				return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.BadRequest, "Package id is undefined");
			}

			var localFeedPackagesResult = await GetPackages();

			if (!localFeedPackagesResult.Success)
			{
				return new ResponseModel<IReadOnlyList<Package>>(localFeedPackagesResult.StatusCode, localFeedPackagesResult.Message);
			}

			var packageVersions = localFeedPackagesResult.Data.Where(x =>
				x.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).ToList();

			if (!packageVersions.Any())
			{
				return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound, $"Package [{id}] not found");
			}

			return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, packageVersions);
		}


		/// <summary>
		/// Search packages by query in local feed (session/file system)
		/// </summary>
		/// <param name="query">search query (optional)</param>
		/// <returns>response with result</returns>		
		public async Task<ResponseModel<IReadOnlyList<Package>>> Search(string query = null)
		{
			// before we should check packages in session and use their if are exist there
			var localFeedPackagesResult = await GetPackages();

			if (!localFeedPackagesResult.Success)
			{
				return new ResponseModel<IReadOnlyList<Package>>(localFeedPackagesResult.StatusCode, localFeedPackagesResult.Message);
			}

			var searchResult = new List<Package>(localFeedPackagesResult.Data);
			if (!string.IsNullOrEmpty(query))
			{
				searchResult = searchResult.Where(x => x.Id.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
				if (!searchResult.Any())
				{
					return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound, "No any packages matching to your request");
				}
			}

			searchResult = searchResult.OrderByDescending(s => new NuGetVersion(s.Version))
				.GroupBy(g => g.Id)
				.Select(z => z.First()).ToList();

			return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, searchResult);
		}

		/// <summary>
		/// Get packages from session or file system
		/// </summary>
		/// <returns>response with result</returns>
		public async Task<ResponseModel<IReadOnlyList<Package>>> GetPackages()
		{
			var sessionFeedPackages = _sessionService.Get();

			if (sessionFeedPackages != null && sessionFeedPackages.Any())
			{
				return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, sessionFeedPackages);
			}

			ResponseModel<IReadOnlyList<Package>> filesReadResult;

			// otherwise we need to load packages from file system
			try
			{
				filesReadResult = await Task.FromResult(_storageService.Read());
			}
			catch (Exception)
			{
				return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.InternalServerError);
			}

			if (!filesReadResult.Success || filesReadResult.Data == null)
			{
				return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound, "Packages feed is empty");
			}

			//update packages in session storage
			_sessionService.Set(filesReadResult.Data);

			return new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, filesReadResult.Data);
		}
	}
}