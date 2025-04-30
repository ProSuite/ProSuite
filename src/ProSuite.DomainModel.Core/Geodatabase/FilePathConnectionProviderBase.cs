using System;
using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.Geodatabase
{
	public abstract class FilePathConnectionProviderBase : ConnectionProvider
	{
		[UsedImplicitly] private string _path;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="FilePathConnectionProviderBase"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected FilePathConnectionProviderBase() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="FilePathConnectionProviderBase"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		protected FilePathConnectionProviderBase([NotNull] string path)
			: this(GetDefaultName(path), path) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="FilePathConnectionProviderBase"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		protected FilePathConnectionProviderBase([NotNull] string name, [NotNull] string path)
			: base(name)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			_path = path;
		}

		#endregion

		[Required]
		[UsedImplicitly]
		public string Path
		{
			get { return _path; }
			set { _path = value; }
		}

		public abstract string FileDefaultExtension { get; }

		public abstract string FileFilter { get; }

		public abstract bool FilePathIsFolder { get; }

		public override string ToString()
		{
			string name = StringUtils.IsNullOrEmptyOrBlank(Name)
				              ? "<no name>"
				              : Name.Trim();

			return StringUtils.IsNullOrEmptyOrBlank(Path) ||
			       string.Equals(name.Trim(), Path.Trim(), StringComparison.OrdinalIgnoreCase)
				       ? name
				       : $"{name} ({Path})";
		}

		public void AssertFileExists()
		{
			Assert.True(StringUtils.IsNotEmpty(_path), "Path is not defined");

			if (! File.Exists(_path))
			{
				throw new FileNotFoundException(
					string.Format("File does not exist: {0}", _path), _path);
			}
		}

		public void AssertDirectoryExists()
		{
			Assert.True(StringUtils.IsNotEmpty(_path), "Path is not defined");

			if (! Directory.Exists(_path))
			{
				throw new DirectoryNotFoundException(
					string.Format("Directory does not exist: {0}", _path));
			}
		}

		#region Non-public members

		[NotNull]
		private static string GetDefaultName([CanBeNull] string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			if (path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
			{
				// invalid path; use entire path as default name
				return path;
			}

			const int maxNameLength = 30;

			if (path.Length <= maxNameLength)
			{
				return path;
			}

			string fileName = System.IO.Path.GetFileName(path);

			if (fileName.Length > maxNameLength)
			{
				return fileName;
			}

			return string.Format(@"...{0}",
			                     path.Substring(path.Length - maxNameLength,
			                                    maxNameLength));
		}

		#endregion
	}
}
