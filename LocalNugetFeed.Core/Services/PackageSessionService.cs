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
		
		///<inheritdoc cref="IPackageSessionService.Get"/>	
		public IReadOnlyList<Package> Get()
		{
			return _session.Current.Get<IReadOnlyList<Package>>(Constants.PackagesSessionCookieKey) ?? new List<Package>();
		}

		///<inheritdoc cref="IPackageSessionService.SetRange"/>	
		public void SetRange(IEnumerable<Package> packages)
		{
			if (packages != null)
			{
				_session.Current.Set(Constants.PackagesSessionCookieKey, packages);
			}
		}
		
		///<inheritdoc cref="IPackageSessionService.Set"/>	
		public void Set(Package package)
		{
			if (package == null) return;
			var packages = new List<Package>(Get()) {package};
			_session.Current.Set(Constants.PackagesSessionCookieKey, packages);
		}
	}
}