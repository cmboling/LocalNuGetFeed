namespace LocalNugetFeed.Core.Common
{
	public class Constants
	{
		public const string PackagesSessionCookieKey = "_Packages";
		public const string PackagesFileStorage = nameof(PackagesFileStorage);
		public const string DefaultPackagesDirectory = "Packages";
		public const string NuGetPushRelativeUrl = "v2/package";
		public const string NuGetPushActionName = "upload";
	}
}