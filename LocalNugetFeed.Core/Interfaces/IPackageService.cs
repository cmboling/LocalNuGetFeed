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
		/// Find package in local feed
		/// </summary>
		/// <param name="id">package id</param>
		/// <param name="version">package version</param>
		/// <returns>response with result</returns>		
		Task<Package> Find(string id, string version);
	}
}