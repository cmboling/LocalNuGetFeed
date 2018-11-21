using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using LocalNugetFeed.Core.BLL;
using LocalNugetFeed.Core.BLL.Interfaces;
using LocalNugetFeed.Core.Configuration;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using Moq;
using NuGet.Packaging;
using Xunit;

namespace LocalNuGetFeed.Core.Tests
{
	public class PackageManagerTest
	{
		private readonly Mock<IPackageService> _mockPackageService;
		private readonly IPackageManager _packageManager;
		private string _mockFilePath => TestPackageHelper.GetOSVersionPackageFilePath();
		private readonly Mock<IPackageSessionService> _mockPackageSessionService;
		private readonly Mock<IPackageFileStorageService> _mockPackageFileStorageService;

		public PackageManagerTest()
		{
			IMapper mapper = AutoMapperConfiguration.Configure().CreateMapper();
			_mockPackageService = new Mock<IPackageService>();
			_mockPackageFileStorageService = new Mock<IPackageFileStorageService>();
			_mockPackageSessionService = new Mock<IPackageSessionService>();
			_packageManager = new PackageManager(_mockPackageService.Object, mapper);
		}

		[Fact]
		public async Task Push_ReturnsSuccessfulResponse()
		{
			using (var stream = new MemoryStream(File.ReadAllBytes(_mockFilePath)))
			{
				stream.Seek(0, SeekOrigin.Begin);

				// setup
				_mockPackageService.Setup(s => s.Push(It.IsAny<NuspecReader>(), It.IsAny<Stream>()))
					.ReturnsAsync(TestPackageHelper.GetOSVersionPackage);
				_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(() => new List<Package>());

				// Act
				var result = await _packageManager.Push(stream);

				// Assert
				Assert.True(result.Success);
				Assert.NotNull(result.Data);
				Assert.Equal(result.Data.Id, TestPackageHelper.GetOSVersionPackageId, StringComparer.OrdinalIgnoreCase);
			}
		}


		[Fact]
		public async Task Push_ReturnsBadRequestResponse_WhenPackageIsAlreadyExists()
		{
			using (var stream = new MemoryStream(File.ReadAllBytes(_mockFilePath)))
			{
				stream.Seek(0, SeekOrigin.Begin);

				// setup
				_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(() => new[] {TestPackageHelper.GetOSVersionPackage()});

				// Act
				var result = await _packageManager.Push(stream);

				// Assert
				Assert.False(result.Success);
				Assert.True(result.StatusCode == HttpStatusCode.Conflict);
			}
		}

		[Fact]
		public async Task Push_ThrowsException_PackageFileIsIncorrect()
		{
			// setup
			var mockFile = TestPackageHelper.GetMockFile("some content", "wrongPackageFileExtension.txt");

			// act + assert
			await Assert.ThrowsAsync<InvalidDataException>(() =>
				_packageManager.Push(mockFile.OpenReadStream()));
		}


		[Fact]
		public async Task Search_ReturnsPackagesOrderedByVersionDesc_WhenQueryIsEmpty()
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(TestPackageHelper.TestPackages);

			// Act
			var result = await _packageManager.Search();

			// Assert
			Assert.True(result.Success);
			Assert.True(result.Data.Any());
			Assert.NotNull(result.Data.Single());
		}


		[Theory]
		[InlineData("", true)]
		[InlineData(TestPackageHelper.MyTestPackageId, true)]
		[InlineData("testpackage", true)]
		[InlineData("UnknownPackage", false)]
		public async Task Search_ReturnsContentOrNot_WhenQueryIsExists(string query, bool isExist)
		{
			// setup
			var testPackageWithNameInLowerCase = new Package()
			{
				Id = TestPackageHelper.MyTestPackageId.ToLowerInvariant(),
				Description = "Package description",
				Authors = "D.B.",
				Version = "1.1.1"
			};
			var allPackages = new List<Package>(TestPackageHelper.TestPackages) {testPackageWithNameInLowerCase};

			_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(() => allPackages);

			// Act
			var result = await _packageManager.Search(query);

			// Assert
			if (isExist)
			{
				Assert.True(result.Success);
				Assert.NotNull(result.Data);
				Assert.True(result.Data.Any());
				Assert.True(result.Data.Count == 1); // we should get only the latest version of TestPackage package
				Assert.True(result.Data.First().Version == testPackageWithNameInLowerCase.Version);
			}
			else
			{
				Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
			}
		}

		[Fact]
		public async Task Search_ReturnsEmptyList_WhenWeHaveNothing()
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(() => new Package[] { });

			// Act
			var result = await _packageManager.Search();

			// Assert
			Assert.True(result.Success);
			Assert.False(result.Data.Any());
		}

		[Theory]
		[InlineData(TestPackageHelper.MyTestPackageId, true)]
		[InlineData("UnknownPackage", false)]
		public async Task PackageVersions_ReturnsPackageVersionsOrNotFound(string packageId, bool isExist)
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(() => TestPackageHelper.TestPackages);

			// Act
			var result = await _packageManager.PackageVersions(packageId);

			// Assert
			if (isExist)
			{
				Assert.True(result.Success);
				Assert.NotNull(result.Data);
				Assert.True(result.Data.Any());
				Assert.True(result.Data.Count == 2); // we should get only the latest version of TestPackage package
				Assert.NotNull(result.Data.First(x => x.Id == packageId && x.PackageDependencies.Any()));
				Assert.Equal(TestPackageHelper.SomePackageDependencyId,
					result.Data.First(x => x.Id == packageId).PackageDependencies.First().Dependencies.First().Id);
			}
			else
			{
				Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
			}
		}


		[Fact]
		public async Task PackageVersions_ReturnsNotFoundResult()
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(() => new List<Package>());

			// Act
			var result = await _packageManager.PackageVersions(TestPackageHelper.MyTestPackageId);

			// Assert
			Assert.False(result.Success);
			Assert.True(result.StatusCode == HttpStatusCode.NotFound);
		}


		[Theory]
		[InlineData("mytestpackage", "1.0.0", true)]
		[InlineData("mytest", "1.0.0", false)]
		[InlineData(TestPackageHelper.MyTestPackageId, "1.0.1", true)]
		[InlineData(TestPackageHelper.MyTestPackageId, "2.0.0", false)]
		[InlineData("UnknownPackage", "1.0.0", false)]
		public async Task PackageExists_ReturnsPackageOrNotFound(string packageId, string packageVersion, bool isExist)
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(() => TestPackageHelper.TestPackages);

			// Act
			var packageExists = await _packageManager.PackageExists(packageId, packageVersion);

			// assert
			Assert.Equal(isExist, packageExists);
		}


		[Fact]
		public async Task PackageExists_ReturnsFalse_WhenWeHaveNothing()
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackages()).ReturnsAsync(new List<Package>());

			// Act
			var packageExists = await _packageManager.PackageExists(TestPackageHelper.GetOSVersionPackageId, TestPackageHelper.GetOSVersionPackageVersion);

			// Assert
			Assert.False(packageExists);
		}

		[Fact]
		public async Task PackageExists_ThrowsException_WhenIdOrVersionAreNull()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => new List<Package>());
			_mockPackageFileStorageService.Setup(s => s.Read()).Returns(() => new Package[] { });

			// Act + Assert
			await Assert.ThrowsAsync<ArgumentNullException>(() => _packageManager.PackageExists(It.IsAny<string>(), It.IsAny<string>()));
		}
		
		[Fact]
		public async Task Push_ThrowsException_WhenStreamIsUnavailableToRead()
		{
			// act + assert
			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				_packageManager.Push(It.IsAny<Stream>()));
		}
	}
}