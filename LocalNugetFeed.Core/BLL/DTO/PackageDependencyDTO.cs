using System.Collections.Generic;

namespace LocalNugetFeed.Core.BLL.DTO
{
	public class PackageDependencyDTO
	{
		public string Id { get; set; }

		public string Version { get; set; }
	}
	
	public class PackageDependenciesDTO
	{
		public string TargetFramework { get; set; }

		public IReadOnlyList<PackageDependencyDTO> Dependencies { get; set; }
	}
}