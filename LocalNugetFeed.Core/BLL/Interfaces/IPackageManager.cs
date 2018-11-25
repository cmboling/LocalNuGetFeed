using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LocalNugetFeed.Core.BLL.DTO;

namespace LocalNugetFeed.Core.BLL.Interfaces
{
	public interface IPackageManager
	{
		/// <summary>
		/// Push package to the local feed
		/// </summary>
		/// <param name="sourceFileStream">nuget package stream</param>
		/// <returns>response with result</returns>
		Task<ResponseDTO<PackageDTO>> Push(Stream sourceFileStream);

		/// <summary>
		/// Checks that package exists or not
		/// </summary>
		/// <param name="id">package id</param>
		/// <param name="version">package version</param>
		/// <returns>true/false</returns>		
		Task<bool> PackageExists(string id, string version);

		/// <summary>
		/// Get package(s) by id
		/// </summary>
		/// <param name="id">package id</param>
		/// <returns>response with result</returns>		
		Task<ResponseDTO<IReadOnlyList<PackageVersionsDTO>>> GetPackageVersions(string id);

		/// <summary>
		/// Search packages by query in local feed 
		/// </summary>
		/// <param name="query">search query (optional)</param>
		/// <returns>response with result</returns>		
		Task<ResponseDTO<IReadOnlyList<PackageDTO>>> Search(string query = null);
	}
}