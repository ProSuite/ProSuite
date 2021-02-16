using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase
{
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

		public override string TypeDescription => "SDE Connection File";

		public override string FileDefaultExtension => ".sde";

		public override string FileFilter => "SDE files (*.sde) | *.sde";

		public override bool FilePathIsFolder => false;
	}
}
