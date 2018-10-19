using System.IO;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace LocalNugetFeed.Core.Extensions
{
	public static class PackageExtensions
	{
		public static string PackageId(this NuspecReader nuspec) => nuspec.GetId().ToLowerInvariant();
		
		public static string PackageVersion(this NuspecReader nuspec) => nuspec.GetVersion().ToNormalizedString().ToLowerInvariant();
	}
}