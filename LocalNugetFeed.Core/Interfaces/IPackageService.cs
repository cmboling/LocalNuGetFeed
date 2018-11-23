using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using NuGet.Packaging;

namespace LocalNugetFeed.Core.Interfaces
{
	public interface IPackageService
	{
		/// <summary>
		/// Pushes package to the local feed
		/// </summary>
		/// <param name="nuspecReader">nuspec reader</param>
		/// <param name="packageFileStream">package file stream</param>
		/// <returns>Package</returns>
		Task<Package> Push(NuspecReader nuspecReader, Stream packageFileStream);

		/// <summary>
		/// Get packages from local feed
		/// </summary>
		/// <param name="onlyLastVersion">Boolean flag which is determines - return all versions of an each package or only last</param>
		/// <returns>packages</returns>
		Task<IReadOnlyList<Package>> GetPackages(bool onlyLastVersion = false);

		/// <summary>
		/// Get package versions from local feed
		/// </summary>
		/// <returns>packages</returns>
		Task<IReadOnlyCollection<Package>> GetPackageVersions(string id);

		/// <summary>
		/// Search packages by query
		/// </summary>
		/// <param name="query">search query (required)</param>
		/// <returns>packages</returns>	
		Task<IReadOnlyCollection<Package>> Search(string query);

		/// <summary>
		/// Checks that package exists or not
		/// </summary>
		/// <param name="id">package id</param>
		/// <param name="version">package version</param>
		/// <returns>true/false</returns>		
		Task<bool> PackageExists(string id, string version);
	}
}