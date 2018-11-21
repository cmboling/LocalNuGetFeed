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
			Assert.True(result.Count == 2);
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


		
	}
}