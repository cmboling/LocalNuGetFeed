using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;
using Microsoft.AspNetCore.Http;

namespace LocalNugetFeed.Core.Services
{
	public class PackageFileStorageService : IPackageFileStorageService
	{
		public async Task<ResponseModel> SavePackageFile(IFormFile package)
		{
			if (package == null)
			{
				return new ResponseModel(HttpStatusCode.BadRequest, "Package file not found");
			}

			using (var packageData = package.OpenReadStream())
			{
				// TODO:
				// 1. check that package doesn't exists in local feed
				// 2. save it locally on hard drive if it's not exist
				// 3. to index the made changes in local database for further search by packages in db
			}

			return new ResponseModel(HttpStatusCode.OK);
		}
	}
}