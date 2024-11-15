using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IFeature : IObject
	{
		new IFeatureClass Class { get; }

		IGeometry ShapeCopy { get; }

		IGeometry Shape { get; set; }

		IEnvelope Extent { get; }

		//esriFeatureType FeatureType { get; }
	}

	public interface IFeatureChanges
	{
		bool ShapeChanged { get; }

		IGeometry OriginalShape { get; }
	}
}
