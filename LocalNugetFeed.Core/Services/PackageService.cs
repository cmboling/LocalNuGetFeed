using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;
using NuGet.Packaging;

namespace LocalNugetFeed.Core.Services
{
	public class PackageService : IPackageService
	{
		private readonly IPackageFileStorageService _storageService;
		private readonly IPackageDatabaseService _databaseService;

		public PackageService(IPackageFileStorageService storageService, IPackageDatabaseService databaseService)
		{
			_storageService = storageService;
			_databaseService = databaseService;
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

			using (var sourceFileStream = packageFile.OpenReadStream())
			{
				using (var reader = new PackageArchiveReader(sourceFileStream))
				{
					var packageNuspec = reader.NuspecReader;

					// step 1. we should make sure that package doesn't exists in local feed
					var package = await Find(packageNuspec.GetId(), PackageVersion(packageNuspec));
					if (package != null)
					{
						return new ResponseModel(HttpStatusCode.BadRequest, "Package already exists");
					}

					// step 2. Save package locally to the feed			
					var savePackageToFileResult = await _storageService.SavePackageFile(packageNuspec, sourceFileStream);

					if (!savePackageToFileResult.Success)
					{
						return new ResponseModel(savePackageToFileResult.StatusCode, savePackageToFileResult.Message);
					}

					package = new Package()
					{
						// TODO
					};

					// step 3. now need index the package info in local database for further search by packages in db
					var savePackageToDbResult = await _databaseService.Save(package);

					if (!savePackageToDbResult.Success)
					{
						return new ResponseModel(savePackageToDbResult.StatusCode, savePackageToDbResult.Message);
					}

					return new ResponseModel(HttpStatusCode.OK);
				}
			}

			string PackageVersion(NuspecReader packageNuspec)
			{
				return packageNuspec.GetVersion().ToNormalizedString();
			}
		}

		/// <summary>
		/// Find package in local feed
		/// </summary>
		/// <param name="id">package id</param>
		/// <param name="version">package version</param>
		/// <returns>response with result</returns>		
		public Task<Package> Find(string id, string version)
		{
			throw new System.NotImplementedException();
		}
	}
}