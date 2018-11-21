using System.Collections.Generic;

namespace LocalNugetFeed.Core.BLL.DTO
{
	public class PackageVersionsDTO : PackageDTO
	{
		public string Authors { get; set; }

		public IReadOnlyList<PackageDependenciesDTO> PackageDependencies { get; set; }
	}
}