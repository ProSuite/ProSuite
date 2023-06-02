using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.EsriShape;

namespace ProSuite.Commons.Db
{
	public interface IGeoDbDataset
	{
		IBoundedXY Extent { get; }
		ISpatialReferenceDef SpatialReference { get; }
	}

	public interface IFeatureClassSchemaDef : ITableSchemaDef
	{
		string ShapeFieldName { get; }

		ProSuiteGeometryType ShapeType { get; }

		ITableField AreaField { get; }
		ITableField LengthField { get; }
	}

	public interface IFeatureClassData : IFeatureClassSchemaDef, ITableData { }
}
