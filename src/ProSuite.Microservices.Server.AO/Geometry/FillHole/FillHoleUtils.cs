using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO.Geometry.FillHole
{
	internal static class FillHoleUtils
	{
		[NotNull]
		public static IList<IPolygon> CalculateHoles(
			[NotNull] IList<IFeature> features,
			[CanBeNull] IList<IEnvelope> intersectionEnvelopes,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			// Could be optimized, but for the time being:
			Assert.False(intersectionEnvelopes?.Count > 1,
			             "Multiple envelopes are not yet supported");

			IEnvelope clipEnvelope =
				intersectionEnvelopes == null || intersectionEnvelopes.Count == 0
					? null
					: intersectionEnvelopes[0];

			IPolygon polyToAnalyze = GetPolygonToAnalyze(features, clipEnvelope, trackCancel);

			if (polyToAnalyze == null)
			{
				return new List<IPolygon>();
			}

			Predicate<IPolygon> predicate =
				clipEnvelope == null
					? (Predicate<IPolygon>) null
					: p => GeometryUtils.Intersects(p, clipEnvelope);

			List<IPolygon> holePolygons = GetIslandAndBoundaryLoopPolygons(
				polyToAnalyze, clipEnvelope, predicate,
				GeometryUtils.GetXyTolerance(polyToAnalyze),
				out IList<IRing> exteriorRings);

			IList<IPolygon> highLevelExteriorRings = CreatePolygons(exteriorRings);

			// subtract the islands from the holes. This must be done hole-by-hole to avoid missing holes on islands.
			var result = new List<IPolygon>(
				holePolygons.Select(
					holePolygon => RemoveHoleIslands(highLevelExteriorRings, holePolygon)));

			// ReSharper disable once RedundantEnumerableCastCall
			ReleaseGeometries(exteriorRings.Cast<IGeometry>());
			// ReSharper disable once RedundantEnumerableCastCall
			ReleaseGeometries(highLevelExteriorRings.Cast<IGeometry>());

			return result;
		}

		private static IPolygon GetPolygonToAnalyze(
			[NotNull] IList<IFeature> features, IEnvelope clipEnvelope,
			ITrackCancel trackCancel)
		{
			IList<IPolygon> relevantPolygons = GetPolygons(features, clipEnvelope);

			if (relevantPolygons.Count == 0 || trackCancel?.Continue() == false)
			{
				{
					return null;
				}
			}

			IPolygon polyToAnalyze;
			if (relevantPolygons.Count == 1)
			{
				polyToAnalyze = relevantPolygons[0];
			}
			else
			{
				polyToAnalyze = (IPolygon) GeometryUtils.Union(relevantPolygons);
			}

			return polyToAnalyze;
		}

		private static List<IPolygon> GetPolygons(
			[NotNull] IList<IFeature> features,
			[CanBeNull] IEnvelope clipEnvelope)
		{
			if (clipEnvelope == null)
			{
				return features.Select(f => (IPolygon) f.Shape).ToList();
			}

			var selectedShapes = new List<IPolygon>(features.Count);
			var shapesToClip = new List<IPolygon>(features.Count);

			IEnvelope originalEnvelope = GeometryFactory.Clone(clipEnvelope);

			foreach (IFeature selectedFeature in features)
			{
				var selectedPoly = (IPolygon) selectedFeature.Shape;

				if (GeometryUtils.Disjoint(selectedPoly, clipEnvelope))
				{
					Marshal.ReleaseComObject(selectedPoly);
					continue;
				}

				if (GeometryUtils.Contains(clipEnvelope, selectedPoly))
				{
					selectedShapes.Add(selectedPoly);
				}
				else
				{
					shapesToClip.Add(selectedPoly);

					// Expand the clip envelope to ensure that the partially visible holes also appear as holes
					ExpandExtentToContainRelevantRings(
						clipEnvelope, originalEnvelope, selectedPoly);
				}
			}

			foreach (IPolygon polygonToClip in shapesToClip)
			{
				selectedShapes.Add(GeometryUtils.GetClippedPolygon(polygonToClip,
					                   Assert.NotNull(clipEnvelope)));
				Marshal.ReleaseComObject(polygonToClip);
			}

			return selectedShapes;
		}

		private static void ReleaseGeometries(IEnumerable<IGeometry> geometries)
		{
			foreach (IGeometry geometry in geometries)
			{
				Marshal.ReleaseComObject(geometry);
			}
		}

		[NotNull]
		private static List<IPolygon> GetIslandAndBoundaryLoopPolygons(
			[NotNull] IPolygon inPolygon,
			[CanBeNull] IEnvelope envelopeIntersectionPredicate,
			[CanBeNull] Predicate<IPolygon> predicate,
			double xyTolerance,
			out IList<IRing> exteriorRings)
		{
			// The the inner and exterior rings
			IList<IRing> innerRings = GeometryUtils.GetRings(inPolygon, out exteriorRings);

			var candidates = new List<IPolygon>();

			// Filtering the rings is critical for performance of large polygon with many inner rings:

			IEnvelope ringEnvelope = inPolygon.Envelope;

			if (innerRings.Count > 0)
			{
				foreach (IRing ring in innerRings)
				{
					ring.QueryEnvelope(ringEnvelope);

					if (envelopeIntersectionPredicate != null &&
					    GeometryUtils.Disjoint(ring.Envelope, envelopeIntersectionPredicate))
					{
						continue;
					}

					IPolygon innerRingPoly = GeometryFactory.CreatePolygon(ring);
					GeometryUtils.Simplify(innerRingPoly, true);

					// NOTE: The inner ring could be an 8-shaped (boundary loop interior ring)
					// -> when creating an outer ring with simplify it turns into 2 rings
					foreach (IPolygon partPolygon in GetSinglePartPolygons(innerRingPoly))
					{
						if (predicate == null || predicate(partPolygon))
						{
							candidates.Add(partPolygon);
						}
						else
						{
							Marshal.ReleaseComObject(partPolygon);
						}
					}
				}

				// ReSharper disable once RedundantEnumerableCastCall
				ReleaseGeometries(innerRings.Cast<IGeometry>());
			}

			foreach (
				IPolygon boundaryLoop in
				BoundaryLoopUtils.GetBoundaryLoops(inPolygon, xyTolerance))
			{
				foreach (IPolygon partPolygon in GetSinglePartPolygons(boundaryLoop))
				{
					if (predicate == null || predicate(partPolygon))
					{
						candidates.Add(partPolygon);
					}
					else
					{
						Marshal.ReleaseComObject(partPolygon);
					}
				}
			}

			return candidates;
		}

		/// <summary>
		/// Create the differences between the given polygon and the rings that
		/// are contained by the source polygon
		/// </summary>
		/// <param name="highLevelExteriorRings">The rings that could be placed inside the sourcePolygon.</param>
		/// <param name="sourcePolygon">The polygon to substract the rings from.</param>
		/// <returns></returns>
		[CanBeNull]
		private static IPolygon RemoveHoleIslands(
			[NotNull] ICollection<IPolygon> highLevelExteriorRings,
			[CanBeNull] IPolygon sourcePolygon)
		{
			var removePolys = new List<IPolygon>(highLevelExteriorRings.Count);

			if (highLevelExteriorRings.Count <= 0 || sourcePolygon == null)
			{
				return sourcePolygon;
			}

			foreach (IPolygon highLevelRing in highLevelExteriorRings)
			{
				if (GeometryUtils.Contains(sourcePolygon, highLevelRing))
				{
					removePolys.Add(highLevelRing);
				}
			}

			IGeometry result = sourcePolygon;

			foreach (IPolygon removePoly in removePolys)
			{
				result = IntersectionUtils.Difference(result, removePoly);
			}

			return (IPolygon) result;
		}

		private static IList<IPolygon> CreatePolygons(IList<IRing> rings)
		{
			var result = new List<IPolygon>(rings.Count);

			foreach (IRing ring in rings)
			{
				IPolygon polygon = GeometryFactory.CreatePolygon(ring);

				result.Add(polygon);
			}

			return result;
		}

		private static void ExpandExtentToContainRelevantRings(
			[NotNull] IEnvelope extentToExpand,
			[NotNull] IEnvelope intersectingExtent,
			[NotNull] IPolygon polygon)
		{
			foreach (IRing ring in GeometryUtils.GetRings(polygon))
			{
				if (ring.IsExterior)
				{
					continue;
				}

				IEnvelope ringEnv = ring.Envelope;

				if (GeometryUtils.Intersects(intersectingExtent, ringEnv))
				{
					extentToExpand.Union(ringEnv);

					double margin = 5 * GeometryUtils.GetXyTolerance(polygon);
					extentToExpand.Expand(margin, margin, false);
				}
			}
		}

		private static IEnumerable<IPolygon> GetSinglePartPolygons(
			[NotNull] IPolygon polygon)
		{
			if (polygon.IsEmpty)
			{
				yield break;
			}

			if (polygon.ExteriorRingCount > 1)
			{
				foreach (IPolygon partPolygon in GeometryUtils.Explode(polygon)
				                                              .Cast<IPolygon>())
				{
					yield return partPolygon;
				}
			}
			else
			{
				yield return polygon;
			}
		}
	}
}
