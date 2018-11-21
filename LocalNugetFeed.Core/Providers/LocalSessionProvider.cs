using LocalNugetFeed.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LocalNugetFeed.Core.Providers
{
	public class LocalSessionProvider : ISessionProvider<LocalSession>
	{
		private LocalSession _session;

		public LocalSession Session => _session;

		public LocalSessionProvider(IHttpContextAccessor accessor)
		{
			_session.Current = accessor.HttpContext.Session;
		}
	}
	
	public struct LocalSession
	{
		public ISession Current { get; set; }

		public LocalSession(ISession session)
		{
			Current = session;
		}
	}
}