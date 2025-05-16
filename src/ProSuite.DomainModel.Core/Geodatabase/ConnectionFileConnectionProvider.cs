using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.Geodatabase
{
	[UsedImplicitly]
	public class ConnectionFileConnectionProvider : FilePathConnectionProviderBase
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public ConnectionFileConnectionProvider() { }

		public ConnectionFileConnectionProvider([NotNull] string connectionFilePath)
			: base(connectionFilePath) { }

		#endregion

		#region Overrides of ConnectionProvider

		public override DbConnectionType ConnectionType => DbConnectionType.DatabaseConnectionFile;

		public override string TypeDescription => "SDE Connection File";

		#endregion

		public override string FileDefaultExtension => ".sde";

		public override string FileFilter => "SDE files (*.sde) | *.sde";

		public override bool FilePathIsFolder => false;
	}
}
