using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;
using NuGet.Packaging;

namespace LocalNugetFeed.Core.Interfaces
{
	public interface IPackageFileStorageService
	{
		/// <summary>
		/// Save nuget package on local hard drive to predefined folder
		/// </summary>
		/// <param name="nuspecReader">nuspec reader</param>
		/// <param name="packageFileStream">package file stream</param>
		/// <returns>created package</returns>
		Task<Package> Save(NuspecReader nuspecReader, Stream packageFileStream);

		/// <summary>
		/// Read all packages from file system
		/// </summary>
		/// <returns>packages collection</returns>
		IReadOnlyList<Package> Read();
	}
}