using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Controllers;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace LocalNugetFeed.Web.Tests
{
	public class PackageControllerTest
	{
		[Fact]
		public async Task Push_ReturnsSuccessfulResponse()
		{
			var _mockFile = GetMockFile();
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Push(_mockFile))
				.ReturnsAsync(new ResponseModel(HttpStatusCode.OK));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Push(_mockFile);

			// Assert
			var actionResult = Assert.IsType<OkObjectResult>(result.Result);
			var pushResponse = Assert.IsAssignableFrom<ResponseModel>(
				actionResult.Value);

			Assert.True(pushResponse.Success);
		}

		[Fact]
		public async Task Push_ReturnsFailedResponse_PackageFileIsNull()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			const string failedResponseText = "Failed";
			mockPackageService.Setup(s => s.Push(null))
				.ReturnsAsync(new ResponseModel(HttpStatusCode.BadRequest, failedResponseText));
			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Push(null);

			// Assert
			var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
			Assert.Equal(actionResult.Value, failedResponseText);
		}

		[Fact]
		public async Task Get_ReturnsContent_WhenQueryIsEmpty()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, TestPackages));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Get();

			// Assert
			var actionResult = Assert.IsType<OkObjectResult>(result.Result);
			var packages = Assert.IsAssignableFrom<IReadOnlyList<Package>>(
				actionResult.Value);
			Assert.NotNull(packages);
			Assert.True(packages.Any());
			Assert.True(packages.First().Id == TestPackages.First().Id);
		}

		[Fact]
		public async Task Get_ReturnsBadRequestResult()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.BadRequest));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Get();

			// Assert
			Assert.IsType<BadRequestObjectResult>(result.Result);
		}


		[Theory]
		[InlineData("", true)]
		[InlineData("TestPackage", true)]
		[InlineData("UnknownPackage", false)]
		public async Task Get_ReturnsContentOrNot_WhenQueryIsExists(string query, bool isExist)
		{
			// setup
			var searchResult = new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK,
				new[] {TestPackages.OrderByDescending(x => new NuGetVersion(x.Version)).Last()});

			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Search(query))
				.ReturnsAsync(() => isExist ? searchResult : new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Get(query);

			// Assert
			if (isExist)
			{
				var actionResult = Assert.IsType<OkObjectResult>(result.Result);
				var packages = Assert.IsAssignableFrom<IReadOnlyList<Package>>(
					actionResult.Value);
				Assert.NotNull(packages);
				Assert.True(packages.Any());
				Assert.True(packages.First() == searchResult.Data.First());
			}
			else
			{
				Assert.IsType<NotFoundObjectResult>(result.Result);
			}
		}

		[Fact]
		public async Task Get_ReturnsNotFound_WhenNoPackages()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Get();

			// Assert
			Assert.IsType<NotFoundObjectResult>(result.Result);
		}

		[Theory]
		[InlineData("MyTestPackage", true)]
		[InlineData("UnknownPackage", false)]
		public async Task PackageVersions_ReturnsContentOrNotFound(string packageId, bool packageExists)
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();

			mockPackageService.Setup(s => s.GetPackages())
				.ReturnsAsync(() => new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, TestPackages));

			mockPackageService.Setup(s => s.PackageVersions(packageId))
				.ReturnsAsync(() =>
				{
					var searchResult = new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, TestPackages);
					return packageExists ? searchResult : new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound);
				});

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.PackageVersions(packageId);

			// Assert
			if (packageExists)
			{
				var actionResult = Assert.IsType<OkObjectResult>(result.Result);
				var packages = Assert.IsAssignableFrom<IReadOnlyList<Package>>(
					actionResult.Value);
				Assert.NotNull(packages);
				Assert.True(packages.Count == 2);
			}
			else
			{
				Assert.IsType<NotFoundObjectResult>(result.Result);
			}
		}

		[Fact]
		public async Task PackageVersions_ReturnsBadRequestResult_WhenModelStateIsInvalid()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.PackageVersions(null))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.BadRequest));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.PackageVersions(null);

			// Assert
			var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
			Assert.IsType<SerializableError>(badRequestResult.Value);
		}

		/// <summary>
		/// Setup mock file using a memory stream
		/// </summary>
		/// <returns>Mock file</returns>
		private IFormFile GetMockFile()
		{
			var fileMock = new Mock<IFormFile>();

			var content = "Some content";
			var fileName = "MyTestPackage.nupkg";

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

		private static IReadOnlyList<Package> TestPackages => new List<Package>()
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
	}
}