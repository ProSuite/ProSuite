using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public class MobileGdbConnectionProvider : FilePathConnectionProviderBase
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public MobileGdbConnectionProvider() { }

		public MobileGdbConnectionProvider([NotNull] string path) : base(path) { }

		#endregion

		public override IFeatureWorkspace OpenWorkspace(int hWnd = 0)
		{
			AssertFileExists();
			return WorkspaceUtils.OpenMobileGdbFeatureWorkspace(Path);
		}

		public override DbConnectionType ConnectionType => DbConnectionType.SQLite;

		public override string TypeDescription => "Mobile Geodatabase";

		public override string FileDefaultExtension => ".geodatabase";

		public override string FileFilter => "Geodatabase file (*.geodatabase) | *.geodatabase";

		public override bool FilePathIsFolder => false;
	}
}