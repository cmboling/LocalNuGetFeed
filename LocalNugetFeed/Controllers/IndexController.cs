using System.Collections.Generic;
using LocalNugetFeed.Core.Models;
using LocalNugetFeed.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LocalNugetFeed.Controllers
{
	public class IndexController : Controller
	{
		/// <summary>
		/// Init of NuGet package source  ../v3/index.json
		/// Refs: https://docs.microsoft.com/en-us/nuget/api/service-index, 
		/// https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package
		/// </summary>
		/// <returns>Json file of packages store</returns>
		[HttpGet]
		public object Get()
		{
			var feedInfo = new
			{
				Version = "3.0.0",
				Resources = new List<NuGetPackageResourceModel>()
				{
					new NuGetPackageResourceModel("PackagePublish/2.0.0", Url.AbsoluteRouteUrl("upload"))
				}
			};

			return feedInfo;
		}
	}
}