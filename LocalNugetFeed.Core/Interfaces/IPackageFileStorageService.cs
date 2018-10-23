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
		/// Save nuget package and nuspec metadata on local hard drive to an according folder
		/// </summary>
		/// <param name="packageReader">package reader</param>
		/// <param name="packageFileStream">package file stream</param>
		/// <returns>response status info</returns>
		Task<ResponseModel<Package>> Save(PackageArchiveReader packageReader, Stream packageFileStream);

		ResponseModel<IReadOnlyList<Package>> Read();
	}
}