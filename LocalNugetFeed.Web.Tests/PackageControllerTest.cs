using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LocalNugetFeed.Controllers;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
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
			mockPackageService.Setup(s => s.Push(null))
				.ReturnsAsync(new ResponseModel(HttpStatusCode.BadRequest));
			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Push(null);

			// Assert
			Assert.IsType<BadRequestObjectResult>(result.Result);
		}

		[Fact]
		public async Task Search_ReturnsPackagesFilteredByVersionDesc_WhenQueryIsEmpty()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			var lastVersionPackage = TwoTestPackageVersions.OrderByDescending(x => new NuGetVersion(x.Version)).Last();
			mockPackageService.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, new[] {lastVersionPackage}));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Search();

			// Assert
			var actionResult = Assert.IsType<OkObjectResult>(result.Result);
			var packages = Assert.IsAssignableFrom<IReadOnlyList<Package>>(
				actionResult.Value);
			Assert.NotNull(packages);
			Assert.True(packages.Any());
			var testPackage = packages.Single();
			Assert.True(testPackage.Version == lastVersionPackage.Version);
		}

		[Fact]
		public async Task Search_ReturnsBadRequestResult()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.BadRequest));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Search();

			// Assert
			Assert.IsType<BadRequestObjectResult>(result.Result);
		}


		[Theory]
		[InlineData("", true)]
		[InlineData("TestPackage", true)]
		[InlineData("UnknownPackage", false)]
		public async Task Search_ReturnsContentOrNot_WhenQueryIsExists(string query, bool isExist)
		{
			// setup
			var searchResult = new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK,
				new[] {TwoTestPackageVersions.OrderByDescending(x => new NuGetVersion(x.Version)).Last()});

			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Search(query))
				.ReturnsAsync(() => isExist ? searchResult : new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Search(query);

			// Assert
			if (isExist)
			{
				var actionResult = Assert.IsType<OkObjectResult>(result.Result);
				var packages = Assert.IsAssignableFrom<IReadOnlyList<Package>>(
					actionResult.Value);
				Assert.NotNull(packages);
				Assert.True(packages.Any());
				Assert.True(packages.Count == 1); // we should get only the latest version of TestPackage package
				Assert.True(packages.First() == searchResult.Data.First());
			}
			else
			{
				Assert.IsType<NotFoundObjectResult>(result.Result);
			}
		}

		[Fact]
		public async Task Search_ReturnsNotFound_WhenNoPackages()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.NotFound));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.Search();

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
				.ReturnsAsync(() => new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, TwoTestPackageVersions));

			mockPackageService.Setup(s => s.PackageVersions(packageId))
				.ReturnsAsync(() =>
				{
					var searchResult = new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.OK, TwoTestPackageVersions);
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
				Assert.True(packages.Count == 2); // we should get both versions of TestPackage package
			}
			else
			{
				Assert.IsType<NotFoundObjectResult>(result.Result);
			}
		}

		[Fact]
		public async Task PackageVersions_ReturnsBadRequestResult_WhenPackageIdIsUndefined()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.PackageVersions(null))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(HttpStatusCode.BadRequest));

			// Act
			var controller = new PackageController(mockPackageService.Object);
			var result = await controller.PackageVersions(null);

			// Assert
			Assert.IsType<BadRequestObjectResult>(result.Result);
		}

		#region unit tests with routing

		[Theory]
		[InlineData(null, HttpStatusCode.OK)]
		[InlineData("test", HttpStatusCode.OK)]
		[InlineData("Unknown", HttpStatusCode.NotFound)]
		public async Task Search_GetHttpRequest_ReturnsResponseAccordingWithRequest(string query, HttpStatusCode statusCode)
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			var searchResult = TwoTestPackageVersions.Where(x => string.IsNullOrWhiteSpace(query) || x.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(s => new NuGetVersion(s.Version))
				.GroupBy(g => g.Id)
				.Select(z => z.First()).ToList();
			mockPackageService.Setup(s => s.Search(query))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(statusCode, searchResult));
			// Act
			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(mockPackageService.Object); }));

			var client = server.CreateClient();

			var response = await client.GetAsync(!string.IsNullOrWhiteSpace(query) ? $"?q={query}" : "");
			//Assert

			Assert.Equal(statusCode, response.StatusCode);
			var content = await response.Content.ReadAsStringAsync();
			var packages = JsonConvert.DeserializeObject<IReadOnlyList<Package>>(content);

			if (string.IsNullOrWhiteSpace(query))
			{
				Assert.Equal(packages.Count, searchResult.Count);
			}
			else
			{
				if (statusCode == HttpStatusCode.OK)
				{
					Assert.NotNull(packages.Single());
				}
				else
				{
					Assert.Equal(HttpStatusCode.NotFound, statusCode);
				}
			}
		}

		[Fact]
		public async Task Push_PutHttpRequest_AddPackageToLocalFeed_ReturnsSuccessfulResponse()
		{
			var _mockFile = GetMockFile();
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Push(It.IsAny<IFormFile>()))
				.ReturnsAsync(new ResponseModel(HttpStatusCode.OK));

			// Act

			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(mockPackageService.Object); }));

			var client = server.CreateClient();

			using (var content = new MultipartFormDataContent())
			{
				content.Add(new StreamContent(_mockFile.OpenReadStream())
				{
					Headers =
					{
						ContentLength = _mockFile.Length
					}
				}, "package", _mockFile.FileName);

				var response = await client.PutAsync(Core.Common.Constants.NuGetPushRelativeUrl, content);

				//Assert
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			}
		}

		[Fact]
		public async Task Push_PutHttpRequest_AddNullableFileToLocalFeed_ReturnsBadRequestResponse()
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.Push(null))
				.ReturnsAsync(new ResponseModel(HttpStatusCode.BadRequest));

			// Act

			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(mockPackageService.Object); }));

			var client = server.CreateClient();

			var response = await client.PutAsync(Core.Common.Constants.NuGetPushRelativeUrl, null);

			//Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Theory]
		[InlineData("MyTestPackage", HttpStatusCode.OK)]
		[InlineData("Unknown", HttpStatusCode.NotFound)]
		public async Task PackageVersions_GetHttpRequest_ReturnsResponseAccordingWithRequest(string packageId, HttpStatusCode statusCode)
		{
			// setup
			var mockPackageService = new Mock<IPackageService>();
			mockPackageService.Setup(s => s.PackageVersions(packageId))
				.ReturnsAsync(new ResponseModel<IReadOnlyList<Package>>(statusCode,
					TwoTestPackageVersions.Where(x => x.Id.Contains(packageId)).ToList()));

			// Act
			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(mockPackageService.Object); }));

			var client = server.CreateClient();
			var response = await client.GetAsync($"package/{packageId}");
			var content = await response.Content.ReadAsStringAsync();
			var packages = JsonConvert.DeserializeObject<IReadOnlyList<Package>>(content);
			
			// Assert
			
			Assert.Equal(statusCode, response.StatusCode);
			if (statusCode == HttpStatusCode.OK)
			{
				Assert.True(packages.Any());
				Assert.True(packages.Count == 2); //2 versions = 2 packages
			}
			else
			{
				Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
			}
		}

		#endregion

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
	}
}