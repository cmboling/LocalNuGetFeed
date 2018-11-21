using System;
using System.Linq;
using AutoMapper;
using LocalNugetFeed.Core.BLL.DTO;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Extensions;
using NuGet.Packaging;

namespace LocalNugetFeed.Core.Configuration
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile()
		{
			CreateMap<Package, PackageDTO>();
			
			CreateMap<Package, PackageVersionsDTO>();
			
			CreateMap<PackageDependency, PackageDependencyDTO>();
			
			CreateMap<PackageDependencies, PackageDependenciesDTO>();

			CreateMap<NuspecReader, Package>()
				.ForMember(dest => dest.Id, opts => opts.MapFrom(src => src.PackageId()))
				.ForMember(dest => dest.Version, opts => opts.MapFrom(src => src.PackageVersion()))
				.ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.GetDescription()))
				.ForMember(dest => dest.Authors, opts => opts.MapFrom(src => src.GetAuthors()))
				.ForMember(dest => dest.PackageDependencies, opts => opts.MapFrom(src => src.GetDependencyGroups().Select(x => new PackageDependencies()
				{
					TargetFramework = x.TargetFramework != null ? x.TargetFramework.DotNetFrameworkName : String.Empty,
					Dependencies = x.Packages.Select(z => new PackageDependency()
					{
						Id = z.Id,
						Version = z.VersionRange.ToNormalizedString()
					}).ToList()
				}).ToList()));
		}
	}

	public static class AutoMapperConfiguration
	{
		public static MapperConfiguration Configure()
		{
			return new MapperConfiguration(cfg =>
			{
				cfg.AddProfile<AutoMapperProfile>();
			});
		}
	}
}