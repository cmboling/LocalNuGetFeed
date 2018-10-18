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
		Task<ResponseModel> SavePackageFile(NuspecReader packageNuspec, Stream sourceFileStream);
	}
}