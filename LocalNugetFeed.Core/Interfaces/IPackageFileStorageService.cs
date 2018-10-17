using System.Threading.Tasks;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;

namespace LocalNugetFeed.Core.Interfaces
{
	public interface IPackageFileStorageService
	{
		Task<ResponseModel> SavePackageFile(IFormFile package);
	}
}