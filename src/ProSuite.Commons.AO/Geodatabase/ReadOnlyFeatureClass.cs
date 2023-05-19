using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Db;
using ProSuite.Commons.Geom.EsriShape;
using FieldType = ProSuite.Commons.Db.FieldType;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyFeatureClass : ReadOnlyTable, IReadOnlyFeatureClass, IFeatureClassSchema
	{
		protected static ReadOnlyFeatureClass CreateReadOnlyFeatureClass(IFeatureClass fc)
		{
			return new ReadOnlyFeatureClass(fc);
		}

		protected ReadOnlyFeatureClass(IFeatureClass featureClass)
			: base((ITable) featureClass) { }

		public string ShapeFieldName => FeatureClass.ShapeFieldName;
		public IField AreaField => DatasetUtils.GetAreaField(FeatureClass);
		public IField LengthField => DatasetUtils.GetLengthField(FeatureClass);
		public IEnvelope Extent => ((IGeoDataset) FeatureClass).Extent;
		public ISpatialReference SpatialReference => ((IGeoDataset) FeatureClass).SpatialReference;
		public esriGeometryType ShapeType => FeatureClass.ShapeType;
		protected IFeatureClass FeatureClass => (IFeatureClass) Table;

		public override ReadOnlyRow CreateRow(IRow row)
		{
			return ReadOnlyFeature.Create(this, (IFeature) row);
		}

		#region Implementation of IFeatureClassSchema

		ProSuiteGeometryType IFeatureClassSchema.ShapeType => (ProSuiteGeometryType) ShapeType;

		ITableField IFeatureClassSchema.AreaField =>
			new TableField(AreaField.Name, FieldType.Double);

		ITableField IFeatureClassSchema.LengthField =>
			new TableField(LengthField.Name, FieldType.Double);

		#endregion
	}
}
