using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase.Legacy
{
	/// <summary>
	/// Legacy connection provider that is not supported any more. However, in order
	/// to be able to open a legacy data dictionary, it must be present in the mapping!
	/// </summary>
	[UsedImplicitly]
	public class PgdbConnectionProvider : FilePathConnectionProviderBase
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public PgdbConnectionProvider() { }

		public PgdbConnectionProvider([NotNull] string path) : base(path) { }

		#endregion

		public override IFeatureWorkspace OpenWorkspace(int hWnd = 0)
		{
			throw new NotSupportedException(
				"The Personal Geodatabase Format is not supported in 64-bit applications, including " +
				"ArcGIS Pro. Please use ArcMap to convert it to a File Geodatabase.");
		}

		public override string TypeDescription => "Personal Geodatabase";

		public override string FileDefaultExtension => ".mdb";

		public override string FileFilter => "MDB files (*.mdb) | *.mdb";

		public override bool FilePathIsFolder => false;
	}
}
