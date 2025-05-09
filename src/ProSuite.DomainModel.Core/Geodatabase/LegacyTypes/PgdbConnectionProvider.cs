using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.Geodatabase.LegacyTypes
{
	/// <summary>
	/// Legacy connection provider that is not supported anymore. However, in order
	/// to be able to open a legacy data dictionary, it must be present in the mapping!
	/// </summary>
	[UsedImplicitly]
	[Obsolete("Not supported in ArcGIS 11 but must be retained due to legacy DDX compatibility.")]
	public class PgdbConnectionProvider : FilePathConnectionProviderBase
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public PgdbConnectionProvider() { }

		public PgdbConnectionProvider([NotNull] string path) : base(path) { }

		#endregion
		
		#region Overrides of ConnectionProvider

		public override DbConnectionType ConnectionType => DbConnectionType.Other;

		public override string TypeDescription => "Personal Geodatabase";

		#endregion

		public override string FileDefaultExtension => ".mdb";

		public override string FileFilter => "MDB files (*.mdb) | *.mdb";

		public override bool FilePathIsFolder => false;
	}
}
