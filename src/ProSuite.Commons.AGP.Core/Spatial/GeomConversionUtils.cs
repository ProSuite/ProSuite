using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class GeomConversionUtils
	{
		public static EnvelopeXY CreateEnvelopeXY(Envelope envelope)
		{
			return new EnvelopeXY(envelope.XMin, envelope.YMin, envelope.XMax, envelope.YMax);
		}

		public static Polyhedron CreatePolyhedron(Multipatch multipatch)
		{
			var ringGroups = new List<RingGroup>();

			RingGroup newGroup = null;
			for (int i = 0; i < multipatch.PartCount; i++)
			{
				ReadOnlyPointCollection pointCollection = multipatch.Points;

				var patchType = multipatch.GetPatchType(i);

				int patchPointCount = multipatch.GetPatchPointCount(i);

				int patchStartPointIndex = multipatch.GetPatchStartPointIndex(i);

				// How to differentiate outer rings from inner rings? Do we really have to check
				// - whether the ring is 2D-contained and co-planar with the previous ring
				// - if so, whether its orientation is inverted from the previous ring's orientation?
				// -> For the moment, assume every first ring is an outer ring and every other ring is interior
				if (patchType == PatchType.FirstRing)
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
					Assert.True(patchType == PatchType.Ring, "Unsupported ring type");

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

		public static Multipatch CreateMultipatch([NotNull] Polyhedron polyhedron,
		                                          [CanBeNull] SpatialReference spatialReference)
		{
			MultipatchBuilderEx mpBuilder = new MultipatchBuilderEx(spatialReference);

			var patches = new List<Patch>();

			foreach (RingGroup ringGroup in polyhedron.RingGroups)
			{
				Linestring exteriorRing = ringGroup.ExteriorRing;

				if (exteriorRing == null)
				{
					continue;
				}

				Patch firstRing = ToPatch(exteriorRing, PatchType.FirstRing, mpBuilder);

				patches.Add(firstRing);

				foreach (Linestring interiorRing in ringGroup.InteriorRings)
				{
					Patch interiorPatch = ToPatch(interiorRing, PatchType.Ring, mpBuilder);

					patches.Add(interiorPatch);
				}
			}

			mpBuilder.Patches = patches;

			Multipatch multipatch = mpBuilder.ToGeometry();

			return multipatch;
		}

		private static Patch ToPatch([NotNull] Linestring linestring,
		                             PatchType patchType,
		                             MultipatchBuilderEx patchBuilder)
		{
			Patch result = patchBuilder.MakePatch(patchType);

			result.Coords = linestring.GetPoints().Select(
				pnt => new Coordinate3D(pnt.X, pnt.Y, pnt.Z)).ToList();

			return result;
		}
	}
}
