using System;
using System.Collections.Generic;

namespace LocalNugetFeed.Core.Entities
{
	/// <summary>
	/// Representation model of nuget package 
	/// </summary>
	public class Package
	{
		public string Id { get; set; }

		public string Version { get; set; }

		public string Description { get; set; }

		public string Authors { get; set; }

		public IReadOnlyList<PackageDependencies> PackageDependencies { get; set; }
	}
	
	public class PackageComparer: IEqualityComparer<Package>
	{
		public bool Equals(Package x, Package y)
		{
			return string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Version, y.Version, StringComparison.OrdinalIgnoreCase);
		}

		public int GetHashCode(Package obj)
		{
			unchecked
			{
				return (StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Id) * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Version);
			}
		}
	}
}