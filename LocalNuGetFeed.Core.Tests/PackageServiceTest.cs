using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Services;
using Moq;
using NuGet.Packaging;
using Xunit;

namespace LocalNuGetFeed.Core.Tests
{
	public class PackageServiceTest
	{
		private readonly Mock<IPackageFileStorageService> _mockPackageFileStorageService;
		private readonly Mock<IPackageSessionService> _mockPackageSessionService;

		private readonly PackageService _packageService;

		public PackageServiceTest()
		{
			_mockPackageFileStorageService = new Mock<IPackageFileStorageService>();
			_mockPackageSessionService = new Mock<IPackageSessionService>();
			_packageService = new PackageService(_mockPackageFileStorageService.Object, _mockPackageSessionService.Object);
		}

		[Fact]
		public async Task Push_ReturnsSuccessfulResponse()
		{
			// setup
			_mockPackageFileStorageService.Setup(s => s.Save(It.IsAny<NuspecReader>(), It.IsAny<Stream>()))
				.ReturnsAsync(TestPackageHelper.GetOSVersionPackage);
			_mockPackageSessionService.Setup(s => s.Set(It.IsAny<Package>()));

			// Act
			var newPackage = await _packageService.Push(It.IsAny<NuspecReader>(), It.IsAny<Stream>());

			// Assert
			Assert.NotNull(newPackage);
			Assert.Equal(newPackage.Id, TestPackageHelper.GetOSVersionPackageId);
		}


		[Fact]
		public async Task GetPackages_ReturnsDataFromSession()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => TestPackageHelper.TestPackages);

			// Act
			var result = await _packageService.GetPackages();

			// Assert
			Assert.NotNull(result);
			Assert.True(result.Any());
			Assert.True(result.Count == TestPackageHelper.TestPackages.Count);
		}

		[Fact]
		public async Task GetPackages_ReturnsDataFromFileSystem()
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => null);
			_mockPackageFileStorageService.Setup(s => s.Read())
				.Returns(() => TestPackageHelper.TestPackages);

			// Act
			var result = await _packageService.GetPackages();

			// Assert
			Assert.NotNull(result);
			Assert.True(result.Any());
			Assert.True(result.Count == TestPackageHelper.TestPackages.Count);
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


		[Theory]
		[InlineData(TestPackageHelper.MyTestPackageId, true)]
		[InlineData("UnknownPackage", false)]
		public async Task PackageVersions_ReturnsPackageVersionsOrNotFound(string packageId, bool isExist)
		{
			// setup
			_mockPackageFileStorageService.Setup(s => s.Read()).Returns(() => TestPackageHelper.TestPackages);

			// Act
			var result = await _packageService.GetPackageVersions(packageId);

			// Assert
			if (isExist)
			{
				Assert.True(result.Any());
				Assert.True(result.Count == TestPackageHelper.TestPackages.Count);
			}
			else
			{
				Assert.False(result.Any());
			}
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

			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => allPackages);

			// Act
			var result = await _packageService.Search(query);

			// Assert
			if (isExist)
			{
				Assert.True(result.Any());
				Assert.NotNull(result.Single());
			}
			else
			{
				Assert.False(result.Any());
			}
		}

		[Fact]
		public async Task PackageExists_ReturnsFalse_WhenWeHaveNothingInLocalFeed()
		{
			// setup
			_mockPackageFileStorageService.Setup(s => s.Read()).Returns(new List<Package>());

			// Act
			var packageExists = await _packageService.PackageExists(TestPackageHelper.GetOSVersionPackageId, TestPackageHelper.GetOSVersionPackageVersion);

			// Assert
			Assert.True(packageExists == false);
		}

		[Theory]
		[InlineData("mytestpackage", "1.0.0", true)]
		[InlineData("mytest", "1.0.0", false)]
		[InlineData(TestPackageHelper.MyTestPackageId, "1.0.1", true)]
		[InlineData(TestPackageHelper.MyTestPackageId, "2.0.0", false)]
		[InlineData("NewSomePackage", "1.0.0", false)]
		public async Task PackageExists_ReturnsBooleanFlag_AccordingWithInlineData(string packageId, string version, bool isExist)
		{
			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(TestPackageHelper.TestPackages);

			// Act
			var result = await _packageService.PackageExists(packageId, version);

			// Assert
			Assert.Equal(result, isExist);
		}

		[Theory]
		[InlineData("microsoft", "aspnetcore", "1.0.0", "this is microsoft.aspnetcore description", 1000, true)]
		[InlineData("my test package", "microsoft.aspnetcore.authentication.oauth", "1.0.0", "this is microsoft.aspnetcore.authentication.oauth some description of my test package", 5000, true)]
		[InlineData("test", "microsoft.aspnetcore.mvc.formatters.json","1.0.0",  "microsoft.aspnetcore.mvc.formatters.json description", 100000, false)] // this test case will crawl all 100k of records, because query does not match to any package id or description
		[InlineData("identity", "microsoft.aspnetcore.identity","1.0.0",  "just microsoft.aspnetcore.ui description", 134500, true)] // 134k+ -> this is real approximate count of nuget packages in it's storage 
		public async Task Search_BenchmarkTesting(string query, string packageId, string packageVersion, string description, int packagesCount, bool isExist)
		{
			var packages = new List<Package>();
			for (int i = 0; i < packagesCount; i++)
			{
				packages.Add(new Package() {Id = $"{packageId}.{new Random().Next()}", Description = description, Version = packageVersion});
			}

			// setup
			_mockPackageSessionService.Setup(s => s.Get()).Returns(() => packages);

			// Act
			var timer = new Stopwatch();
			timer.Start();

			var result = await _packageService.Search(query);

			timer.Stop();

			// Assert
			Debug.WriteLine(timer.ElapsedMilliseconds);
			Assert.True(timer.ElapsedMilliseconds < 500); // 0.5 sec
			Assert.Equal(result.Any(), isExist);
		}
	}
}