using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LocalNugetFeed.Core.Configuration;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Extensions;
using LocalNugetFeed.Core.Interfaces;
using NuGet.Packaging;

namespace LocalNugetFeed.Core.Services
{
	public class PackageFileStorageService : IPackageFileStorageService
	{
		private readonly PackagesFileStorageOptions _storageOptions;
		private readonly IMapper _mapper;

		public PackageFileStorageService(PackagesFileStorageOptions storageOptions, IMapper mapper)
		{
			_storageOptions = storageOptions;
			_mapper = mapper;
		}

		/// <summary>
		/// Save nuget package on local hard drive to predefined folder
		/// </summary>
		/// <param name="nuspecReader">nuspec reader</param>
		/// <param name="packageFileStream">package file stream</param>
		/// <returns>created package</returns>
		public async Task<Package> Save(NuspecReader nuspecReader, Stream packageFileStream)
		{
			if (packageFileStream == null)
			{
				throw new ArgumentNullException("Stream is undefined");
			}

			if (!packageFileStream.CanSeek || !packageFileStream.CanRead)
			{
				throw new InvalidDataException("Unable to seek to stream");
			}

			var packageFolderPath = Path.Combine(_storageOptions.Path, nuspecReader.PackageId(), nuspecReader.PackageVersion());
			var fullPackagePath = Path.Combine(packageFolderPath, $"{nuspecReader.PackageId()}.{nuspecReader.PackageVersion()}");

			Directory.CreateDirectory(packageFolderPath);

			if (File.Exists($"{fullPackagePath}.nupkg"))
			{
				throw new DuplicateNameException("NuGet package file with such name already exists in the feed");
			}

			using (var destinationFileStream = File.Open($"{fullPackagePath}.nupkg", FileMode.CreateNew))
			{
				packageFileStream.Seek(0, SeekOrigin.Begin);

				await packageFileStream.CopyToAsync(destinationFileStream);
			}

			return _mapper.Map<Package>(nuspecReader);
		}

		/// <summary>
		/// Read all packages from file system
		/// </summary>
		/// <returns>packages collection</returns>
		public IReadOnlyList<Package> Read()
		{
			if (!Directory.Exists(_storageOptions.Path))
			{
				throw new DirectoryNotFoundException("Packages folder not found");
			}

			var packagesPath = new DirectoryInfo(_storageOptions.Path);

			var packageFiles = packagesPath.EnumerateDirectories()
				.AsParallel()
				.SelectMany(di =>
				{
					var nupkgFiles = di.EnumerateFiles("*.nupkg", SearchOption.AllDirectories).Select(package =>
					{
						using (var reader = new PackageArchiveReader(package.FullName))
						{
							return reader.NuspecReader;
						}
					});

					return nupkgFiles;
				});

			return _mapper.Map<IReadOnlyList<Package>>(packageFiles);
		}
	}
}