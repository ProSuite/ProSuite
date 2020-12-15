using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	[CLSCompliant(false)]
	public class FileGdbConnectionProvider : FilePathConnectionProviderBase
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public FileGdbConnectionProvider() { }

		public FileGdbConnectionProvider([NotNull] string path) : base(path) { }

		#endregion

		public override IFeatureWorkspace OpenWorkspace(int hWnd = 0)
		{
			AssertDirectoryExists();
			return WorkspaceUtils.OpenFileGdbFeatureWorkspace(Path);
		}

		public override string TypeDescription => "File Geodatabase";

		public override string FileDefaultExtension => ".gdb";

		public override string FileFilter => "GDB folder (*.gdb) | *.gdb";

		public override bool FilePathIsFolder => true;
	}
}
