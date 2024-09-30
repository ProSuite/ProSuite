using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IFeature : IObject
	{
		IGeometry ShapeCopy { get; }

		IGeometry Shape { get; set; }

		IEnvelope Extent { get; }

		//esriFeatureType FeatureType { get; }
	}
}
