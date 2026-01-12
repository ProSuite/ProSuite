using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.Spatial;

public static class GeomConversionUtils
{
	public static EnvelopeXY CreateEnvelopeXY([NotNull] Envelope envelope)
	{
		return new EnvelopeXY(envelope.XMin, envelope.YMin, envelope.XMax, envelope.YMax);
	}

	public static Pnt3D GetPoint3D(Coordinate3D coord)
	{
		return new Pnt3D(coord.X, coord.Y, coord.Z);
	}

	public static Polyhedron CreatePolyhedron([NotNull] Multipatch multipatch)
	{
		List<RingGroup> ringGroups = CreateRingGroups(multipatch, false);

		return new Polyhedron(ringGroups);
	}

	public static IEnumerable<Polyhedron> CreatePolyhedra([NotNull] Multipatch multipatch)
	{
		bool maintainRingIds = multipatch.HasID;

		List<RingGroup> ringGroups = CreateRingGroups(multipatch, maintainRingIds);

		if (maintainRingIds)
		{
			foreach (IGrouping<int?, RingGroup> grouping in ringGroups.GroupBy(r => r.Id))
			{
				yield return new Polyhedron(grouping.ToList());
			}
		}
		else
		{
			// When not maintaining ring IDs, return all ring groups as a single polyhedron
			yield return new Polyhedron(ringGroups);
		}
	}

	public static Multipatch CreateMultipatch([NotNull] Polyhedron polyhedron,
	                                          [CanBeNull] SpatialReference spatialReference,
	                                          int? partId = 0)
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

			Patch firstRing = ToPatch(exteriorRing, PatchType.FirstRing, mpBuilder, partId);

			patches.Add(firstRing);

			foreach (Linestring interiorRing in ringGroup.InteriorRings)
			{
				Patch interiorPatch = ToPatch(interiorRing, PatchType.Ring, mpBuilder, partId);

				patches.Add(interiorPatch);
			}
		}

		mpBuilder.Patches = patches;

		if (partId != null)
		{
			mpBuilder.HasID = true;
		}

		Multipatch multipatch = mpBuilder.ToGeometry();

		return multipatch;
	}

	public static MultiPolycurve CreateMultiPolycurve([NotNull] Polygon polygon)
	{
		var result = new List<RingGroup>();
		foreach (Polygon singlePolygon in GeometryUtils.ConnectedComponents(polygon))
		{
			RingGroup ringGroup = null;

			foreach (ReadOnlySegmentCollection ring in singlePolygon.Parts)
			{
				var line = new Linestring(GetPoints(ring));
				if (ringGroup == null)
				{
					ringGroup = new RingGroup(line);
				}
				else
				{
					ringGroup.AddInteriorRing(line);
				}
			}

			result.Add(ringGroup);
		}

		return new MultiPolycurve(result);
	}

	private static List<RingGroup> CreateRingGroups([NotNull] Multipatch multipatch,
	                                                bool maintainRingIds)
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

				newGroup = AddToRingGroup(pointCollection, null, patchStartPointIndex,
				                          patchPointCount, maintainRingIds);
			}
			else
			{
				Assert.True(patchType == PatchType.Ring, $"Unsupported ring type: {patchType}");
				Assert.NotNull(newGroup);

				newGroup = AddToRingGroup(pointCollection, newGroup, patchStartPointIndex,
				                          patchPointCount, maintainRingIds);
			}
		}

		if (newGroup != null)
		{
			ringGroups.Add(newGroup);
		}

		return ringGroups;
	}

	private static RingGroup AddToRingGroup([NotNull] ReadOnlyPointCollection pointCollection,
	                                        [CanBeNull] RingGroup existingGroup,
	                                        int patchStartPointIndex, int patchPointCount,
	                                        bool maintainRingIds)
	{
		int? ringId = null;

		if (maintainRingIds)
		{
			// Extract unique IDs from all points in this patch
			int? uniqueId =
				GetUniquePointId(pointCollection, patchStartPointIndex, patchPointCount);

			// Check if it matches the existing group's ID (if exists)
			if (existingGroup == null || existingGroup.Id == uniqueId)
			{
				ringId = uniqueId;
			}
		}

		var ring =
			new Linestring(GetPoints(pointCollection, patchStartPointIndex,
			                         patchPointCount));

		if (existingGroup == null)
		{
			existingGroup = new RingGroup(ring) { Id = ringId };
		}
		else
		{
			existingGroup.AddInteriorRing(ring);

			// Only update the ID if it's still consistent
			if (ringId.HasValue && existingGroup.Id != ringId)
			{
				existingGroup.Id = null;
			}
		}

		return existingGroup;
	}

	private static int? GetUniquePointId([NotNull] ReadOnlyPointCollection pointCollection,
	                                     int patchStartPointIndex,
	                                     int patchPointCount)
	{
		int? uniqueId = null;
		int patchEndPointIndex = patchStartPointIndex + patchPointCount;

		for (int i = patchStartPointIndex; i < patchEndPointIndex; i++)
		{
			MapPoint mapPoint = pointCollection[i];

			if (uniqueId == null)
			{
				// First ID found:
				uniqueId = mapPoint.ID;
			}
			else if (uniqueId != mapPoint.ID)
			{
				// Found a different ID, not unique
				return null;
			}
		}

		return uniqueId;
	}

	private static IEnumerable<Pnt3D> GetPoints(
		[NotNull] ReadOnlyPointCollection pointCollection,
		int patchStartPointIndex, int patchPointCount)
	{
		int patchEndPointIndex = patchStartPointIndex + patchPointCount;
		for (int i = patchStartPointIndex; i < patchEndPointIndex; i++)
		{
			MapPoint mapPoint = pointCollection[i];
			yield return new Pnt3D(mapPoint.X, mapPoint.Y, mapPoint.Z);
		}
	}

	private static IEnumerable<Pnt3D> GetPoints([NotNull] ReadOnlySegmentCollection ring)
	{
		Segment lastSegment = null;
		foreach (Segment segment in ring)
		{
			lastSegment = segment;
			yield return GetPoint3D(segment.StartPoint.Coordinate3D);
		}

		if (lastSegment != null)
		{
			yield return GetPoint3D(lastSegment.EndPoint.Coordinate3D);
		}
	}

	private static Patch ToPatch([NotNull] Linestring linestring,
	                             PatchType patchType,
	                             MultipatchBuilderEx patchBuilder,
	                             int? partId = null)
	{
		Patch result = patchBuilder.MakePatch(patchType);

		result.Coords = linestring.GetPoints().Select(
			pnt => new Coordinate3D(pnt.X, pnt.Y, pnt.Z)).ToList();

		if (partId != null)
		{
			result.IDs = Enumerable.Repeat(partId.Value, result.Coords.Count).ToList();
		}

		return result;
	}
}
