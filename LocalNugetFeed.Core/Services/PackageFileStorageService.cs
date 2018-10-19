using System.IO;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.ConfigurationOptions;
using LocalNugetFeed.Core.Extensions;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace LocalNugetFeed.Core.Services
{
	public class PackageFileStorageService : IPackageFileStorageService
	{
		private readonly PackagesFileStorageOptions _storageOptions;

		public PackageFileStorageService(PackagesFileStorageOptions storageOptions)
		{
			_storageOptions = storageOptions;
		}

		/// <summary>
		/// Save nuget package and nuspec metadata on local hard drive to an according folder
		/// </summary>
		/// <param name="packageReader">package reader</param>
		/// <param name="packageFileStream">package file stream</param>
		/// <returns>response status info</returns>
		public async Task<ResponseModel> SavePackageFile(PackageArchiveReader packageReader, Stream packageFileStream)
		{
			var packageFolderPath = Path.Combine(_storageOptions.Path, packageReader.NuspecReader.PackageId(), packageReader.NuspecReader.PackageVersion());
			var fullPackagePath = Path.Combine(packageFolderPath, $"{packageReader.NuspecReader.PackageId()}.{packageReader.NuspecReader.PackageVersion()}");

			Directory.CreateDirectory(packageFolderPath);

			using (var destinationFileStream = File.Open($"{fullPackagePath}.nupkg", FileMode.CreateNew))
			{
				packageFileStream.Seek(0, SeekOrigin.Begin);

				await packageFileStream.CopyToAsync(destinationFileStream);
			}

			using (var nuspec = packageReader.GetNuspec())
			{
				using (var fileStream = File.Open($"{fullPackagePath}.nuspec", FileMode.CreateNew))
				{
					await nuspec.CopyToAsync(fileStream);
				}
			}

			return new ResponseModel(HttpStatusCode.OK);
		}
	}
}