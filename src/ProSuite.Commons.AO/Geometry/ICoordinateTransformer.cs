using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geometry
{
	public interface ICoordinateTransformer
	{
		ISpatialReference SourceSpatialReference { get; }

		ISpatialReference TargetSpatialReference { get; }

		void Transform(IGeometry geometry);

		void TransformBack(IGeometry geometry);

		T Project<T>(T immutableGeometry) where T : IGeometry;

		T ProjectBack<T>(T immutableGeometry) where T : IGeometry;
	}
}
