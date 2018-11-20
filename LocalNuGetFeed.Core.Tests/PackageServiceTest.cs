using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using LocalNugetFeed.Core.Services;
using Microsoft.AspNetCore.Http.Internal;
using Moq;
using NuGet.Packaging;
using NuGet.Versioning;
using Xunit;

namespace LocalNuGetFeed.Core.Tests
{
	public class PackageServiceTest
	{
		private readonly Mock<IPackageFileStorageService> _mockPackageFileStorageService;
		private readonly Mock<IPackageSessionService> _mockPackageSessionService;

		private const string MyTestPackageId = "MyTestPackage";
		private const string SomePackageDependencyId = "SomePackageDependency";

		private readonly PackageService _packageService;
		private string _mockFilePath => TestPackageHelper.GetOSVersionPackageFilePath();

		public PackageServiceTest()
		{
			_mockPackageFileStorageService = new Mock<IPackageFileStorageService>();
			_mockPackageSessionService = new Mock<IPackageSessionService>();
			_packageService = new PackageService(_mockPackageFileStorageService.Object, _mockPackageSessionService.Object);
		}

		[Fact]
		public async Task Push_ReturnsSuccessfulResponse()
		{
			using (var stream = new MemoryStream(File.ReadAllBytes(_mockFilePath)))
			{
				var _mockFile = new FormFile(stream, 0, stream.Length, TestPackageHelper.TestPackageId,
					$"{TestPackageHelper.TestPackageId}.{TestPackageHelper.TestPackageVersion}.nupkg");
				stream.Seek(0, SeekOrigin.Begin);

				// setup
				_mockPackageFileStorageService.Setup(s => s.Save(It.IsAny<NuspecReader>(), It.IsAny<Stream>()))
					.ReturnsAsync(It.IsAny<Package>());
				_mockPackageSessionService.Setup(s => s.Set(It.IsAny<Package>()));
				_mockPackageSessionService.Setup(s => s.Get()).Returns(() => new[] {TestPackageHelper.GetMockPackage()});

				// Act
				var result = await _packageService.Push(_mockFile);

				// Assert
				Assert.True(result.Success);
			}
		}

		[Fact]
		public async Task Push_ReturnsBadRequestResponse_WhenPackageIsAlreadyExists()
		{
			using (var stream = new MemoryStream(File.ReadAllBytes(_mockFilePath)))
			{
				var _mockFile = new FormFile(stream, 0, stream.Length, TestPackageHelper.TestPackageId,
					$"{TestPackageHelper.TestPackageId}.{TestPackageHelper.TestPackageVersion}.nupkg");
				stream.Seek(0, SeekOrigin.Begin);

				// setup
				_mockPackageFileStorageService.Setup(s => s.Save(It.IsAny<NuspecReader>(), It.IsAny<Stream>()))
					.ReturnsAsync(It.IsAny<Package>());
				_mockPackageSessionService.Setup(s => s.Set(It.IsAny<Package>()));
				_mockPackageSessionService.Setup(s => s.Get()).Returns(() => new[] {TestPackageHelper.GetOSVersionPackage()});

				// Act
				var packageService = new PackageService(_mockPackageFileStorageService.Object, _mockPackageSessionService.Object);
				var result = await packageService.Push(_mockFile);

				// Assert
				Assert.False(result.Success);
				Assert.True(result.StatusCode == HttpStatusCode.Conflict);
			}
		}

		[Fact]
		public async Task Push_ThrowsException_PackageFileIsNull()
		{
			// setup
			var packageService = new PackageService(_mockPackageFileStorageService.Object, _mockPackageSessionService.Object);

			// Act + Assert
			await Assert.ThrowsAsync<ArgumentNullException>(() => packageService.Push(null));
		}

		[Fact]
		public async Task Push_ThrowsException_PackageFileIsIncorrect()
		{
			// setup
			var mockFile = TestPackageHelper.GetMockFile("some content", "wrongPackageFileExtension.txt");

			// act + assert
			await Assert.ThrowsAsync<InvalidDataException>(() =>
				_packageService.Push(mockFile));
		}

		[Fact]
		public async Task Search_ReturnsPackagesFilteredByVersionDesc_WhenQueryIsEmpty()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => TwoTestPackageVersions);

			// Act
			var result = await _packageService.Search();

			// Assert
			Assert.True(result.Success);
			Assert.True(result.Data.Any());
			Assert.NotNull(result.Data.Single());
		}

		[Theory]
		[InlineData("", true)]
		[InlineData("TestPackage", true)]
		[InlineData("testpackage", true)]
		[InlineData("UnknownPackage", false)]
		public async Task Search_ReturnsContentOrNot_WhenQueryIsExists(string query, bool isExist)
		{
			// setup
			var testPackageWithNameInLowerCase = new Package()
			{
				Id = MyTestPackageId.ToLowerInvariant(),
				Description = "Package description",
				Authors = "D.B.",
				Version = "1.1.1"
			};
			var allPackages = new List<Package>(TwoTestPackageVersions) {testPackageWithNameInLowerCase};

			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => allPackages);

			// Act
			var result = await _packageService.Search(query);

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
		public async Task Search_ReturnsEmptyList()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => new Package[] { });
			_mockPackageFileStorageService.Setup(s => s.Read()).Returns(() => new Package[] { });

			// Act
			var result = await _packageService.Search();

			// Assert
			Assert.True(result.Success);
			Assert.False(result.Data.Any());
		}

		[Theory]
		[InlineData(MyTestPackageId, true)]
		[InlineData("UnknownPackage", false)]
		public async Task PackageVersions_ReturnsPackageVersionsOrNotFound(string packageId, bool isExist)
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => TwoTestPackageVersions);

			// Act
			var result = await _packageService.PackageVersions(packageId);

			// Assert
			if (isExist)
			{
				Assert.True(result.Success);
				Assert.NotNull(result.Data);
				Assert.True(result.Data.Any());
				Assert.True(result.Data.Count == 2); // we should get only the latest version of TestPackage package
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
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => new List<Package>());
			_mockPackageFileStorageService.Setup(s => s.Read()).Returns(() => new Package[] { });

			// Act
			var result = await _packageService.PackageVersions(MyTestPackageId);

			// Assert
			Assert.False(result.Success);
			Assert.True(result.StatusCode == HttpStatusCode.NotFound);
		}

		[Theory]
		[InlineData("mytestpackage", "1.0.0", false, true)]
		[InlineData("mytest", "1.0.0", false, false)]
		[InlineData(MyTestPackageId, "1.0.1", true, true)]
		[InlineData(MyTestPackageId, "2.0.0", false, false)]
		[InlineData("UnknownPackage", "1.0.0", false, false)]
		public async Task GetPackage_ReturnsPackageOrNotFound(string packageId, string packageVersion, bool hasDependencies, bool isExist)
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => TwoTestPackageVersions);

			// Act
			var result = await _packageService.GetPackage(packageId, packageVersion);

			// Assert
			if (isExist)
			{
				Assert.True(result.Success);
				Assert.NotNull(result.Data);
				if (hasDependencies)
				{
					Assert.NotNull(result.Data.PackageDependencies);
					Assert.True(result.Data.PackageDependencies.Any());
					Assert.Equal(SomePackageDependencyId, result.Data.PackageDependencies.First().Dependencies.First().Id);
				}
			}
			else
			{
				Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
			}
		}

		[Fact]
		public async Task GetPackage_ReturnsNotFoundResult()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => new List<Package>());
			_mockPackageFileStorageService.Setup(s => s.Read()).Returns(() => new Package[] { });

			// Act
			var result = await _packageService.GetPackage(MyTestPackageId, "1.0.0");

			// Assert
			Assert.False(result.Success);
			Assert.True(result.StatusCode == HttpStatusCode.NotFound);
		}

		[Fact]
		public async Task GetPackages_ReturnsDataFromSession()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => TwoTestPackageVersions);

			// Act
			var result = await _packageService.GetPackages();

			// Assert
			Assert.NotNull(result);
			Assert.True(result.Any());
			Assert.True(result.Count == 2);
		}

		[Fact]
		public async Task GetPackages_ReturnsDataFromFileSystem()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => null);
			_mockPackageFileStorageService.Setup(s => s.Read())
				.Returns(() => TwoTestPackageVersions);

			// Act
			var result = await _packageService.GetPackages();

			// Assert
			Assert.NotNull(result);
			Assert.True(result.Any());
			Assert.True(result.Count == 2);
		}

		[Fact]
		public async Task GetPackages_ReturnsEmptyList()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => null);
			_mockPackageFileStorageService.Setup(s => s.Read()).Returns(() => new Package[] { });

			// Act
			var result = await _packageService.GetPackages();

			// Assert
			Assert.NotNull(result);
			Assert.False(result.Any());
		}


		private static IReadOnlyList<Package> TwoTestPackageVersions => new List<Package>()
		{
			new Package()
			{
				Id = MyTestPackageId,
				Description = "Package description",
				Authors = "D.B.",
				Version = "1.0.0"
			},
			new Package()
			{
				Id = MyTestPackageId,
				Description = "Package description",
				Authors = "D.B.",
				Version = "1.0.1",
				PackageDependencies = new List<PackageDependencies>()
				{
					new PackageDependencies()
					{
						TargetFramework = ".Net Core v.2.1",
						Dependencies = new List<PackageDependency>()
						{
							new PackageDependency()
							{
								Id = "SomePackageDependency",
								Version = "1.1.1"
							}
						}
					}
				}
			}
		};
	}
}