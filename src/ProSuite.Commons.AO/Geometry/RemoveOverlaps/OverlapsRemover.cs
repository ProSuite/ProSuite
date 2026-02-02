using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Geometry.ExtractParts;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.RemoveOverlaps
{
	public class OverlapsRemover
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly bool _explodeMultipartResult;

		private readonly bool _storeOverlapsAsNewFeatures;

		// TODO: Extract configuration option
		private readonly bool _onlyBoundaryTouchingMultipatches = true;

		public OverlapsRemover(bool explodeMultipartResult,
		                       bool storeOverlapsAsNewFeatures = false)
		{
			_explodeMultipartResult = explodeMultipartResult;
			_storeOverlapsAsNewFeatures = storeOverlapsAsNewFeatures;

			Result = new RemoveOverlapsResult();
		}

		public IFlexibleSettingProvider<ChangeAlongZSource> ZSourceProvider { get; set; }

		[NotNull]
		public RemoveOverlapsResult Result { get; }

		/// <summary>
		/// Calculates the result geometries and adds them to the relevant dictionaries.
		/// </summary>
		/// <param name="fromFeatures"></param>
		/// <param name="overlaps"></param>
		/// <param name="targetFeaturesForVertexInsertion"></param>
		/// <param name="trackCancel"></param>
		public void CalculateResults(
			[NotNull] IEnumerable<IFeature> fromFeatures,
			[NotNull] Overlaps overlaps,
			[CanBeNull] ICollection<IFeature> targetFeaturesForVertexInsertion = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			Assert.ArgumentNotNull(fromFeatures, nameof(fromFeatures));
			Assert.ArgumentNotNull(overlaps, nameof(overlaps));

			foreach (IFeature feature in fromFeatures)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				var gdbObjRef = new GdbObjectReference(feature);

				IList<IGeometry> overlapsForFeature;
				if (! overlaps.OverlapsBySourceRef.TryGetValue(gdbObjRef, out overlapsForFeature))
				{
					_msg.DebugFormat("No overlaps for feature {0}", gdbObjRef);
					continue;
				}

				IPolycurve overlapGeometry = (IPolycurve) GeometryUtils.Union(overlapsForFeature);

				ProcessFeature(feature, overlapGeometry);

				if (targetFeaturesForVertexInsertion != null)
				{
					// TODO: Filter target features using spatial index!
					InsertIntersectingVerticesInTargets(targetFeaturesForVertexInsertion,
					                                    overlapGeometry);
				}
			}
		}

		/// <summary>
		/// Calculates the result geometries and adds them to the relevant dictionaries.
		/// </summary>
		/// <param name="fromFeatures"></param>
		/// <param name="overlap"></param>
		/// <param name="targetFeaturesForVertexInsertion"></param>
		/// <param name="trackCancel"></param>
		public void CalculateResults(
			[NotNull] IEnumerable<IFeature> fromFeatures,
			[NotNull] IPolycurve overlap,
			[CanBeNull] IEnumerable<IFeature> targetFeaturesForVertexInsertion = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			Assert.ArgumentNotNull(fromFeatures, nameof(fromFeatures));
			Assert.ArgumentNotNull(overlap, nameof(overlap));

			GeometryUtils.AllowIndexing(overlap);

			foreach (IFeature feature in fromFeatures)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				ProcessFeature(feature, overlap);
			}

			if (targetFeaturesForVertexInsertion != null)
			{
				InsertIntersectingVerticesInTargets(targetFeaturesForVertexInsertion,
				                                    overlap);
			}
		}

		public void InsertIntersectingVerticesInTargets(
			[NotNull] IEnumerable<IFeature> targetFeatures,
			[NotNull] IPolycurve removeGeometry)
		{
			if (Result.TargetFeaturesToUpdate == null)
			{
				Result.TargetFeaturesToUpdate = new Dictionary<IFeature, IGeometry>();
			}

			ReshapeUtils.InsertIntersectingVerticesInTargets(
				targetFeatures, removeGeometry,
				Result.TargetFeaturesToUpdate);
		}

		private void ProcessFeature([NotNull] IFeature feature,
		                            [NotNull] IPolycurve overlappingGeometry)
		{
			IGeometry featureShape = feature.Shape;

			if (featureShape is IMultiPatch)
			{
				// Multipatches could have inverted rings which makes disjoint tests unreliable
				if (GeometryUtils.Disjoint(featureShape.Envelope, overlappingGeometry))
				{
					return;
				}
			}
			else if (GeometryUtils.Disjoint(featureShape, overlappingGeometry))
			{
				return;
			}

			IList<IGeometry> overlappingResults;

			string note = null;
			ChangeAlongZSource zSource = ZSourceProvider?.GetValue(feature, out note) ??
			                             ChangeAlongZSource.Target;

			if (note != null)
			{
				_msg.Info(note);
			}

			var sourceMultipatch = featureShape as IMultiPatch;

			IList<IGeometry> modifiedGeometries =
				sourceMultipatch != null
					? RemoveMultipatchOverlaps(sourceMultipatch, (IPolygon) overlappingGeometry,
					                           zSource, out overlappingResults)
					: RemoveOverlap((IPolycurve) featureShape, overlappingGeometry,
					                zSource, out overlappingResults);

			// additional check for undefined z values - this happens if the target has no Zs or UseSourceZs is active
			// -> currently the undefined Zs are interpolated before storing
			if (HasAnyGeometryUndefinedZs(modifiedGeometries))
			{
				_msg.DebugFormat(
					"The result geometry of {0} has undefined z values.",
					GdbObjectUtils.ToString(feature));
			}

			OverlapResultGeometries singleFeatureResult =
				new OverlapResultGeometries(feature, modifiedGeometries,
				                            overlappingResults);

			Result.ResultsByFeature.Add(singleFeatureResult);
		}

		private static bool HasAnyGeometryUndefinedZs(
			IEnumerable<IGeometry> modifiedGeometries)
		{
			foreach (IGeometry modifiedGeometry in modifiedGeometries)
			{
				if (GeometryUtils.HasUndefinedZValues(modifiedGeometry))
				{
					_msg.DebugFormat("Geometry with undefined Zs: {0}",
					                 GeometryUtils.ToString(modifiedGeometry));
					return true;
				}
			}

			return false;
		}

		private IList<IGeometry> CalculateOverlappingGeometries(
			IPolycurve sourceGeometry,
			IPolycurve overlapPolycurve)
		{
			esriGeometryDimension resultDimension =
				sourceGeometry.GeometryType == esriGeometryType.esriGeometryPolyline
					? esriGeometryDimension.esriGeometry1Dimension
					: esriGeometryDimension.esriGeometry2Dimension;

			var intersection = (IPolycurve)
				((ITopologicalOperator) sourceGeometry).Intersect(overlapPolycurve,
					resultDimension);

			GeometryUtils.Simplify(intersection);

			if (intersection.IsEmpty)
			{
				return new List<IGeometry>(0);
			}

			return _explodeMultipartResult && IsMultipart(intersection)
				       ? GeometryUtils.Explode(intersection)
				       : new List<IGeometry> { intersection };
		}

		/// <summary>
		/// Removes an overlapping geometry from a source polycurve.
		/// </summary>
		/// <param name="fromGeometry">The source geometry.</param>
		/// <param name="overlaps"></param>
		/// <param name="zSource"></param>
		/// <param name="overlappingResults"></param>
		/// <returns></returns>
		[NotNull]
		private IList<IGeometry> RemoveOverlap(
			[NotNull] IPolycurve fromGeometry,
			[NotNull] IPolycurve overlaps,
			ChangeAlongZSource zSource,
			out IList<IGeometry> overlappingResults)
		{
			Assert.ArgumentNotNull(fromGeometry, nameof(fromGeometry));
			Assert.ArgumentNotNull(overlaps, nameof(overlaps));

			overlaps = ChangeAlongZUtils.PrepareCutPolylineZs(overlaps, zSource);

			Plane3D plane = null;
			if (zSource == ChangeAlongZSource.SourcePlane)
			{
				plane = ChangeZUtils.GetPlane(
					GeometryConversionUtils.GetPntList(fromGeometry),
					GeometryUtils.GetZTolerance(fromGeometry));
			}

			IGeometry rawResult = GetDifference(fromGeometry, overlaps);

			if (plane != null)
			{
				ChangeAlongZUtils.AssignZ((IPointCollection) rawResult, plane);
			}

			if (zSource == ChangeAlongZSource.InterpolatedSource)
			{
				GeometryUtils.TrySimplifyZ(rawResult);
			}

			int originalPartCount = GetRelevantPartCount(fromGeometry);
			int resultPartCount = GetRelevantPartCount(rawResult);

			// TODO: This works for simple cases. To be correct the difference operation and part comparison would have to be 
			//       done on a per-part basis, because in the same operation one part could be added and one removed.
			//       Just comparing part counts before and after is misleading in such cases.
			if (resultPartCount > originalPartCount)
			{
				Result.ResultHasMultiparts = true;
			}

			IList<IGeometry> result = new List<IGeometry>();

			// TODO explode only those input parts that where cut into more than one result part
			// --> preserve other input parts in original geometry

			if (rawResult is IPolycurve && _explodeMultipartResult &&
			    IsMultipart(rawResult))
			{
				// Exploding all selected multipart geos
				// to explode geos only if not avoidable, 
				// use GetPositivePartCount and compare counts before and after cut operation
				foreach (IGeometry geometry in GeometryUtils.Explode(rawResult))
				{
					var part = (IPolycurve) geometry;
					result.Add(part);
				}

				if (((IGeometryCollection) fromGeometry).GeometryCount > 1)
				{
					// TODO: Fix this situation by checking which individual parts were split by the overlap and only turn them into a new feature
					_msg.Warn(
						"The selection included multi-part geometry. Storing this geometry generated several copies of the feature.");
				}
			}
			else
			{
				result.Add(rawResult);
			}

			overlappingResults = _storeOverlapsAsNewFeatures
				                     ? CalculateOverlappingGeometries(fromGeometry,
					                     overlaps)
				                     : null;
			return result;
		}

		private static IGeometry GetDifference(IPolycurve fromGeometry,
		                                       IPolycurve overlappingGeometry)
		{
			GeometryUtils.AllowIndexing(fromGeometry);

			IGeometry result =
				((ITopologicalOperator) fromGeometry).Difference(overlappingGeometry);

			GeometryUtils.EnsureSpatialReference(result, fromGeometry.SpatialReference);
			GeometryUtils.EnsureSimple(result);
			return result;
		}

		#region Multipatches

		private IList<IGeometry> RemoveMultipatchOverlaps(
			[NotNull] IMultiPatch sourceMultipatch,
			[NotNull] IPolygon overlaps,
			ChangeAlongZSource zSource,
			out IList<IGeometry> overlappingMultiPatches)
		{
			// Only remove overlaps from multipatch rings that
			if (! _onlyBoundaryTouchingMultipatches)
			{
				return RemoveOverlaps(sourceMultipatch, overlaps, zSource,
				                      out overlappingMultiPatches);
			}

			Dictionary<IMultiPatch, bool> intersectingByMultipatchPart =
				Intersects3dPerMultipatchPart(sourceMultipatch, overlaps);

			bool anyPart3dIntersects = intersectingByMultipatchPart.Any(kvp => kvp.Value);

			// TODO: If anyPart3dIntersects is false but several parts intersect in 2d there is one
			//       case where the user might want to disambiguate: If the cut line is completely
			//       within both rings -> either 3D-intersect property (area-based) or check if at
			//       least one intersection point is a 3D intersection (i.e. in the plane of the ring).

			var resultParts = new List<IGeometry>();
			int partId = 0;

			foreach (var kvp in intersectingByMultipatchPart)
			{
				IMultiPatch partPatch = kvp.Key;
				bool part3dIntersects = kvp.Value;

				if (part3dIntersects || ! anyPart3dIntersects)
				{
					IList<IGeometry> partResults = RemoveOverlaps(
						partPatch, overlaps, zSource, out _);

					foreach (IGeometry partResult in partResults)
					{
						AssignConstantPointID(partResult, partId++);
					}

					resultParts.AddRange(partResults);
				}
				else
				{
					// Add unmodified part (with potentially different PointID):
					AssignConstantPointID(partPatch, partId++);
					resultParts.Add(partPatch);
				}
			}

			Assert.False(_storeOverlapsAsNewFeatures,
			             "Unsupported option:  store overlaps as new features");
			overlappingMultiPatches = null;

			return new List<IGeometry> { GeometryUtils.Union(resultParts) };
		}

		private IList<IGeometry> RemoveOverlaps(
			[NotNull] IMultiPatch sourceMultipatch,
			[NotNull] IPolygon overlaps,
			ChangeAlongZSource zSource,
			out IList<IGeometry> overlappingMultiPatches)
		{
			IList<IGeometry> result = new List<IGeometry>();

			// TEST
			double tolerance = GeometryUtils.GetXyTolerance(sourceMultipatch);

			Polyhedron polyhedron =
				GeometryConversionUtils.CreatePolyhedron(sourceMultipatch, false, true);
			MultiPolycurve target = GeometryConversionUtils.CreateMultiPolycurve(overlaps);

			// TODO: Remember inverted ring groups and reverse results at the end!
			//       Currently, the result is always upward oriented.

			var resultHedra = new List<RingGroup>();
			foreach (RingGroup source in polyhedron.RingGroups)
			{
				if (source.IsVertical(tolerance))
				{
					IEnumerable<RingGroup> verticalResultRings =
						RemoveOverlapsFromVerticalRings(source, target, tolerance);

					resultHedra.AddRange(verticalResultRings);
				}
				else
				{
					MultiLinestring differenceAreasXY =
						GeomTopoOpUtils.GetDifferenceAreasXY(source, target, tolerance, zSource);

					resultHedra.AddRange(ToRingGroups(differenceAreasXY));
				}
			}

			result.Add(GeometryConversionUtils.CreateMultipatch(resultHedra, sourceMultipatch));

			overlappingMultiPatches = null;
			return result;
		}

		private static IEnumerable<RingGroup> RemoveOverlapsFromVerticalRings(
			[NotNull] RingGroup source,
			[NotNull] MultiPolycurve target,
			double tolerance)
		{
			// The cut rings or those that are not cut because they do not intersect the target:
			var ringsToCheck = new List<RingGroup>();

			IList<RingGroup> cutRingGroups = GeomTopoOpUtils.CutPlanar(source, target, tolerance);

			if (cutRingGroups.Count == 0)
			{
				// The ring was not cut -> add if contained
				ringsToCheck.Add(source);
			}

			foreach (RingGroup cutRingGroup in cutRingGroups)
			{
				ringsToCheck.Add(cutRingGroup);
			}

			foreach (RingGroup ringToCheck in ringsToCheck)
			{
				// Rings should be completely inside or completely outside!

				bool? cutRingIsInTarget =
					GeomRelationUtils.AreaContainsXY(target, ringToCheck, tolerance);

				// Ignore those rings inside. If it is on the boundary (i.e. cutRingIsInTarget == null),
				// keep it (?) -> TODO: Test
				if (cutRingIsInTarget != true)
				{
					yield return ringToCheck;
				}
			}
		}

		private static Dictionary<IMultiPatch, bool> Intersects3dPerMultipatchPart(
			IMultiPatch sourceMultipatch,
			IPolygon overlaps)
		{
			var originalParts =
				GeometryPart.FromGeometry(sourceMultipatch, true)
				            .Select(
					            p => (IMultiPatch) p.CreateAsHighLevelGeometry(sourceMultipatch))
				            .ToList();

			var intersectingByMultipatchPart = new Dictionary<IMultiPatch, bool>();

			// Find 3d intersecting parts, remember rings
			foreach (IMultiPatch multipatchPart in originalParts)
			{
				bool intersects3d = Has3dIntersectionAtBoundary(multipatchPart, overlaps);
				intersectingByMultipatchPart.Add(multipatchPart, intersects3d);
			}

			return intersectingByMultipatchPart;
		}

		private static void AssignConstantPointID(IGeometry geometry, int pointId)
		{
			var geometryCollection = geometry as IGeometryCollection;

			if (geometryCollection == null)
			{
				throw new ArgumentException("Geometry is not a high-level geometry collection");
			}

			for (int partIdx = 0; partIdx < geometryCollection.GeometryCount; partIdx++)
			{
				GeometryUtils.AssignConstantPointID(geometryCollection, partIdx, pointId);
			}
		}

		/// <summary>
		/// Determines whether the multipatch has a 3D intersection with the cut polygon at any
		/// of its rings' boundary.
		/// </summary>
		/// <param name="multiPatch"></param>
		/// <param name="cutPolygon"></param>
		/// <returns></returns>
		private static bool Has3dIntersectionAtBoundary([NotNull] IMultiPatch multiPatch,
		                                                [NotNull] IPolygon cutPolygon)
		{
			var multipatchRingGroupsWith3dIntersection = new List<RingGroup>();

			double tolerance = GeometryUtils.GetXyTolerance(multiPatch);

			// Convert cut polygon to RingGroup
			RingGroup cutPolygonRingGroup = GeometryConversionUtils.CreateRingGroup(cutPolygon);

			// Check each ring group in the multipatch for 3D boundary intersections
			foreach (RingGroup multipatchRingGroup in
			         GeometryConversionUtils.CreateRingGroups(multiPatch))
			{
				IList<IntersectionPoint3D> intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) multipatchRingGroup, (ISegmentList) cutPolygonRingGroup,
						tolerance, false);

				bool has3dIntersection = false;
				foreach (IntersectionPoint3D intersectionPoint in intersectionPoints)
				{
					if (intersectionPoint.Is3dIntersection(cutPolygonRingGroup, tolerance) == true)
					{
						has3dIntersection = true;
					}
				}

				if (has3dIntersection)
				{
					multipatchRingGroupsWith3dIntersection.Add(multipatchRingGroup);
				}
			}

			return multipatchRingGroupsWith3dIntersection.Any();
		}

		private static IList<RingGroup> ToRingGroups(MultiLinestring MultiLinestring)
		{
			var result = new List<RingGroup>();

			foreach (Linestring linestring in MultiLinestring.GetLinestrings())
			{
				if (linestring.ClockwiseOriented == false)
				{
					result.Last().AddInteriorRing(linestring);
				}
				else
				{
					result.Add(new RingGroup(linestring));
				}
			}

			return result;
		}

		#endregion

		private static bool IsMultipart(IGeometry geometry)
		{
			return GetRelevantPartCount(geometry) > 1;
		}

		private static int GetRelevantPartCount(IGeometry geometry)
		{
			var polygon = geometry as IPolygon;

			if (polygon != null)
			{
				return GeometryUtils.GetExteriorRingCount(polygon, allowSimplify: false);
			}

			return ((IGeometryCollection) geometry).GeometryCount;
		}
	}
}
