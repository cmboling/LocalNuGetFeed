using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Common;
using LocalNugetFeed.Core.ConfigurationOptions;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Models;
using LocalNugetFeed.Core.Services;
using NuGet.Packaging;
using Xunit;

namespace LocalNuGetFeed.Core.Tests
{
	public class PackageFileStorageServiceTest
	{
		private readonly PackageFileStorageService _packageFileStorageService;

		public PackageFileStorageServiceTest()
		{
			var storageOptions = new PackagesFileStorageOptions() {Path = PackagesFileHelper.GetPackagesFolderPath(null)};
			_packageFileStorageService = new PackageFileStorageService(storageOptions);
		}

		[Fact]
		public async Task Read_ReturnsGetOSVersionPackage()
		{
			//setup
			TestPackageHelper.CleanPackagesDefaultDirectory(PackagesFileHelper.GetDefaultPackagesFolderFullPath());
			
			// Act
			var result = _packageFileStorageService.Read();

			// Assert 1
			Assert.True(result.Success);
			Assert.False(result.Data.Any());
			
			// save package file 
			var savePackageResult = await SaveTestPackageFile();
			
			// Assert 2
			Assert.True(savePackageResult.Success);
			
			result = _packageFileStorageService.Read();
			
			Assert.True(result.Data.Any());
			Assert.Equal(result.Data.Single().Id, TestPackageHelper.TestPackageId, StringComparer.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task Save_ReturnsGetOSVersionPackage()
		{
			//setup
			TestPackageHelper.CleanPackagesDefaultDirectory(PackagesFileHelper.GetDefaultPackagesFolderFullPath());
			
			// Act
			var result = await SaveTestPackageFile();
			
			// Assert 
			Assert.True(result.Success);
			Assert.NotNull(result.Data);
			Assert.Equal(result.Data.Id, TestPackageHelper.TestPackageId, StringComparer.OrdinalIgnoreCase);
		}

		private async Task<ResponseModel<Package>> SaveTestPackageFile()
		{
			using (var stream = new MemoryStream(File.ReadAllBytes(TestPackageHelper.GetOSVersionPackageFilePath())))
			{
				using (var reader = new PackageArchiveReader(stream))
				{
					var saveResult = await _packageFileStorageService.Save(reader, stream);

					return saveResult;
				}
			}
		}
	}
}