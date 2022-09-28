using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public static class GeomFactory
	{
		public static Linestring CreateRing(IBoundedXY envelopeBoundary)
		{
			Linestring envelopeSegments =
				new Linestring(GetBoundaryRingVertices(envelopeBoundary));

			return envelopeSegments;
		}

		private static IEnumerable<Pnt3D> GetBoundaryRingVertices([NotNull] IBoundedXY envelope)
		{
			yield return new Pnt3D(envelope.XMin, envelope.YMin, double.NaN);
			yield return new Pnt3D(envelope.XMin, envelope.YMax, double.NaN);
			yield return new Pnt3D(envelope.XMax, envelope.YMax, double.NaN);
			yield return new Pnt3D(envelope.XMax, envelope.YMin, double.NaN);
			yield return new Pnt3D(envelope.XMin, envelope.YMin, double.NaN);
		}
	}
}
