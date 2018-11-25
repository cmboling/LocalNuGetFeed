using NuGet.Packaging;

namespace LocalNugetFeed.Core.Extensions
{
	public static class PackageExtensions
	{
		public static string PackageId(this NuspecReader nuspec) => nuspec.GetId();
		
		public static string PackageVersion(this NuspecReader nuspec) => nuspec.GetVersion().ToNormalizedString();
	}
}