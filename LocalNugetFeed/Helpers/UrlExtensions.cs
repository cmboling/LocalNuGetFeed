using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LocalNugetFeed.Web.Helpers
{
	public static class UrlExtensions
	{
		public static string AbsoluteRouteUrl(this IUrlHelper url, string routeName, object routeValues = null)
			=> url.RouteUrl(routeName, routeValues, url.ActionContext.HttpContext.Request.Scheme);
		
	}
}
