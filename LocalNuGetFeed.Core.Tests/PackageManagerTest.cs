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
using NuGet.Versioning;
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
				_mockPackageService.Setup(s => s.GetPackages(false)).ReturnsAsync(() => new List<Package>());

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
				_mockPackageService.Setup(s => s.PackageExists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(() => true);

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
			_mockPackageService.Setup(s => s.GetPackages(true)).ReturnsAsync(TestPackageHelper.TestPackages.GetDictinctPackages);

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

			var distinctPackages = allPackages.GetDictinctPackages();
			
			_mockPackageService.Setup(s => s.GetPackages(true)).ReturnsAsync(() => distinctPackages);
			
			_mockPackageService.Setup(s => s.Search(query)).ReturnsAsync(() => distinctPackages.Where(x=>x.Id.Contains(query, StringComparison.OrdinalIgnoreCase) || 
			                                                                                        x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList());

			// Act
			var result = await _packageManager.Search(query);

			// Assert
			if (isExist)
			{
				Assert.True(result.Success);
				Assert.NotNull(result.Data);
				Assert.True(result.Data.Any());
				Assert.NotNull(result.Data.Single()); // we should get only the single TestPackage package with it's latest version 
				Assert.True(result.Data.Single().Version == allPackages.OrderByDescending(x => new NuGetVersion(x.Version)).First().Version);
			}
			else
			{
				Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
			}
		}

		[Fact]
		public async Task Search_ReturnsEmptyList_WhenWeHaveNothingInLocalFeed()
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackages(true)).ReturnsAsync(() => new Package[] { });

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
			_mockPackageService.Setup(s => s.GetPackageVersions(packageId)).ReturnsAsync(() =>
				TestPackageHelper.TestPackages.Where(x => x.Id.Contains(packageId, StringComparison.OrdinalIgnoreCase)).ToList());
			
			// Act
			var result = await _packageManager.GetPackageVersions(packageId);

			// Assert
			if (isExist)
			{
				Assert.True(result.Success);
				Assert.NotNull(result.Data);
				Assert.True(result.Data.Any());
				Assert.True(result.Data.Count == TestPackageHelper.TestPackages.Count);
			}
			else
			{
				Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
			}
		}


		[Fact]
		public async Task PackageVersions_ReturnsNotFoundResult_WhenWeHaveNothingInLocalFeed()
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackageVersions(It.IsAny<string>())).ReturnsAsync(() => new List<Package>());

			// Act
			var result = await _packageManager.GetPackageVersions(TestPackageHelper.MyTestPackageId);

			// Assert
			Assert.False(result.Success);
			Assert.True(result.StatusCode == HttpStatusCode.NotFound);
		}

		[Fact]
		public async Task PackageExists_ReturnsFalse_WhenWeGetFalseFromService()
		{
			// setup
			_mockPackageService.Setup(s => s.PackageExists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

			// Act
			var packageExists = await _packageManager.PackageExists(TestPackageHelper.GetOSVersionPackageId, TestPackageHelper.GetOSVersionPackageVersion);

			// Assert
			Assert.False(packageExists);
		}

		[Theory]
		[InlineData("TestPackage", null)]
		[InlineData(null, "1.0.0")]
		public async Task PackageExists_ThrowsException_WhenIdOrVersionAreNull(string packageId, string version)
		{
			// Act + Assert
			await Assert.ThrowsAsync<ArgumentNullException>(() => _packageManager.PackageExists(packageId, version));
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