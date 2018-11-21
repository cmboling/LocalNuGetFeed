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
		/// Get packages from session or file system (if session is empty)
		/// </summary>
		/// <returns>packages</returns>
		Task<IReadOnlyList<Package>> GetPackages();

	}
}