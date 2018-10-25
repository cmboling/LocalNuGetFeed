using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LocalNugetFeed.Controllers
{
	public class PackageController : Controller
	{
		private readonly IPackageService _packageService;

		public PackageController(IPackageService packageService)
		{
			_packageService = packageService;
		}

		private const string DefaultErrorMessage = "An error has occured during request. Please try again later.";

		/// <summary>
		/// Pushes a nuget package to local feed
		/// Refs: https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package
		/// </summary>
		/// <param name="package">nuget package</param>
		/// <returns>Status of push request</returns>
		[HttpPut]
		public async Task<IActionResult> Push([BindRequired, FromBody] IFormFile package)
		{
			var result = await _packageService.Push(package);

			if (result.Success)
			{
				return Ok(result);
			}

			return BadRequest(result.Message ?? DefaultErrorMessage);
		}

		/// <summary>
		/// Get packages from local feed (query is optional)
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		[Route("packages/{q}")]
		[Route("")]
		[HttpGet]
		public async Task<IActionResult> Get([FromQuery(Name = "q")] string query = null)
		{
			var searchResult = await _packageService.Search(query);

			if (!searchResult.Success)
			{
				return BadRequest(searchResult.Message ?? DefaultErrorMessage);
			}
			
			if (searchResult.StatusCode == HttpStatusCode.NoContent)
			{
				return NoContent();
			}
			
			return Ok(new JsonResult(searchResult.Data));
		}

		/// <summary>
		/// Get specific package from local feed by id
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[Route("package/{id}")]
		[HttpGet]
		public async Task<IActionResult> PackageVersions([BindRequired, FromRoute] string id)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var result = await Task.FromResult(_packageService.PackageVersions(id));

			if (result.Success)
			{
				return Ok(new JsonResult(result.Data));
			}

			return BadRequest(result.Message ?? DefaultErrorMessage);
		}
	}
}