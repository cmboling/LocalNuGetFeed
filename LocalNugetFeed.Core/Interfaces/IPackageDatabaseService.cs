using System.Threading.Tasks;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Models;

namespace LocalNugetFeed.Core.Interfaces
{
	public interface IPackageDatabaseService
	{
		Task<ResponseModel> Save(Package package);
		
		Task<ResponseModel<Package>> Find(string id, string version);
	}
}