using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	[Obsolete(
		"Not supported in ArcGIS 11 (or at least not programmatically) but must be retained due to legacy DDX compatibility.")]
	[UsedImplicitly]
	public class OleDbConnectionFileConnectionProvider : FilePathConnectionProviderBase,
	                                                     IOpenSdeWorkspace
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public OleDbConnectionFileConnectionProvider() { }

		public OleDbConnectionFileConnectionProvider([NotNull] string connectionFilePath)
			: base(connectionFilePath) { }

		#endregion

		public override IFeatureWorkspace OpenWorkspace(int hWnd = 0)
		{
			return OpenWorkspace(null, hWnd);
		}

		public IFeatureWorkspace OpenWorkspace(string versionName, int hWnd = 0)
		{
			AssertFileExists();
			return (IFeatureWorkspace) WorkspaceUtils.OpenOleDbWorkspaceFromFile(Path, hWnd);
		}

		public override DbConnectionType ConnectionType => DbConnectionType.Other;

		public override string TypeDescription => "OLE DB Connection File";

		public override string FileDefaultExtension => ".odc";

		public override string FileFilter => "OLE DB connection files (*.odc) | *.odc";

		public override bool FilePathIsFolder => false;
	}
}
