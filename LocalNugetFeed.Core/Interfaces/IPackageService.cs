using System.Collections.Generic;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;

namespace LocalNugetFeed.Core.Interfaces
{
	public interface IPackageService
	{
		/// <summary>
		/// Push package to the local feed
		/// </summary>
		/// <param name="packageFile">nuget package</param>
		/// <returns>response with result</returns>
		Task<ResponseModel> Push(IFormFile packageFile);

		/// <summary>
		/// Get package by id and version in local feed
		/// </summary>
		/// <param name="id">package id</param>
		/// <param name="version">package version</param>
		/// <returns>response with result</returns>	
		Package Get(string id, string version);

		/// <summary>
		/// Get package(s) by id
		/// </summary>
		/// <param name="id">package id</param>
		/// <returns>response with result</returns>		
		ResponseModel<IReadOnlyList<Package>> PackageVersions(string id);

		/// <summary>
		/// Search packages by query in local feed (session/file system)
		/// </summary>
		/// <param name="query">search query</param>
		/// <returns>response with result</returns>		
		Task<ResponseModel<IReadOnlyList<Package>>> Search(string query);

	}
}