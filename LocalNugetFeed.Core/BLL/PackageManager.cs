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

		/// <summary>
		/// Push package to the local feed
		/// </summary>
		/// <param name="sourceFileStream">nuget package stream</param>
		/// <returns>response with result</returns>
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

		/// <summary>
		/// Get package by id and version
		/// </summary>
		/// <param name="id">package id</param>
		/// <param name="version">package version</param>
		/// <returns>response with result</returns>		
		public async Task<bool> PackageExists(string id, string version)
		{
			if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException("Package Id and Version are required");
			}

			var packages = await GetPackages();

			var package = packages.FirstOrDefault(x =>
				x.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
				x.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

			return package != null;
		}

		/// <summary>
		/// Get package(s) by id
		/// </summary>
		/// <param name="id">package id</param>
		/// <returns>response with result</returns>		
		public async Task<ResponseDTO<IReadOnlyList<PackageVersionsDTO>>> PackageVersions(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentNullException("Package Id is required");
			}

			var packages = await _packageService.GetPackages();

			var packageVersions = packages.Where(x =>
				x.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).OrderByDescending(z => new NuGetVersion(z.Version)).ToList();

			return packageVersions.Any()
				? new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(_mapper.Map<IReadOnlyList<PackageVersionsDTO>>(packageVersions))
				: new ResponseDTO<IReadOnlyList<PackageVersionsDTO>>(HttpStatusCode.NotFound, $"Package Id [{id}] not found");
		}

		/// <summary>
		/// Search packages by query in local feed (session/file system)
		/// </summary>
		/// <param name="query">search query (optional)</param>
		/// <returns>response with result</returns>		
		public async Task<ResponseDTO<IReadOnlyList<PackageDTO>>> Search(string query = null)
		{
			var packages = await GetPackages();

			if (!string.IsNullOrEmpty(query))
			{
				packages = packages.Where(x =>
					x.Id.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
				if (!packages.Any())
				{
					return new ResponseDTO<IReadOnlyList<PackageDTO>>(HttpStatusCode.NotFound, "No any packages matching to your request");
				}
			}

			var result = packages.OrderByDescending(s => new NuGetVersion(s.Version))
				.GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase)
				.Select(z => _mapper.Map<PackageDTO>(z.First()))
				.ToList();

			return new ResponseDTO<IReadOnlyList<PackageDTO>>(result);
		}

		/// <summary>
		/// Get packages from session or file system (if session is empty)
		/// </summary>
		/// <returns>packages</returns>
		public async Task<IReadOnlyList<PackageDTO>> GetPackages()
		{
			var packages = await _packageService.GetPackages();

			return _mapper.Map<IReadOnlyList<PackageDTO>>(packages);
		}
	}
}