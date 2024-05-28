using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	[UsedImplicitly]
	public class ConnectionFileConnectionProvider : FilePathConnectionProviderBase,
	                                                IOpenSdeWorkspace
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public ConnectionFileConnectionProvider() { }

		public ConnectionFileConnectionProvider([NotNull] string connectionFilePath)
			: base(connectionFilePath) { }

		#endregion

		public override IFeatureWorkspace OpenWorkspace(int hWnd = 0)
		{
			return OpenWorkspace(null, hWnd);
		}

		public IFeatureWorkspace OpenWorkspace(string versionName, int hWnd = 0)
		{
			AssertFileExists();
			return (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(Path, versionName, hWnd);
		}

		#region Overrides of ConnectionProvider

		public override DbConnectionType ConnectionType => DbConnectionType.DatabaseConnectionFile;

		public override string TypeDescription => "SDE Connection File";

		#endregion

		public override string FileDefaultExtension => ".sde";

		public override string FileFilter => "SDE files (*.sde) | *.sde";

		public override bool FilePathIsFolder => false;
	}
}
