using System.Linq;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LocalNugetFeed.Controllers
{
	public class PackageController : Controller
	{
		private readonly IPackageService _packageService;

		public PackageController(IPackageService packageService)
		{
			_packageService = packageService;
		}

		/// <summary>
		/// Pushes a nuget package to local feed
		/// Refs: https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package
		/// </summary>
		/// <param name="package">nuget package</param>
		/// <returns>Status of push request</returns>
		public async Task<ResponseModel> Push(IFormFile package)
		{
			var result = await _packageService.Push(package);

			return result;
		}

		[Route("packages/{q}")]
		[Route("")]
		public async Task<IActionResult> Get([FromQuery(Name = "q")] string query = null)
		{
			query = query ?? string.Empty;

			var searchResult = await _packageService.Search(query);

			if (searchResult.Success)
			{
				return new JsonResult(searchResult.Data);
			}

			return BadRequest(searchResult.Message);
		}

		[Route("package/{id}")]
		public async Task<IActionResult> PackageVersions(string id)
		{
			var result = await Task.FromResult(_packageService.PackageVersions(id));

			if (result.Success)
			{
				return new JsonResult(result.Data);
			}

			return BadRequest(result.Message);
		}
	}
}