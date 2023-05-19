using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.EsriShape;

namespace ProSuite.Commons.Db
{
	public interface IGeoDbDataset
	{
		IBoundedXY Extent { get; }
		ISpatialReferenceInfo SpatialReference { get; }
	}

	public interface IFeatureClassSchema : IDbTableSchema
	{
		string ShapeFieldName { get; }

		ProSuiteGeometryType ShapeType { get; }

		ITableField AreaField { get; }
		ITableField LengthField { get; }
	}

	public interface IFeatureClassData : IFeatureClassSchema, IDbTable { }
}
