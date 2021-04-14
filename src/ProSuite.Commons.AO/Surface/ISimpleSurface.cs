using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public interface ISimpleSurface : IDisposable
	{
		[CanBeNull]
		IPolygon GetDomain();

		double GetZ(double x, double y);

		[CanBeNull]
		IGeometry Drape([NotNull] IGeometry shape, double densifyDistance = double.NaN);

		[CanBeNull]
		IGeometry SetShapeVerticesZ([NotNull] IGeometry shape);

		[CanBeNull]
		ITin AsTin([CanBeNull] IEnvelope extent = null);

		[CanBeNull]
		IRaster AsRaster();
	}
}
