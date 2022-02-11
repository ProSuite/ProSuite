using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class RelatedTable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RelatedTable"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="fullOidFieldName">Full name of the oid field.</param>
		/// <param name="oidFieldIndex">Index of the oid field.</param>
		public RelatedTable([NotNull] IReadOnlyTable table,
		                    [NotNull] string tableName,
		                    [NotNull] string fullOidFieldName,
		                    int oidFieldIndex)
		{
			Table = table;
			TableName = tableName;
			OidFieldIndex = oidFieldIndex;
			FullOidFieldName = fullOidFieldName;

			IsFeatureClass = table is IReadOnlyFeatureClass;
		}

		[NotNull]
		public string FullOidFieldName { get; }

		[NotNull]
		public string TableName { get; }

		[NotNull]
		public IReadOnlyTable Table { get; }

		public int OidFieldIndex { get; }

		public bool IsFeatureClass { get; }

		public IGeometry GetGeometry(int oid)
		{
			return ! IsFeatureClass
				       ? null
				       : TestUtils.GetShapeCopy(Table.GetRow(oid));
		}
	}
}
