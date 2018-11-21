using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LocalNugetFeed.Core.Common;
using LocalNugetFeed.Core.Configuration;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Services;
using Moq;
using NuGet.Packaging;
using Xunit;

namespace LocalNuGetFeed.Core.Tests
{
	public class PackageFileStorageServiceTest
	{
		private readonly PackageFileStorageService _packageFileStorageService;
		
		public PackageFileStorageServiceTest()
		{
			IMapper mapper = AutoMapperConfiguration.Configure().CreateMapper();
			var storageOptions = new PackagesFileStorageOptions() {Path = PackagesFileHelper.GetPackagesFolderPath(null)};
			_packageFileStorageService = new PackageFileStorageService(storageOptions, mapper);
		}

		[Fact]
		public void Read_ThrowException_DirectoryIsNotSet()
		{
			//setup
			TestPackageHelper.CleanPackagesDefaultDirectory(PackagesFileHelper.GetDefaultPackagesFolderFullPath());
			var storageOptions = new PackagesFileStorageOptions();
			var packageFileStorageService = new PackageFileStorageService(storageOptions, It.IsAny<IMapper>());
			
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
			Assert.Equal(packages.Single().Id, TestPackageHelper.GetOSVersionPackageId, StringComparer.OrdinalIgnoreCase);
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
			Assert.Equal(newPackage.Id, TestPackageHelper.GetOSVersionPackageId, StringComparer.OrdinalIgnoreCase);
		}
		
		[Fact]
		public async Task Save_ThrowsException_WhenStreamWasDisposedBefore()
		{
			//setup
			TestPackageHelper.CleanPackagesDefaultDirectory(PackagesFileHelper.GetDefaultPackagesFolderFullPath());

			// Act + assert
			using (var stream = new MemoryStream(File.ReadAllBytes(TestPackageHelper.GetOSVersionPackageFilePath())))
			{
				using (var reader = new PackageArchiveReader(stream))
				{
					stream.Dispose();
					await Assert.ThrowsAsync<ObjectDisposedException>(() => _packageFileStorageService.Save(reader.NuspecReader, stream));
				}
			}
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