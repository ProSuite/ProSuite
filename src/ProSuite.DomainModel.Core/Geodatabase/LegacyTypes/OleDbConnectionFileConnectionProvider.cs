using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.Geodatabase.LegacyTypes
{
	[Obsolete(
		"Not supported in ArcGIS 11 (or at least not programmatically) but must be retained due to legacy DDX compatibility.")]
	[UsedImplicitly]
	public class OleDbConnectionFileConnectionProvider : FilePathConnectionProviderBase
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public OleDbConnectionFileConnectionProvider() { }

		public OleDbConnectionFileConnectionProvider([NotNull] string connectionFilePath)
			: base(connectionFilePath) { }

		#endregion

		public override DbConnectionType ConnectionType => DbConnectionType.Other;

		public override string TypeDescription => "OLE DB Connection File";

		public override string FileDefaultExtension => ".odc";

		public override string FileFilter => "OLE DB connection files (*.odc) | *.odc";

		public override bool FilePathIsFolder => false;
	}
}
