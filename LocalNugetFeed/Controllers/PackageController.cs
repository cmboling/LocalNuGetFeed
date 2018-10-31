using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
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
		/// <param name="package">nuget package file</param>
		/// <returns>Status of push request</returns>
		[HttpPut]
		[ProducesResponseType(400, Type = typeof(BadRequestObjectResult))]
		public async Task<ActionResult<ResponseModel>> Push([FromForm] IFormFile package)
		{
			var result = await _packageService.Push(package);

			if (result.Success)
			{
				return Ok(result);
			}

			return BadRequest(result.Message ?? DefaultErrorMessage);
		}
		
		/// <summary>
		/// Search throw all packages in local feed 
		/// </summary>
		/// <param name="query">search query (optional)</param>
		/// <returns></returns>
		[Route("{q?}")]
		[ProducesResponseType(404, Type = typeof(NotFoundObjectResult))]
		[ProducesResponseType(400, Type = typeof(BadRequestObjectResult))]
		public async Task<ActionResult<IReadOnlyList<Package>>> Search([FromQuery(Name = "q")] string query = null)
		{
			var searchResult = await _packageService.Search(query);

			if (searchResult.Success)
			{
				return Ok(searchResult.Data);
			}

			if (searchResult.StatusCode.Equals(HttpStatusCode.NotFound))
			{
				return NotFound(searchResult.Message);
			}

			return BadRequest(searchResult.Message ?? DefaultErrorMessage);
		}

		/// <summary>
		/// Get specific package from local feed by id
		/// </summary>
		/// <param name="id">Package id</param>
		/// <returns></returns>
		[Route("package/{id}")]
		[ProducesResponseType(404, Type = typeof(NotFoundObjectResult))]
		[ProducesResponseType(400, Type = typeof(BadRequestObjectResult))]
		public async Task<ActionResult<IReadOnlyList<Package>>> PackageVersions([BindRequired, FromRoute] string id)
		{
			var result = await _packageService.PackageVersions(id);

			if (result.Success)
			{
				return Ok(result.Data);
			}

			if (result.StatusCode.Equals(HttpStatusCode.NotFound))
			{
				return NotFound(result.Message);
			}

			return BadRequest(result.Message ?? DefaultErrorMessage);
		}
	}
}