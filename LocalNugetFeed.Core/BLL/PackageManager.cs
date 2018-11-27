using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using LocalNugetFeed.Core.BLL.DTO;
using LocalNugetFeed.Core.BLL.Interfaces;
using LocalNugetFeed.Core.Extensions;
using LocalNugetFeed.Core.Interfaces;
using NuGet.Packaging;
using NuGet.Versioning;

namespace LocalNugetFeed.Core.BLL
{
	public class PackageManager : IPackageManager
	{
		private readonly IPackageService _packageService;
		private readonly IMapper _mapper;

		public PackageManager(IPackageService packageService, IMapper mapper)
		{
			_packageService = packageService;
			_mapper = mapper;
		}

		///<inheritdoc cref="IPackageManager.Push"/>	
		public async Task<ResponseDTO<PackageDTO>> Push(Stream sourceFileStream)
		{
			if (sourceFileStream == null)
			{
				throw new ArgumentNullException("Stream is undefined");
			}

			if (!sourceFileStream.CanSeek || !sourceFileStream.CanRead)
			{
				throw new InvalidDataException("Unable to seek to stream");
			}

			using (var reader = new PackageArchiveReader(sourceFileStream))
			{
				var packageNuspec = reader.NuspecReader;

				// step 1. we should make sure that package doesn't exists in local feed
				var packageExists = await PackageExists(packageNuspec.PackageId(), packageNuspec.PackageVersion());
				if (packageExists)
				{
					return new ResponseDTO<PackageDTO>(HttpStatusCode.Conflict,
						$"Package {packageNuspec.PackageId()} v{packageNuspec.PackageVersion()} already exists in the local feed");
				}

				// step 2. now we can push package
				var package = await _packageService.Push(packageNuspec, sourceFileStream);

				return package != null
					? new ResponseDTO<PackageDTO>(_mapper.Map<PackageDTO>(package))
					: new ResponseDTO<PackageDTO>(HttpStatusCode.BadRequest, "Unable to push package. See logs.");
			}
		}

		///<inheritdoc cref="IPackageManager.PackageExists"/>	
		public async Task<bool> PackageExists(string id, string version)
		{
			if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException("Package Id and Version are required");
			}

			return await _packageService.PackageExists(id, version);
		}

		///<inheritdoc cref="IPackageManager.GetPackageVersions"/>	
		public async Task<ResponseDTO<IReadOnlyList<PackageVersionsDTO>>> GetPackageVersions(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentNullException("Package Id is required");
			}

			var packages = await _packageService.GetPackageVersions(id);

			if (!packages.Any())
			{
				return new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(HttpStatusCode.NotFound, $"Package Id [{id}] not found");
			}

			var packageVersions = packages.OrderByDescending(z => new NuGetVersion(z.Version));

			return packageVersions.Any()
				? new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(_mapper.Map<IReadOnlyList<PackageVersionsDTO>>(packageVersions))
				: new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(HttpStatusCode.NotFound, $"Package Id [{id}] not found");
		}

		///<inheritdoc cref="IPackageManager.Search"/>	
		public async Task<ResponseDTO<IReadOnlyList<PackageDTO>>> Search(string query = null)
		{
			if (string.IsNullOrEmpty(query))
			{
				var allPackages = await _packageService.GetPackages(true);
				
				return new ResponseDTO<IReadOnlyList<PackageDTO>>(_mapper.Map<IReadOnlyList<PackageDTO>>(allPackages));
			}

			var packages = await _packageService.Search(query);

			if (!packages.Any())
			{
				return new ResponseDTO<IReadOnlyList<PackageDTO>>(HttpStatusCode.NotFound, "No any packages matching to your request");
			}
			
			return new ResponseDTO<IReadOnlyList<PackageDTO>>(_mapper.Map<IReadOnlyList<PackageDTO>>(packages));
		}
	}
}