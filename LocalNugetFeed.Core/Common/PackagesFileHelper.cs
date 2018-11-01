using System.IO;

namespace LocalNugetFeed.Core.Common
{
	public static class PackagesFileHelper
	{
		/// <summary>
		/// Get packages folder full path
		/// </summary>
		/// <param name="folderPath">Folder path</param>
		/// <returns></returns>
		public static string GetPackagesFolderPath(string folderPath)
		{
			var packagesFolderPath = string.IsNullOrWhiteSpace(folderPath) ? GetDefaultPackagesFolderFullPath() : folderPath;

			if (!Path.IsPathRooted(packagesFolderPath))
			{
				throw new DirectoryNotFoundException("Folder root not found");
			}
				
			Directory.CreateDirectory(packagesFolderPath);
			
			return packagesFolderPath;
		}

		/// <summary>
		/// Get packages default folder full path
		/// </summary>
		/// <returns>full packages folder path</returns>
		public static string GetDefaultPackagesFolderFullPath()
		{
			return $@"{Directory.GetCurrentDirectory()}\{Constants.DefaultPackagesDirectory}";
		}
	}
}