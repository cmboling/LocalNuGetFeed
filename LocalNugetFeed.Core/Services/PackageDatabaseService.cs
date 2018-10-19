using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;

namespace LocalNugetFeed.Core.Services
{
	public class PackageDatabaseService: IPackageDatabaseService
	{
		public async Task<ResponseModel> Save(Package package)
		{
			// TODO
			
			return new ResponseModel(HttpStatusCode.OK); // temp stub
		}

		public async Task<ResponseModel<Package>> Find(string packageId, string packageVersion)
		{
			throw new System.NotImplementedException();
		}
	}
}