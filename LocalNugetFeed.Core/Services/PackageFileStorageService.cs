using System.IO;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.ConfigurationOptions;
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

		public async Task<ResponseModel> SavePackageFile(NuspecReader packageNuspec, Stream sourceFileStream)
		{
			var packageId = packageNuspec.GetId();
			var packageVersion = packageNuspec.GetVersion();

			var packagePath = Path.Combine(_storageOptions.Path, packageId.ToLowerInvariant(), packageVersion.ToNormalizedString().ToLowerInvariant());
			Directory.CreateDirectory(packagePath);

			using (var destinationFileStream = File.Open(packagePath, FileMode.CreateNew))
			{
				sourceFileStream.Seek(0, SeekOrigin.Begin);

				await sourceFileStream.CopyToAsync(destinationFileStream);
			}

			return new ResponseModel(HttpStatusCode.OK);
		}
	}
}