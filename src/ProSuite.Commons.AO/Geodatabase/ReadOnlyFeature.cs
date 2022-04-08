using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyFeature : ReadOnlyRow, IReadOnlyFeature
	{
		public new static ReadOnlyFeature Create(IFeature feature)
		{
			return new ReadOnlyFeature(
				ReadOnlyTableFactory.Create((IFeatureClass) feature.Table), feature);
		}
		public ReadOnlyFeature(ReadOnlyFeatureClass featureClass, IFeature feature)
			: base(featureClass, feature)
		{ }
		protected IFeature Feature => (IFeature)Row;
		public IEnvelope Extent => Feature.Extent;
		public IGeometry Shape => Feature.Shape;
		public IGeometry ShapeCopy => Feature.ShapeCopy;
		IReadOnlyFeatureClass IReadOnlyFeature.FeatureClass => FeatureClass;
		public ReadOnlyFeatureClass FeatureClass => (ReadOnlyFeatureClass)Table;
		public esriFeatureType FeatureType => Feature.FeatureType;
	}
}
