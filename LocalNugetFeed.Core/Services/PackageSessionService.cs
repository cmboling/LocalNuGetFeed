using System.Collections.Generic;
using LocalNugetFeed.Core.Common;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Extensions;
using LocalNugetFeed.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LocalNugetFeed.Core.Services
{
	public class PackageSessionService : IPackageSessionService
	{
		private readonly ISession _session;

		public PackageSessionService(IHttpContextAccessor accessor)
		{
			_session = accessor.HttpContext.Session;
		}
		
		/// <summary>
		/// Get packages from current session
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<Package> Get()
		{
			return _session.Get<IReadOnlyList<Package>>(Constants.PackagesSessionCookieKey) ?? new List<Package>();
		}

		/// <summary>
		/// Add multiple packages to session storage
		/// </summary>
		/// <param name="packages">packages</param>
		public void Set(IEnumerable<Package> packages)
		{
			if (packages != null)
			{
				_session.Set(Constants.PackagesSessionCookieKey, packages);
			}
		}
		
		/// <summary>
		/// Add single package to session storage
		/// </summary>
		/// <param name="package">Package entity</param>
		public void Set(Package package)
		{
			var packages = new List<Package>(Get()) {package};
			_session.Set(Constants.PackagesSessionCookieKey, packages);
		}
	}
}