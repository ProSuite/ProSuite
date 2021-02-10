using System;
using System.IO;
using System.IO.Compression;

namespace ProSuite.Commons.IO
{
	public static class ZipUtils
	{
		/// <summary>
		/// Similar to the extension like-named method in ZipFileExtensions
		/// but also accepts a callback that is invoked for each entry
		/// prior to extraction. The entry is only extracted, if the
		/// callback returns true. Allows for filtering and progress tracking.
		/// </summary>
		public static void ExtractToDirectory(this ZipArchive archive, string targetDirectory,
		                                      Func<string, bool> callback = null)
		{
			if (archive == null)
				throw new ArgumentNullException(nameof (archive));
			if (targetDirectory == null)
				throw new ArgumentNullException(nameof(targetDirectory));

			string targetPath = Directory.CreateDirectory(targetDirectory).FullName;

			foreach (ZipArchiveEntry entry in archive.Entries)
			{
				if (callback != null && ! callback(entry.FullName))
				{
					continue;
				}

				string fullPath = Path.GetFullPath(Path.Combine(targetPath, entry.FullName));

				if (!fullPath.StartsWith(targetPath, StringComparison.OrdinalIgnoreCase))
					throw new IOException("Zip archive attempts to extract outside target directory");

				if (Path.GetFileName(fullPath).Length == 0)
				{
					if (entry.Length != 0L)
						throw new IOException("Zip archive has directory name with data");
					Directory.CreateDirectory(fullPath);
				}
				else
				{
					var parentPath = Path.GetDirectoryName(fullPath);
					if (parentPath != null)
						Directory.CreateDirectory(parentPath);
					entry.ExtractToFile(fullPath);
				}
			}
		}

		/// <remarks>
		/// Equivalent to the method of the same name in ZipFileExtensions,
		/// put spares us a reference to System.IO.Compression.FileSystem.dll,
		/// which has not this name in all environments.
		/// </remarks>
		public static void ExtractToFile(this ZipArchiveEntry source, string targetFile,
		                                 bool overwrite = false)
		{
			if (source == null)
				throw new ArgumentNullException(nameof (source));
			if (targetFile == null)
				throw new ArgumentNullException(nameof (targetFile));

			FileMode mode = overwrite ? FileMode.Create : FileMode.CreateNew;

			using (Stream destination = File.Open(targetFile, mode, FileAccess.Write, FileShare.None))
			{
				using (Stream stream = source.Open())
				{
					stream.CopyTo(destination);
				}
			}

			File.SetLastWriteTime(targetFile, source.LastWriteTime.DateTime);
		}
	}
}
