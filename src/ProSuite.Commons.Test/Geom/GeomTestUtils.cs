using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.Test.Geom
{
	internal static class GeomTestUtils
	{
		internal static List<Pnt3D> GetRotatedRing(List<Pnt3D> ringPoints, int steps)
		{
			Pnt3D[] array = ringPoints.Select(p => p.ClonePnt3D()).ToArray();

			CollectionUtils.Rotate(array, steps);
			var rotatedRing = new List<Pnt3D>(array);

			rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

			return rotatedRing;
		}

		public static RingGroup CreatePoly(List<Pnt3D> points)
		{
			Linestring ring = CreateRing(points);

			RingGroup poly = new RingGroup(ring);

			return poly;
		}

		public static Linestring CreateRing(List<Pnt3D> points)
		{
			if (! points[0].Equals(points[points.Count - 1]))
			{
				points = new List<Pnt3D>(points);
				points.Add(points[0].ClonePnt3D());
			}

			var ring = new Linestring(points);
			return ring;
		}

		public static string GetGeometryTestDataPath(string fileName)
		{
			return TestDataPreparer.FromDirectory(@"TestData\Geom").GetPath(fileName);
		}
	}
}
