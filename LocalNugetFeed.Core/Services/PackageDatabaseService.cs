using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Models;

namespace LocalNugetFeed.Core.Services
{
	public class PackageDatabaseService: IPackageDatabaseService
	{
		public Task<ResponseModel> Save(Package package)
		{
			throw new System.NotImplementedException();
		}

		public Task<ResponseModel<Package>> Find(string packageId, string packageVersion)
		{
			throw new System.NotImplementedException();
		}
	}
}