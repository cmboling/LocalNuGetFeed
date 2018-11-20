using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LocalNugetFeed.Core.ConfigurationOptions;
using LocalNugetFeed.Core.Entities;
using LocalNugetFeed.Core.Extensions;
using LocalNugetFeed.Core.Interfaces;
using NuGet.Packaging;

namespace LocalNugetFeed.Core.Services
{
	public class PackageFileStorageService : IPackageFileStorageService
	{
		private readonly PackagesFileStorageOptions _storageOptions;

		public PackageFileStorageService(PackagesFileStorageOptions storageOptions)
		{
			_storageOptions = storageOptions;
		}

		/// <summary>
		/// Save nuget package on local hard drive to predefined folder
		/// </summary>
		/// <param name="nuspecReader">nuspec reader</param>
		/// <param name="packageFileStream">package file stream</param>
		/// <returns>created package</returns>
		public async Task<Package> Save(NuspecReader nuspecReader, Stream packageFileStream)
		{
			var packageFolderPath = Path.Combine(_storageOptions.Path, nuspecReader.PackageId(), nuspecReader.PackageVersion());
			var fullPackagePath = Path.Combine(packageFolderPath, $"{nuspecReader.PackageId()}.{nuspecReader.PackageVersion()}");

			Directory.CreateDirectory(packageFolderPath);

			using (var destinationFileStream = File.Open($"{fullPackagePath}.nupkg", FileMode.CreateNew))
			{
				packageFileStream.Seek(0, SeekOrigin.Begin);

				await packageFileStream.CopyToAsync(destinationFileStream);
			}

			return MapNuspecDataToPackage(nuspecReader);
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

			var result = new List<Package>();
			var packagesPaths = Directory.GetDirectories(_storageOptions.Path);

			// since all packages are storing according with 2-level hierarchy (packageRootDirectory -> packageVersionDirectory) we should lookup using nested loop by package version, and we will ignore other packages 
			foreach (var packageRootPath in packagesPaths)
			{
				var packageVersionPaths = Directory.GetDirectories(packageRootPath);

				foreach (var packageVersionPath in packageVersionPaths)
				{
					var packageFileName = Directory.GetFiles(packageVersionPath, "*.nupkg").FirstOrDefault();
					if (!string.IsNullOrWhiteSpace(packageFileName))
					{
						using (var reader = new PackageArchiveReader(packageFileName))
						{
							var packageNuspec = reader.NuspecReader;

							result.Add(MapNuspecDataToPackage(packageNuspec));
						}
					}
				}
			}

			return result;
		}

		private static Package MapNuspecDataToPackage(NuspecReader packageNuspec)
		{
			return new Package()
			{
				Id = packageNuspec.PackageId(),
				Version = packageNuspec.PackageVersion(),
				Description = packageNuspec.GetDescription(),
				Authors = packageNuspec.GetAuthors(),
				PackageDependencies = packageNuspec.GetDependencyGroups().Select(x => new PackageDependencies()
				{
					TargetFramework = x.TargetFramework?.DotNetFrameworkName,
					Dependencies = x.Packages.Select(z => new PackageDependency()
					{
						Id = z.Id,
						Version = z.VersionRange.ToNormalizedString()
					}).ToList()
				}).ToList()
			};
		}
	}
}