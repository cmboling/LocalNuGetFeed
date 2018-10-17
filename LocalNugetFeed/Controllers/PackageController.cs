using System.Threading.Tasks;
using LocalNugetFeed.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LocalNugetFeed.Controllers
{
	public class PackageController : Controller
	{
		private readonly IPackageFileStorageService _storageService;

		public PackageController(IPackageFileStorageService storageService)
		{
			_storageService = storageService;
		}
		
		/// <summary>
		/// Pushes a nuget package to local feed
		/// Refs: https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package
		/// </summary>
		/// <param name="package">nuget package</param>
		/// <returns>Status of push request</returns>
		public async Task Push(IFormFile package)
		{
			var result = await _storageService.SavePackageFile(package);
		}
	}
}