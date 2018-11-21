using System;
using Newtonsoft.Json;

namespace LocalNugetFeed.Web.Models
{
	/// <summary>
	/// Model of NuGet Package Resource
	/// Refs - https://docs.microsoft.com/en-us/nuget/api/service-index#resources
	/// </summary>
	public class NuGetPackageResourceModel
	{
		public NuGetPackageResourceModel(string type, string id, string comment = null)
		{
			Id = id ?? throw new ArgumentNullException("Package Id is undefined");
			Type = type ?? throw new ArgumentNullException("Package Type is undefined");
			Comment = comment ?? string.Empty;
		}

		[JsonProperty(PropertyName = "@id")]
		public string Id { get; }

		[JsonProperty(PropertyName = "@type")]
		public string Type { get; }

		public string Comment { get; }

	}
}
