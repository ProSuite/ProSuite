using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class GeomConversionUtils
	{
		public static Polyhedron CreatePolyhedron(Multipatch multipatch)
		{
			var ringGroups = new List<RingGroup>();

			RingGroup newGroup = null;
			for (int i = 0; i < multipatch.PartCount; i++)
			{
				ReadOnlyPointCollection pointCollection = multipatch.Points;

				esriPatchType patchType = multipatch.GetPatchType(i);

				int patchPointCount = multipatch.GetPatchPointCount(i);

				int patchStartPointIndex = multipatch.GetPatchStartPointIndex(i);

				// How to differentiate outer rings from inner rings? Do we really have to check
				// - whether the ring is 2D-contained and co-planar with the previous ring
				// - if so, whether its orientation is inverted from the previous ring's orientation?
				// -> For the moment, assume every first ring is an outer ring and every other ring is interior
				if (patchType == esriPatchType.FirstRing)
				{
					if (newGroup != null)
					{
						ringGroups.Add(newGroup);
					}

					var firstRing =
						new Linestring(GetPoints(pointCollection, patchStartPointIndex,
						                         patchPointCount));

					newGroup = new RingGroup(firstRing);
				}
				else
				{
					Assert.True(patchType == esriPatchType.Ring, "Unsupported ring type");

					var ring = new Linestring(GetPoints(pointCollection, patchStartPointIndex,
					                                    patchPointCount));

					Assert.NotNull(newGroup).AddInteriorRing(ring);
				}
			}

			if (newGroup != null)
			{
				ringGroups.Add(newGroup);
			}

			return new Polyhedron(ringGroups);
		}

		private static IEnumerable<Pnt3D> GetPoints(
			ReadOnlyPointCollection pointCollection,
			int patchStartPointIndex, int patchPointCount)
		{
			int patchEndPointIndex = patchStartPointIndex + patchPointCount;
			for (int i = patchStartPointIndex; i < patchEndPointIndex; i++)
			{
				MapPoint mapPoint = pointCollection[i];
				yield return new Pnt3D(mapPoint.X, mapPoint.Y, mapPoint.Z);
			}
		}
	}
}
