using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.Geodatabase
{
	public class FileGdbConnectionProvider : FilePathConnectionProviderBase
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public FileGdbConnectionProvider() { }

		public FileGdbConnectionProvider([NotNull] string path) : base(path) { }

		#endregion

		public override DbConnectionType ConnectionType => DbConnectionType.FileGeodatabase;

		public override string TypeDescription => "File Geodatabase";

		public override string FileDefaultExtension => ".gdb";

		public override string FileFilter => "GDB folder (*.gdb) | *.gdb";

		public override bool FilePathIsFolder => true;
	}
}
