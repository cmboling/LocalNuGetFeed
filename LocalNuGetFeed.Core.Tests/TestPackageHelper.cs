using System.Collections.Generic;
using System.IO;
using LocalNugetFeed.Core.Common;
using LocalNugetFeed.Core.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace LocalNuGetFeed.Core.Tests
{
	public static class TestPackageHelper
	{
		public const string GetOSVersionPackageId = "GetOSVersion";
		public const string GetOSVersionPackageVersion = "1.0.0";
		public const string MyTestPackageId = "MyTestPackage";
		public const string SomePackageDependencyId = "SomePackageDependency";

		/// <summary>
		/// Clean directory with test packages
		/// </summary>
		/// <param name="path">packages folder path</param>
		public static void CleanPackagesDefaultDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				DirectoryInfo di = new DirectoryInfo(path);

				foreach (var dir in di.GetDirectories())
				{
					dir.Delete(true);
				}
			}
		}

		/// <summary>
		/// Setup mock file using a memory stream
		/// </summary>
		/// <returns>Mock file</returns>
		public static IFormFile GetMockFile(string content, string fileName)
		{
			var fileMock = new Mock<IFormFile>();

			var ms = new MemoryStream();
			var writer = new StreamWriter(ms);
			writer.Write(content ?? "some content");
			writer.Flush();
			ms.Position = 0;
			fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
			fileMock.Setup(_ => _.FileName).Returns(fileName);
			fileMock.Setup(_ => _.Length).Returns(ms.Length);

			return fileMock.Object;
		}

		/// <summary>
		/// Mock Package entity
		/// </summary>
		/// <returns></returns>
		public static Package GetMockPackage()
		{
			return new Package()
			{
				Id = "MyPackageTest",
				Version = "1.0.0"
			};
		}

		/// <summary>
		/// Get full path of getosversion nuget package which is included to the project
		/// </summary>
		/// <returns></returns>
		public static string GetOSVersionPackageFilePath()
		{
			var directoryInfo = Directory.GetParent(Directory.GetCurrentDirectory()).Parent;
			if (directoryInfo != null)
			{
				var physicalFile =
					new FileInfo(
						$@"{directoryInfo.Parent}\{Constants.DefaultPackagesDirectory}\{GetOSVersionPackageId}\{GetOSVersionPackageVersion}\{GetOSVersionPackageId}.{GetOSVersionPackageVersion}.nupkg");

				return physicalFile.FullName;
			}

			return null;
		}


		/// <summary>
		/// Get GetOSVersion Package entity
		/// </summary>
		/// <returns></returns>
		public static Package GetOSVersionPackage()
		{
			return new Package()
			{
				Id = GetOSVersionPackageId,
				Version = GetOSVersionPackageVersion
			};
		}

		public static IReadOnlyList<Package> TestPackages => new List<Package>()
		{
			new Package()
			{
				Id = MyTestPackageId,
				Description = "Package description",
				Authors = "D.B.",
				Version = "1.0.0"
			},
			new Package()
			{
				Id = MyTestPackageId,
				Description = "Package description",
				Authors = "D.B.",
				Version = "1.0.1",
				PackageDependencies = new List<PackageDependencies>()
				{
					new PackageDependencies()
					{
						TargetFramework = ".Net Core v.2.1",
						Dependencies = new List<PackageDependency>()
						{
							new PackageDependency()
							{
								Id = "SomePackageDependency",
								Version = "1.1.1"
							}
						}
					}
				}
			}
		};
	}
}