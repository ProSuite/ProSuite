using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class TinSurface : ISimpleSurface
	{
		private readonly ITin _tin;

		public TinSurface([NotNull] ITin tin)
		{
			_tin = tin;
		}

		private ISurface Surface => (ISurface) _tin;

		public IPolygon GetDomain()
		{
			return ((ISurface) _tin).Domain;
		}

		public double GetZ(double x, double y)
		{
			return ((ISurface) _tin).get_Z(x, y);
		}

		public IGeometry Drape(IGeometry shape, double densifyDistance = double.NaN)
		{
			Assert.ArgumentNotNull(shape, nameof(shape));

			object stepSizeObj = densifyDistance > 0
				                     ? densifyDistance
				                     : Type.Missing;

			Surface.InterpolateShape(shape, out IGeometry outShape, ref stepSizeObj);

			return outShape;
		}

		public IGeometry SetShapeVerticesZ(IGeometry shape)
		{
			Assert.ArgumentNotNull(shape, nameof(shape));

			Surface.InterpolateShapeVertices(shape, out IGeometry outShape);

			return outShape;
		}

		public ITin AsTin(IEnvelope extent = null)
		{
			return _tin;
		}

		public void Dispose()
		{
			Marshal.ReleaseComObject(_tin);
		}
	}
}
