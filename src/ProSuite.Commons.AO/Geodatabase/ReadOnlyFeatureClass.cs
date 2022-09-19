using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyFeatureClass : ReadOnlyTable, IReadOnlyFeatureClass
	{
		protected static ReadOnlyFeatureClass CreateReadOnlyFeatureClass(IFeatureClass fc)
		{ return new ReadOnlyFeatureClass(fc); }
		protected ReadOnlyFeatureClass(IFeatureClass featureClass)
			: base((ITable)featureClass)
		{ }

		public string ShapeFieldName => FeatureClass.ShapeFieldName;
		public IField AreaField => DatasetUtils.GetAreaField(FeatureClass);
		public IField LengthField => DatasetUtils.GetLengthField(FeatureClass);
		public IEnvelope Extent => ((IGeoDataset)FeatureClass).Extent;
		public ISpatialReference SpatialReference => ((IGeoDataset)FeatureClass).SpatialReference;
		public esriGeometryType ShapeType => FeatureClass.ShapeType;
		protected IFeatureClass FeatureClass => (IFeatureClass)Table;
		public override ReadOnlyRow CreateRow(IRow row)
		{
			return new ReadOnlyFeature(this, (IFeature)row);
		}
	}
}
