using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.IO
{
	public static class FileSystemUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly Dictionary<string, Environment.SpecialFolder>
			_specialFolderNames =
				new Dictionary<string, Environment.SpecialFolder>
				{
					{ "APPDATA", Environment.SpecialFolder.ApplicationData },
					{ "LOCALAPPDATA", Environment.SpecialFolder.LocalApplicationData },
					{ "PROGRAMDATA", Environment.SpecialFolder.CommonApplicationData }
				};

		private static readonly string[] _variableFormats = { @"${{{0}}}", "%{0}%" };
		private static char[] _invalidPathChars;
		private static char[] _invalidFileNameChars;

		[NotNull]
		public static char[] InvalidPathChars
			=> _invalidPathChars ?? (_invalidPathChars = GetWindowsInvalidPathChars());

		[NotNull]
		public static char[] InvalidFileNameChars
			=> _invalidFileNameChars ??
			   (_invalidFileNameChars = Assert.NotNull(Path.GetInvalidFileNameChars()));

		/// <summary>
		/// Creates a relative path from one file or folder to another.
		/// </summary>
		/// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
		/// <param name="toPath">Contains the path to a  file or folder that defines the endpoint of the relative path.</param>
		/// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
		/// <remarks>Adapted from https://stackoverflow.com/a/340454 </remarks>
		[NotNull]
		public static string GetRelativePath([NotNull] string fromPath,
		                                     [NotNull] string toPath)
		{
			Assert.ArgumentNotNullOrEmpty(fromPath, nameof(fromPath));
			Assert.ArgumentNotNullOrEmpty(toPath, nameof(toPath));

			var fromUri = new Uri(fromPath);
			var toUri = new Uri(toPath);

			Assert.ArgumentCondition(fromUri.Scheme.Equals(Uri.UriSchemeFile),
			                         "fromPath is not Uri.UriSchemeFile");
			Assert.ArgumentCondition(toUri.Scheme.Equals(Uri.UriSchemeFile),
			                         "toPath is not Uri.UriSchemeFile");

			Uri relativeUri = fromUri.MakeRelativeUri(toUri);
			string relativePath = Uri.UnescapeDataString(relativeUri.OriginalString);

			return relativePath.Replace(Path.AltDirectorySeparatorChar,
			                            Path.DirectorySeparatorChar);
		}

		public static bool HasInvalidPathChars([NotNull] string path)
		{
			return path.IndexOfAny(InvalidPathChars) >= 0;
		}

		public static bool HasInvalidFileNameChars([NotNull] string fileName)
		{
			return fileName.IndexOfAny(InvalidFileNameChars) >= 0;
		}

		[NotNull]
		public static string ReplaceInvalidFileNameChars([NotNull] string fileName,
		                                                 char replacementChar)
		{
			return StringUtils.ReplaceChars(fileName, replacementChar,
			                                InvalidFileNameChars);
		}

		[NotNull]
		public static string ReplaceInvalidPathChars([NotNull] string path,
		                                             char replacementChar)
		{
			return StringUtils.ReplaceChars(path, replacementChar, InvalidPathChars);
		}

		public static string FindFile([NotNull] IEnumerable<string> directories,
		                              [NotNull] string fileName)
		{
			Assert.ArgumentNotNull(directories, nameof(directories));
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			foreach (string searchDirectory in directories)
			{
				string fullPath = Path.Combine(searchDirectory, fileName);

				if (File.Exists(fullPath))
				{
					return fullPath;
				}
			}

			return null;
		}

		public static IEnumerable<string> EnumerateFiles(
			[NotNull] string directory,
			[NotNull] string searchString,
			SearchOption searchOption = SearchOption.TopDirectoryOnly)
		{
			// NOTE: DirectoryInfo.EnumerateFiles is not available in .NET 3.5
			foreach (string fileName in Directory.GetFiles(directory, searchString, searchOption))
			{
				yield return fileName;
			}
		}

		/// <summary>
		/// Copies the entire content of the specified source directory to a given target directory path.
		/// </summary>
		/// <param name="sourceDirectoryPath">The source directory.</param>
		/// <param name="targetDirectoryPath">The target director path.</param>
		/// <param name="overwriteFiles">if set to <c>true</c> any copied files that already exist in 
		/// the target directory are overwritten. If the file exists and overwriteFiles is <c>false</c>, 
		/// an <see cref="IOException"></see> is thrown.</param>
		public static void CopyDirectory([NotNull] string sourceDirectoryPath,
		                                 [NotNull] string targetDirectoryPath,
		                                 bool overwriteFiles)
		{
			Assert.ArgumentNotNullOrEmpty(sourceDirectoryPath, nameof(sourceDirectoryPath));
			Assert.ArgumentNotNullOrEmpty(targetDirectoryPath, nameof(targetDirectoryPath));

			CopyDirectory(new DirectoryInfo(sourceDirectoryPath),
			              new DirectoryInfo(targetDirectoryPath), overwriteFiles);
		}

		/// <summary>
		/// Copies the entire content of the specified source directory to a given target directory path.
		/// </summary>
		/// <param name="source">The source directory.</param>
		/// <param name="target">The target director path.</param>
		/// <param name="overwriteFiles">if set to <c>true</c> any copied files that already exist in 
		/// the target directory are overwritten. If the file exists and overwriteFiles is <c>false</c>, 
		/// an <see cref="IOException"></see> is thrown.</param>
		public static void CopyDirectory([NotNull] DirectoryInfo source,
		                                 [NotNull] DirectoryInfo target,
		                                 bool overwriteFiles)
		{
			Assert.ArgumentNotNull(source, nameof(source));
			Assert.ArgumentNotNull(target, nameof(target));

			// Check if the target directory exists, if not, create it.
			if (! Directory.Exists(target.FullName))
			{
				_msg.DebugFormat("Creating directory {0}", target.Name);
				Directory.CreateDirectory(target.FullName);
			}
			else
			{
				_msg.DebugFormat("Directory {0} already exists", target.Name);
			}

			using (_msg.IncrementIndentation())
			{
				// Copy each file into it's new directory.
				foreach (FileInfo fileInfo in source.GetFiles())
				{
					_msg.DebugFormat("Copying {0}", fileInfo.Name);

					string targetFilePath = Path.Combine(target.FullName, fileInfo.Name);
					fileInfo.CopyTo(targetFilePath, overwriteFiles);
				}

				// Copy each subdirectory using recursion.
				foreach (DirectoryInfo sourceSubdirectory in source.GetDirectories())
				{
					var targetSubdirectory =
						new DirectoryInfo(
							Path.Combine(target.FullName, sourceSubdirectory.Name));

					CopyDirectory(sourceSubdirectory, targetSubdirectory, overwriteFiles);
				}
			}
		}

		/// <summary>
		/// Move the specified source directory to a given target directory path.
		/// </summary>
		/// <param name="sourceDirectoryPath">The source directory.</param>
		/// <param name="targetDirectoryPath">The target director path.</param>
		public static void MoveDirectory([NotNull] string sourceDirectoryPath,
		                                 [NotNull] string targetDirectoryPath)
		{
			Assert.ArgumentNotNullOrEmpty(sourceDirectoryPath, nameof(sourceDirectoryPath));
			Assert.ArgumentNotNullOrEmpty(targetDirectoryPath, nameof(targetDirectoryPath));

			_msg.DebugFormat("Moving {0} to {1}", sourceDirectoryPath, targetDirectoryPath);

			Directory.Move(sourceDirectoryPath, targetDirectoryPath);
		}

		/// <summary>
		/// Deletes the directory.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="recursive"><c>true</c> to remove subdirectories and files in the directory; otherwise, <c>false</c>.</param>
		/// <param name="force">if set to <c>true</c> read-only files are deleted also.</param>
		public static void DeleteDirectory([NotNull] string directory,
		                                   bool recursive = false,
		                                   bool force = false)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));

			if (force)
			{
				_msg.DebugFormat("Making sure there are no read-only files in {0}", directory);

				SetAttributes(directory, recursive, FileAttributes.Normal);
			}

			Directory.Delete(directory, recursive);
		}

		public static void SetAttributes([NotNull] string directory,
		                                 bool recursive,
		                                 FileAttributes attribute)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));

			string[] files = recursive
				                 ? Directory.GetFiles(
					                 directory, "*", SearchOption.AllDirectories)
				                 : Directory.GetFiles(directory);

			foreach (string file in files)
			{
				_msg.DebugFormat("Setting file {0} to '{1}'", file, attribute);

				File.SetAttributes(file, attribute);
			}
		}

		[NotNull]
		public static string ReadTextFile([NotNull] string filePath,
		                                  Encoding encoding = null)
		{
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
			Assert.ArgumentCondition(File.Exists(filePath),
			                         $"File does not exist: {filePath}", filePath);

			if (encoding == null)
			{
				encoding = Encoding.Default;
			}

			using (var reader = new StreamReader(filePath, encoding))
			{
				return reader.ReadToEnd();
			}
		}

		public static bool CompareTextFiles([NotNull] string filePath1,
		                                    [NotNull] string filePath2)
		{
			Assert.ArgumentNotNullOrEmpty(filePath1, nameof(filePath1));
			Assert.ArgumentNotNullOrEmpty(filePath2, nameof(filePath2));

			if (! File.Exists(filePath1))
				throw new InvalidOperationException($"File does not exist: {filePath1}");
			if (! File.Exists(filePath2))
				throw new InvalidOperationException($"File does not exist: {filePath2}");

			var file1Info = new FileInfo(filePath1);
			var file2Info = new FileInfo(filePath2);

			if (file1Info.Length != file2Info.Length)
			{
				return false;
			}

			using (var reader1 = new StreamReader(filePath1))
			using (var reader2 = new StreamReader(filePath2))
			{
				string line1;
				do
				{
					line1 = reader1.ReadLine();
					string line2 = reader2.ReadLine();

					if (! Equals(line1, line2))
					{
						return false;
					}
				} while (line1 != null);
			}

			return true;
		}

		public static void WriteTextFile([NotNull] string s, [NotNull] string filePath)
		{
			WriteTextFile(s, filePath, Encoding.Unicode);
		}

		public static void WriteTextFile([NotNull] string s, [NotNull] string filePath,
		                                 Encoding encoding)
		{
			Assert.ArgumentNotNullOrEmpty(s, nameof(s));
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

			using (var writer = new StreamWriter(filePath, false, encoding))
			{
				writer.Write(s);
			}
		}

		[NotNull]
		public static string ExpandPathVariables([NotNull] string path)
		{
			Assert.ArgumentNotNull(path, nameof(path));

			string result = path.Trim();

			foreach (string format in _variableFormats)
			{
				foreach (KeyValuePair<string, Environment.SpecialFolder> pair
				         in _specialFolderNames)
				{
					string variableName = string.Format(format, pair.Key);

					if (path.IndexOf(variableName, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						result = StringUtils.Replace(result,
						                             variableName,
						                             Environment.GetFolderPath(pair.Value),
						                             StringComparison.OrdinalIgnoreCase);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the available space in bytes of a drive or UNC network location.
		/// </summary>
		/// <param name="folderName"></param>
		/// <param name="availableFreeBytes"></param>
		/// <returns></returns>
		public static bool TryGetAvailableFreeBytes([NotNull] string folderName,
		                                            out long availableFreeBytes)
		{
			Assert.ArgumentNotNullOrEmpty(folderName, nameof(folderName));

			availableFreeBytes = -1;

			if (! folderName.EndsWith("\\"))
			{
				folderName += '\\';
			}

			if (GetDiskFreeSpaceEx(folderName, out ulong available, out ulong _, out ulong _))
			{
				availableFreeBytes = (long) available;
				return true;
			}

			return false;
		}

		public static bool EnsureDirectoryExists([NotNull] string path)
		{
			Assert.ArgumentNotNullOrEmpty(nameof(path));

			if (Directory.Exists(path))
			{
				return true;
			}

			try
			{
				_msg.DebugFormat("Try to create folder {0}", path);
				Directory.CreateDirectory(path);

				return true;
			}
			catch (Exception e)
			{
				_msg.Debug($"Cannot create folder {path}", e);
				return false;
			}
		}

		/// <summary>
		/// Gets the available space (in gigabytes) of a drive or UNC network location.
		/// </summary>
		/// <param name="folderName"></param>
		/// <param name="availableFreeGigaBytes"></param>
		/// <returns></returns>
		public static bool TryGetAvailableFreeGigaBytes([NotNull] string folderName,
		                                                out double availableFreeGigaBytes)
		{
			bool result = TryGetAvailableFreeBytes(folderName, out long availableFreeBytes);

			availableFreeGigaBytes = result ? availableFreeBytes / Math.Pow(1024, 3) : -1;

			return result;
		}

		// From http://stackoverflow.com/questions/1393711/get-free-disk-space
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
		                                              out ulong lpFreeBytesAvailable,
		                                              out ulong lpTotalNumberOfBytes,
		                                              out ulong lpTotalNumberOfFreeBytes);

		private static char[] GetWindowsInvalidPathChars()
		{
			var result = Path.GetInvalidPathChars().ToList();

			// https://github.com/dotnet/runtime/issues/63383: A few are missing in .net 6
			var potentiallyMissingChars = new List<char> { '"', '<', '>' };

			foreach (char potentiallyMissing in potentiallyMissingChars)
			{
				if (! result.Contains(potentiallyMissing))
				{
					result.Add(potentiallyMissing);
				}
			}

			return result.ToArray();
		}

		[CanBeNull]
		public static string FromPathUri([CanBeNull] string pathUri)
		{
			if (string.IsNullOrEmpty(pathUri))
			{
				return null;
			}

			Uri uri = new Uri(pathUri, UriKind.Absolute);

			string path = uri.LocalPath;

			return path;
		}
	}
}
