using System.IO;
using LocalNugetFeed.Core.Common;
using LocalNugetFeed.Core.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace LocalNuGetFeed.Core.Tests
{
	public static class TestPackageHelper
	{
		public const string TestPackageId = "GetOSVersion";
		public const string TestPackageVersion = "1.0.0";
		
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
			writer.Write(content);
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
				var physicalFile = new FileInfo($@"{directoryInfo.Parent}\{Constants.DefaultPackagesDirectory}\{TestPackageId}\{TestPackageVersion}\{TestPackageId}.{TestPackageVersion}.nupkg");

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
				Id = TestPackageId,
				Version = TestPackageVersion
			};
		}

	}
}