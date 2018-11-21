using System.Collections.Generic;
using LocalNugetFeed.Core.Common;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Extensions;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Providers;

namespace LocalNugetFeed.Core.Services
{
	public class PackageSessionService : IPackageSessionService
	{
		private readonly LocalSession _session;

		public PackageSessionService(ISessionProvider<LocalSession> sessionProvider)
		{
			_session = sessionProvider.Session;
		}
		
		/// <summary>
		/// Get packages from current session
		/// </summary>
		/// <returns>list of packages</returns>
		public IReadOnlyList<Package> Get()
		{
			return _session.Current.Get<IReadOnlyList<Package>>(Constants.PackagesSessionCookieKey) ?? new List<Package>();
		}

		/// <summary>
		/// Add multiple packages to session storage
		/// </summary>
		/// <param name="packages">packages</param>
		/// <returns></returns>
		public void Set(IEnumerable<Package> packages)
		{
			if (packages != null)
			{
				_session.Current.Set(Constants.PackagesSessionCookieKey, packages);
			}
		}
		
		/// <summary>
		/// Add single package to session storage
		/// </summary>
		/// <param name="package">Package entity</param>
		/// <returns></returns>
		public void Set(Package package)
		{
			if (package == null) return;
			var packages = new List<Package>(Get()) {package};
			_session.Current.Set(Constants.PackagesSessionCookieKey, packages);
		}
	}
}