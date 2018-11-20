using System;
using System.IO;
using System.Linq;
using System.Net;
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
		public void Read_ThrowException_DirectoryIsNotSet()
		{
			//setup
			TestPackageHelper.CleanPackagesDefaultDirectory(PackagesFileHelper.GetDefaultPackagesFolderFullPath());
			var storageOptions = new PackagesFileStorageOptions();
			var packageFileStorageService = new PackageFileStorageService(storageOptions);
			
			// act + assert
			Assert.Throws<DirectoryNotFoundException>(() =>
				packageFileStorageService.Read());
		}
		
		[Theory]
		[InlineData("")]
		[InlineData("D:\\Packages")]
		public void GetPackagesFolderPath_ReturnsPathAccordingWithInlineData(string path)
		{
			// Act
			var folderPath = PackagesFileHelper.GetPackagesFolderPath(path);

			// Assert
			if (!string.IsNullOrWhiteSpace(path))
			{
				Assert.Equal(path, folderPath);
			}
			else
			{
				Assert.Equal(folderPath, PackagesFileHelper.GetDefaultPackagesFolderFullPath());
			}
		}

		[Fact]
		public async Task Read_ReturnsGetOSVersionPackage()
		{
			//setup
			TestPackageHelper.CleanPackagesDefaultDirectory(PackagesFileHelper.GetDefaultPackagesFolderFullPath());

			// Act
			var packages = _packageFileStorageService.Read();

			// Assert 1
			Assert.False(packages.Any());

			// save package file 
			var newPackage = await SaveTestPackageFile();

			// Assert 2
			Assert.NotNull(newPackage);

			packages = _packageFileStorageService.Read();

			Assert.True(packages.Any());
			Assert.Equal(packages.Single().Id, TestPackageHelper.TestPackageId, StringComparer.OrdinalIgnoreCase);
			Assert.NotNull(packages.Single().PackageDependencies);
			Assert.True(packages.Single().PackageDependencies.Any());
		}

		[Fact]
		public async Task Save_ReturnsGetOSVersionPackage()
		{
			//setup
			TestPackageHelper.CleanPackagesDefaultDirectory(PackagesFileHelper.GetDefaultPackagesFolderFullPath());

			// Act
			var newPackage = await SaveTestPackageFile();

			// Assert 
			Assert.NotNull(newPackage);
			Assert.Equal(newPackage.Id, TestPackageHelper.TestPackageId, StringComparer.OrdinalIgnoreCase);
		}

		private async Task<Package> SaveTestPackageFile()
		{
			using (var stream = new MemoryStream(File.ReadAllBytes(TestPackageHelper.GetOSVersionPackageFilePath())))
			{
				using (var reader = new PackageArchiveReader(stream))
				{
					var saveResult = await _packageFileStorageService.Save(reader.NuspecReader, stream);

					return saveResult;
				}
			}
		}
	}
}