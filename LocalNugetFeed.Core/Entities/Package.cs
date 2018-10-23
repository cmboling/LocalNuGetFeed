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
	}
}