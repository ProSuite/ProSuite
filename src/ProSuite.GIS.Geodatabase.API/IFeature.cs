using ESRI.ArcGIS.Geometry;

namespace ESRI.ArcGIS.Geodatabase
{
	public interface IFeature : IObject
	{
		IGeometry ShapeCopy { get; }

		IGeometry Shape { get; set; }

		IEnvelope Extent { get; }

		//esriFeatureType FeatureType { get; }
	}
}
