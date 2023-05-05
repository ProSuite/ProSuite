using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class AoTableFilter : ITableFilter
	{
		public AoTableFilter()
		{}
		public string SubFields { get; set; }

		public string WhereClause { get; set; }

		public virtual object ToNativeFilterImpl(IFeatureClass featureClass = null)
		{
			IQueryFilter result = GdbQueryUtils.CreateQueryFilter();

			result.SubFields = SubFields;
			result.WhereClause = WhereClause;

			return result;
		}
	}
	public class AoFeatureClassFilter : AoTableFilter, IFeatureClassFilter, ITileFilter
	{
		public AoFeatureClassFilter(
			[NotNull] IGeometry filterGeometry,
			esriSpatialRelEnum spatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			FilterGeometry = filterGeometry;
			SpatialRelationship = spatialRelationship;
		}

		#region Implementation of IFeatureClassFilter

		public esriSpatialRelEnum SpatialRelationship { get; set; }

		public IGeometry FilterGeometry { get; set; }

		#endregion

		#region Implementation of ITileFilter

		public IEnvelope TileExtent { get; set; }

		#endregion

		#region Implementation of ITableFilter

		public override object ToNativeFilterImpl(IFeatureClass featureClass = null)
		{
			IQueryFilter result = GdbQueryUtils.CreateSpatialFilter(
				featureClass, FilterGeometry, SpatialRelationship,
				filterOwnsGeometry: true, outputSpatialReference: null);

			result.SubFields = SubFields;
			result.WhereClause = WhereClause;

			return result;
		}

		//public object ToNativeFilterImpl(IReadOnlyFeatureClass readOnlyFeatureClass = null)
		//{
		//	IFeatureClass featureClass = null;

		//	if (readOnlyFeatureClass is ReadOnlyTable roTable)
		//	{
		//		featureClass = (IFeatureClass) roTable.BaseTable;
		//	}

		//	// TODO: GdbQueryUtils.CreateSpatialFilter(IReadOnlyFeatureClass)
		//	return ToNativeFilterImpl(featureClass);
		//}

		#endregion

	}
}
