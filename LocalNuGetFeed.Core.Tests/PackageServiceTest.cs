using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using LocalNugetFeed.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Moq;
using NuGet.Packaging;
using Xunit;

namespace LocalNuGetFeed.Core.Tests
{
	public class PackageServiceTest
	{
		private readonly Mock<IPackageFileStorageService> _mockPackageFileStorageService;
		private readonly Mock<IPackageSessionService> _mockPackageSessionService;
		private string _getOSVersionPackageName = "GetOSVersion";
		private string _getOSVersionPackageVersion = "1.0.0";
		private PackageService _packageService;

		public PackageServiceTest()
		{
			_mockPackageFileStorageService = new Mock<IPackageFileStorageService>();
			_mockPackageSessionService = new Mock<IPackageSessionService>();
			_packageService = new PackageService(_mockPackageFileStorageService.Object, _mockPackageSessionService.Object);
		}

		[Fact]
		public async Task Push_ReturnsSuccessfulResponse()
		{
			var _mockFilePath = GetOSVersionPackageFilePath();

			using (var stream = new MemoryStream(File.ReadAllBytes(_mockFilePath)))
			{
				var _mockFile = new FormFile(stream, 0, stream.Length, _getOSVersionPackageName, $"{_getOSVersionPackageName}.{_getOSVersionPackageVersion}.nupkg");
				stream.Seek(0, SeekOrigin.Begin);

				// setup
				_mockPackageFileStorageService.Setup(s => s.Save(It.IsAny<PackageArchiveReader>(), It.IsAny<Stream>()))
					.ReturnsAsync(new ResponseModel<Package>(HttpStatusCode.OK));
				_mockPackageSessionService.Setup(s => s.Set(It.IsAny<Package>()));
				_mockPackageSessionService.Setup(s => s.Get()).Returns(() => new[] {GetMockPackage()});

				// Act
				var result = await _packageService.Push(_mockFile);

				// Assert
				Assert.True(result.Success);
			}
		}
		
		[Fact]
		public async Task Push_ReturnsBadRequestResponse_WhenPackageIsAlreadyExists()
		{
			var _mockFilePath = GetOSVersionPackageFilePath();

			using (var stream = new MemoryStream(File.ReadAllBytes(_mockFilePath)))
			{
				var _mockFile = new FormFile(stream, 0, stream.Length, _getOSVersionPackageName, $"{_getOSVersionPackageName}.{_getOSVersionPackageVersion}.nupkg");
				stream.Seek(0, SeekOrigin.Begin);

				// setup
				_mockPackageFileStorageService.Setup(s => s.Save(It.IsAny<PackageArchiveReader>(), It.IsAny<Stream>()))
					.ReturnsAsync(new ResponseModel<Package>(HttpStatusCode.OK));
				_mockPackageSessionService.Setup(s => s.Set(It.IsAny<Package>()));
				_mockPackageSessionService.Setup(s => s.Get()).Returns(() => new[] {GetOSVersionPackage()});

				// Act
				var packageService = new PackageService(_mockPackageFileStorageService.Object, _mockPackageSessionService.Object);
				var result = await packageService.Push(_mockFile);

				// Assert
				Assert.False(result.Success);
				Assert.True(result.StatusCode == HttpStatusCode.Conflict);
			}
		}

		[Fact]
		public async Task Push_ReturnsFailedResponse_PackageFileIsNull()
		{
			var packageService = new PackageService(_mockPackageFileStorageService.Object, _mockPackageSessionService.Object);
			var result = await packageService.Push(null);

			// Assert
			Assert.False(result.Success);
			Assert.True(result.StatusCode == HttpStatusCode.BadRequest);
		}
		
		[Fact]
		public async Task Push_ReturnsFailedResponse_PackageFileIsIncorrect()
		{
			var result = await _packageService.Push(GetMockFile("some content", "wrongPackageFileExtension.txt"));
			// Assert
			Assert.False(result.Success);
			Assert.True(result.StatusCode == HttpStatusCode.UnsupportedMediaType);
			Assert.IsType<InvalidDataException>(result.ExceptionDetails);
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


		private string GetOSVersionPackageFilePath()
		{
			var directoryInfo = Directory.GetParent(Directory.GetCurrentDirectory()).Parent;
			if (directoryInfo != null)
			{
				var physicalFile = new FileInfo($@"{directoryInfo.Parent}\Packages\{_getOSVersionPackageName}.{_getOSVersionPackageVersion}.nupkg");

				return physicalFile.FullName;
			}

			return null;
		}
		
		
		private static IReadOnlyList<Package> TwoTestPackageVersions => new List<Package>()
		{
			new Package()
			{
				Id = "MyTestPackage",
				Description = "Package description",
				Authors = "D.B.",
				Version = "1.0.0"
			},
			new Package()
			{
				Id = "MyTestPackage",
				Description = "Package description",
				Authors = "D.B.",
				Version = "1.0.1"
			}
		};

		private Package GetMockPackage()
		{
			return new Package()
			{
				Id = "MyPackageTest",
				Version = "1.0.0"
			};
		}
		
		private Package GetOSVersionPackage()
		{
			return new Package()
			{
				Id = _getOSVersionPackageName,
				Version = _getOSVersionPackageVersion
			};
		}
		
		/// <summary>
		/// Setup mock file using a memory stream
		/// </summary>
		/// <returns>Mock file</returns>
		private IFormFile GetMockFile(string content, string fileName)
		{
			var fileMock = new Mock<IFormFile>();

			var ms = new MemoryStream();
			var writer = new StreamWriter(ms);
			writer.Write(content);
			writer.Flush();
			ms.Position = 0;
			fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
			fileMock.Setup(_ => _.FileName).Returns(fileName);
			fileMock.Setup(_ => _.Length).Returns(ms.Length);

			return fileMock.Object;
		}
	}
}