using System;
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

	/// <summary>
	/// Converts an SDK polygon into a multipatch, grouping each exterior ring with its
	/// holes so an annular polygon (e.g. the buffer of a closed loop) becomes a multipatch
	/// with an actual hole: the exterior ring is emitted as a <see cref="PatchType.FirstRing"/>
	/// patch and every interior ring as a (hole) <see cref="PatchType.Ring"/> patch. Emitting
	/// every ring as a FirstRing instead would fill the interior hole with a separate solid ring.
	/// </summary>
	/// <returns>The multipatch, or null if the input is null or empty.</returns>
	[CanBeNull]
	public static Multipatch CreateMultipatch([CanBeNull] Polygon polygon, int? partId = null)
	{
		if (polygon == null || polygon.IsEmpty)
		{
			return null;
		}

		List<RingGroup> ringGroups = CreateRingGroups(polygon);

		if (ringGroups.Count == 0)
		{
			return null;
		}

		return CreateMultipatch(new Polyhedron(ringGroups), polygon.SpatialReference, partId);
	}

	public static MultiPolycurve CreateMultiPolycurve([NotNull] Polygon polygon)
	{
		return new MultiPolycurve(CreateRingGroups(polygon));
	}

	/// <summary>
	/// Groups the rings of an SDK polygon into ring groups, pairing each exterior ring with
	/// its holes (interior rings). Relies on <see cref="GeometryUtils.ConnectedComponents"/>
	/// to split the polygon into single-shell components: within each component the first ring
	/// is the exterior ring and the remaining rings are its holes.
	/// </summary>
	private static List<RingGroup> CreateRingGroups([NotNull] Polygon polygon)
	{
		var result = new List<RingGroup>();

		foreach (Polygon component in GeometryUtils.ConnectedComponents(polygon))
		{
			RingGroup ringGroup = null;

			foreach (ReadOnlySegmentCollection ring in component.Parts)
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

			if (ringGroup != null)
			{
				result.Add(ringGroup);
			}
		}

		return result;
	}

	/// <summary>
	/// Converts an (open) polyline into the SDK-independent geometry model, one
	/// <see cref="Linestring"/> per part.
	/// </summary>
	public static MultiPolycurve CreateMultiPolycurve([NotNull] Polyline polyline)
	{
		Assert.ArgumentNotNull(polyline, nameof(polyline));

		var linestrings = new List<Linestring>();

		foreach (ReadOnlySegmentCollection part in polyline.Parts)
		{
			linestrings.Add(new Linestring(GetPoints(part)));
		}

		return new MultiPolycurve(linestrings);
	}

	/// <summary>
	/// Converts a <see cref="MultiLinestring"/> (whose rings carry the Esri ring
	/// orientation, i.e. exterior rings clockwise, interior rings counter-clockwise)
	/// into an SDK polygon.
	/// </summary>
	/// <returns>The polygon, or null if the input is empty.</returns>
	[CanBeNull]
	public static Polygon CreatePolygon([NotNull] MultiLinestring multiLinestring,
	                                    [CanBeNull] SpatialReference spatialReference)
	{
		Assert.ArgumentNotNull(multiLinestring, nameof(multiLinestring));

		if (multiLinestring.IsEmpty)
		{
			return null;
		}

		var builder = new PolygonBuilderEx(spatialReference) { HasZ = true };

		foreach (Linestring ring in multiLinestring.GetLinestrings())
		{
			if (ring.IsEmpty)
			{
				continue;
			}

			List<MapPoint> ringPoints =
				ring.GetPoints()
				    .Select(pnt => MapPointBuilderEx.CreateMapPoint(
					            pnt.X, pnt.Y, pnt.Z, spatialReference))
				    .ToList();

			builder.AddPart(ringPoints);
		}

		return builder.ToGeometry();
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
			else if (patchType != PatchType.Ring)
			{
				throw new NotSupportedException($"Unsupported ring type: {patchType}");
			}
			else
			{
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

		result.Coords = linestring.GetPoints().Select(pnt => new Coordinate3D(pnt.X, pnt.Y, pnt.Z))
		                          .ToList();

		if (partId != null)
		{
			result.IDs = Enumerable.Repeat(partId.Value, result.Coords.Count).ToList();
		}

		return result;
	}
}
