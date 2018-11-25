using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using LocalNugetFeed.Core.BLL.DTO;
using LocalNugetFeed.Core.BLL.Interfaces;
using LocalNugetFeed.Core.Configuration;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Services;
using LocalNugetFeed.Web.Controllers;
using LocalNuGetFeed.Core.Tests;
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
		private readonly Mock<IPackageManager> _mockPackageManager;
		private readonly Mock<IPackageFileStorageService> _mockPackageFileStorageService;
		private readonly Mock<IPackageService> _mockPackageService;
		private readonly PackageController _controller;
		private readonly IMapper _mapper;
		private IReadOnlyList<PackageDTO> TestPackageDTOs => _mapper.Map<IReadOnlyList<PackageDTO>>(TestPackageHelper.TestPackages);
		private IReadOnlyList<PackageVersionsDTO> TestPackageVersionsDTOs => _mapper.Map<IReadOnlyList<PackageVersionsDTO>>(TestPackageHelper.TestPackages);

		public PackageControllerTest()
		{
			_mapper = AutoMapperConfiguration.Configure().CreateMapper();
			_mockPackageManager = new Mock<IPackageManager>();
			_mockPackageFileStorageService = new Mock<IPackageFileStorageService>();
			_mockPackageService = new Mock<IPackageService>();
			_controller = new PackageController(_mockPackageManager.Object);
		}

		[Fact]
		public async Task Push_ThrowsException_PackageFileIsNull()
		{
			// Act + Assert
			await Assert.ThrowsAsync<ArgumentNullException>(() => _controller.Push(null));
		}

		[Fact]
		public async Task Push_ReturnsSuccessfulResponse()
		{
			var _mockFile = TestPackageHelper.GetMockFile("MyTestPackage.nupkg", "Some content");
			// setup
			_mockPackageManager.Setup(s => s.Push(_mockFile.OpenReadStream()))
				.ReturnsAsync(new ResponseDTO<PackageDTO>(_mapper.Map<PackageDTO>(TestPackageHelper.GetOSVersionPackage())));

			// Act
			var result = await _controller.Push(_mockFile);

			// Assert
			var actionResult = Assert.IsType<OkObjectResult>(result.Result);
			var pushResponse = Assert.IsAssignableFrom<ResponseDTO<PackageDTO>>(
				actionResult.Value);

			Assert.True(pushResponse.Success);
		}

		[Fact]
		public async Task Search_ReturnsPackagesFilteredByVersionDesc_WhenQueryIsEmpty()
		{
			// setup
			var lastVersionPackage = TestPackageDTOs.OrderByDescending(x => new NuGetVersion(x.Version)).Last();
			_mockPackageManager.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseDTO<IReadOnlyList<PackageDTO>>(new[] {lastVersionPackage}));

			// Act
			var result = await _controller.Search();

			// Assert
			var actionResult = Assert.IsType<OkObjectResult>(result.Result);
			var packages = Assert.IsAssignableFrom<IReadOnlyList<PackageDTO>>(
				actionResult.Value);
			Assert.NotNull(packages);
			Assert.True(packages.Any());
			var testPackage = packages.Single();
			Assert.True(testPackage.Version == lastVersionPackage.Version);
		}


		[Fact]
		public async Task Search_ReturnsEmptyList()
		{
			// setup
			_mockPackageManager.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseDTO<IReadOnlyList<PackageDTO>>(new PackageDTO[] { }));

			// Act
			var result = await _controller.Search();

			// Assert
			var actionResult = Assert.IsType<OkObjectResult>(result.Result);
			var packages = Assert.IsAssignableFrom<IReadOnlyList<PackageDTO>>(
				actionResult.Value);
			Assert.NotNull(packages);
			Assert.False(packages.Any());
		}


		[Theory]
		[InlineData("", true)]
		[InlineData("TestPackage", true)]
		[InlineData("UnknownPackage", false)]
		public async Task Search_ReturnsResponseAccordingWithInlineData(string query, bool isExist)
		{
			// setup
			var searchResult =
				new ResponseDTO<IReadOnlyList<PackageDTO>>(new[]
					{TestPackageDTOs.OrderByDescending(x => new NuGetVersion(x.Version)).Last()});

			_mockPackageManager.Setup(s => s.Search(query))
				.ReturnsAsync(() => isExist ? searchResult : new ResponseDTO<IReadOnlyList<PackageDTO>>(HttpStatusCode.NotFound));

			// Act
			var result = await _controller.Search(query);

			// Assert
			if (isExist)
			{
				var actionResult = Assert.IsType<OkObjectResult>(result.Result);
				var packages = Assert.IsAssignableFrom<IReadOnlyList<PackageDTO>>(
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
			_mockPackageManager.Setup(s => s.Search(null))
				.ReturnsAsync(new ResponseDTO<IReadOnlyList<PackageDTO>>(HttpStatusCode.NotFound));

			// Act
			var result = await _controller.Search();

			// Assert
			Assert.IsType<NotFoundObjectResult>(result.Result);
		}

		[Theory]
		[InlineData("MyTestPackage", true)]
		[InlineData("UnknownPackage", false)]
		public async Task PackageVersions_ReturnsResponseAccordingWithInlineData(string packageId, bool packageExists)
		{
			// setup
			_mockPackageService.Setup(s => s.GetPackages(true))
				.ReturnsAsync(() => TestPackageHelper.TestPackages);

			_mockPackageManager.Setup(s => s.GetPackageVersions(packageId))
				.ReturnsAsync(() =>
				{
					var searchResult = new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(TestPackageVersionsDTOs);
					return packageExists ? searchResult : new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(HttpStatusCode.NotFound);
				});

			// Act
			var result = await _controller.PackageVersions(packageId);

			// Assert
			if (packageExists)
			{
				var actionResult = Assert.IsType<OkObjectResult>(result.Result);
				var packages = Assert.IsAssignableFrom<IReadOnlyList<PackageVersionsDTO>>(
					actionResult.Value);
				Assert.NotNull(packages);
				Assert.True(packages.Count == TestPackageDTOs.Count); // we should get all versions of TestPackage package
			}
			else
			{
				Assert.IsType<NotFoundObjectResult>(result.Result);
			}
		}


		#region integration & unit tests using testserver host

		[Theory]
		[InlineData(null, HttpStatusCode.OK)]
		[InlineData("test", HttpStatusCode.OK)]
		[InlineData("Unknown", HttpStatusCode.NotFound)]
		public async Task Search_GetByRouteTemplate_ReturnsResponseAccordingWithInlineData(string query, HttpStatusCode statusCode)
		{
			// setup
			var searchResult = TestPackageDTOs
				.Where(x => string.IsNullOrWhiteSpace(query) || x.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(s => new NuGetVersion(s.Version))
				.GroupBy(g => g.Id)
				.Select(z => z.First()).ToList();
			_mockPackageManager.Setup(s => s.Search(query))
				.ReturnsAsync(searchResult.Any()
					? new ResponseDTO<IReadOnlyList<PackageDTO>>(searchResult)
					: new ResponseDTO<IReadOnlyList<PackageDTO>>(HttpStatusCode.NotFound));
			// Act
			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(_mockPackageManager.Object); }));

			var client = server.CreateClient();

			var response = await client.GetAsync(!string.IsNullOrWhiteSpace(query) ? $"api/packages?q={query}" : "api/packages");
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
		public async Task Push_PutByRouteTemplate_ReturnsSuccessfulResponse()
		{
			var _mockFile = TestPackageHelper.GetMockFile("MyTestPackage.nupkg", "Some content");
			// setup
			_mockPackageManager.Setup(s => s.Push(It.IsAny<Stream>()))
				.ReturnsAsync(new ResponseDTO<PackageDTO>(HttpStatusCode.OK));

			// Act

			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(_mockPackageManager.Object); }));

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

		[Theory]
		[InlineData("MyTestPackage", HttpStatusCode.OK)]
		[InlineData("Unknown", HttpStatusCode.NotFound)]
		public async Task PackageVersions_GetByRouteTemplate_ReturnsResponseAccordingWithInlineData(string packageId, HttpStatusCode statusCode)
		{
			// setup
			var packageVersions = TestPackageVersionsDTOs.Where(x => x.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)).ToList();
			_mockPackageManager.Setup(s => s.GetPackageVersions(packageId))
				.ReturnsAsync(packageVersions.Any()
					? new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(packageVersions)
					: new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(HttpStatusCode.NotFound));

			// Act
			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(_mockPackageManager.Object); }));

			var client = server.CreateClient();
			var response = await client.GetAsync($"api/package/{packageId}");
			var content = await response.Content.ReadAsStringAsync();
			var packages = JsonConvert.DeserializeObject<IReadOnlyList<Package>>(content);

			// Assert

			Assert.Equal(statusCode, response.StatusCode);
			if (statusCode == HttpStatusCode.OK)
			{
				Assert.True(packages.Any());
				Assert.True(packages.Count == TestPackageVersionsDTOs.Count);  // we should get all versions of TestPackage package
			}
			else
			{
				Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
			}
		}

		/// <summary>
		/// Common test to check global exception handling in app. for example, when we search something
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task Search_GlobalExceptionHandling_WhenReadFromFileThrowsException()
		{
			// setup
			string packageFolderNotFoundExceptionMessage = "Packages folder not found";
			_mockPackageFileStorageService.Setup(s => s.Read())
				.Returns(() => throw new DirectoryNotFoundException(packageFolderNotFoundExceptionMessage));

			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(_mockPackageFileStorageService.Object); }));

			var client = server.CreateClient();

			var responseMessage = await client.GetAsync("api/packages");
			var content = await responseMessage.Content.ReadAsStringAsync();
			var response = JsonConvert.DeserializeObject<ResponseDTO>(content);

			//Assert
			Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
			Assert.Contains(packageFolderNotFoundExceptionMessage, response.Message);
		}

		[Fact]
		public async Task Push_GlobalExceptionHandling_WhenSaveToFileThrowsException()
		{
			// setup
			string packageFileNotFoundExceptionMessage = "Package file not found";
			_mockPackageManager.Setup(s => s.Push(It.IsAny<Stream>()))
				.Returns(() => throw new ArgumentNullException(packageFileNotFoundExceptionMessage));

			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services => { services.AddSingleton(_mockPackageManager.Object); }));

			var client = server.CreateClient();

			var responseMessage = await client.PutAsync(Core.Common.Constants.NuGetPushRelativeUrl, It.IsAny<HttpContent>());

			var content = await responseMessage.Content.ReadAsStringAsync();
			var response = JsonConvert.DeserializeObject<ResponseDTO>(content);

			//Assert
			Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
			Assert.Contains(packageFileNotFoundExceptionMessage, response.Message);
		}

		/// <summary>
		/// integration testing of <see cref="PackageSessionService"/>, but by mocking of <see cref="PackageFileStorageService"/>
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task Search_GetByRouteTemplate_ReturnsPackagesFromSession()
		{
			// setup
			var mockPackageFileStorageService = new Mock<IPackageFileStorageService>();
			mockPackageFileStorageService.Setup(s => s.Read()).Returns(TestPackageHelper.TestPackages);

			// Act
			TestServer server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>()
				.ConfigureTestServices(services =>
				{
					services.AddSingleton(mockPackageFileStorageService.Object);
					services.AddHttpContextAccessor();
				}));

			var client = server.CreateClient();

			var response = await client.GetAsync("api/packages");

			//Assert

			var content = await response.Content.ReadAsStringAsync();
			var packages = JsonConvert.DeserializeObject<IReadOnlyList<Package>>(content);
			Assert.True(packages.Any());
			Assert.NotNull(packages.Single());
		}

		#endregion
	}
}