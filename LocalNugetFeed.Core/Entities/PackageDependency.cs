using System.Collections.Generic;

namespace LocalNugetFeed.Core.Entities
{
	/// <summary>
	/// representation of an each dependency in package 
	/// </summary>
	public class PackageDependency
	{
		public string Id { get; set; }

		public string Version { get; set; }
	}
	
	/// <summary>
	/// Package dependencies (group target framework or another package dependency)
	/// </summary>
	public class PackageDependencies
	{
		public string TargetFramework { get; set; }

		public IReadOnlyList<PackageDependency> Dependencies { get; set; }
	}
}