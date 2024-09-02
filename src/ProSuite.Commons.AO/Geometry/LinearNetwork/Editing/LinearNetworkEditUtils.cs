using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork.Editing
{
	public static class LinearNetworkEditUtils
	{
		public static IList<IFeature> SplitAtJunctions(
			[NotNull] IFeature edgeFeature,
			[NotNull] IEnumerable<IFeature> splittingJunctions)
		{
			var splitPoints =
				(IPointCollection) GeometryFactory.CreateMultipoint(
					splittingJunctions.Select(f => (IPoint) f.Shape));

			List<IFeature> result = SplitAtPoints(edgeFeature, splitPoints);

			return result;
		}

		public static List<IFeature> SplitAtPoints([NotNull] IFeature edgeFeature,
		                                           [NotNull] IPointCollection splitPoints)
		{
			const bool projectPointsOntoPathToSplit = false;
			const bool createParts = true;

			var edge = (IPolyline) edgeFeature.Shape;

			IList<IPoint> splittedAt = GeometryUtils.CrackPolycurve(
				edge, splitPoints, projectPointsOntoPathToSplit, createParts);

			var result = new List<IFeature>();

			if (splittedAt.Count > 0)
			{
				IList<IGeometry> splitGeometries = GeometryUtils.Explode(edge);

				IGeometry geometryToStoreInOriginal =
					Assert.NotNull(GeometryUtils.GetLargestGeometry(splitGeometries));

				// store the update
				IFeature existingFeature = AssignGeometryToFeature(geometryToStoreInOriginal,
					edgeFeature, false);

				existingFeature.Store();

				// store other new geometries as inserts
				foreach (IGeometry modifyGeometry in
				         splitGeometries.Where(polycurve => polycurve != geometryToStoreInOriginal))
				{
					IFeature newEdgeFeature =
						AssignGeometryToFeature(modifyGeometry, edgeFeature, true);
					newEdgeFeature.Store();

					result.Add(newEdgeFeature);
				}
			}

			return result;
		}

		/// <summary>
		/// Stores the result of a merge operation by updating the specified survivor feature
		/// with the merged geometry and deleting the features that are not needed any more.
		/// In case the features are geometric network edges the network junctions between the
		/// original features can be deleted, if required by the provided predicate.
		/// </summary>
		/// <param name="survivingFeature">The feature that should remain after the merge operation.</param>
		/// <param name="featuresToDelete">The features that should be deleted by the merge operation.</param>
		/// <param name="mergedGeometry">The merged geometry.</param>
		/// <param name="linearNetworkFeatureFinder"></param>
		/// <param name="deleteIntermediateNetworkJunctions">The predicate that determines
		/// which intermediate junctions (between the original edges) should be deleted.</param>
		/// <param name="deletedJunctionIDs">The junctions that were deleted.</param>
		/// <remarks>Should be called within an edit operation.</remarks>
		public static void StoreEdgeFeatureMerge(
			[NotNull] IFeature survivingFeature,
			[NotNull] IEnumerable<IFeature> featuresToDelete,
			[NotNull] IPolyline mergedGeometry,
			[CanBeNull] ILinearNetworkFeatureFinder linearNetworkFeatureFinder,
			[CanBeNull] Predicate<IFeature> deleteIntermediateNetworkJunctions,
			out List<long> deletedJunctionIDs)
		{
			Assert.ArgumentNotNull(survivingFeature, nameof(survivingFeature));
			Assert.ArgumentNotNull(featuresToDelete, nameof(featuresToDelete));
			Assert.ArgumentNotNull(mergedGeometry, nameof(mergedGeometry));

			deletedJunctionIDs = new List<long>();

			foreach (IFeature featureToDelete in featuresToDelete)
			{
				if (linearNetworkFeatureFinder != null)
				{
					IPolyline edgePolyline = (IPolyline) featureToDelete.Shape;

					IList<IFeature> fromJunctions =
						linearNetworkFeatureFinder.FindJunctionFeaturesAt(edgePolyline.FromPoint);

					deletedJunctionIDs.AddRange(
						DeleteIntermediateJunctions(fromJunctions,
						                            deleteIntermediateNetworkJunctions,
						                            linearNetworkFeatureFinder,
						                            mergedGeometry));

					IList<IFeature> toJunctions =
						linearNetworkFeatureFinder.FindJunctionFeaturesAt(edgePolyline.ToPoint);

					deletedJunctionIDs.AddRange(
						DeleteIntermediateJunctions(toJunctions,
						                            deleteIntermediateNetworkJunctions,
						                            linearNetworkFeatureFinder,
						                            mergedGeometry));
				}

				featureToDelete.Delete();
			}

			GdbObjectUtils.SetFeatureShape(survivingFeature, mergedGeometry);

			survivingFeature.Store();
		}

		public static bool SnapPoint([NotNull] IPoint newEndPoint,
		                             [NotNull] IList<IFeature> candidateTargetEdges,
		                             [NotNull] ILinearNetworkFeatureFinder networkFeatureFinder)
		{
			if (networkFeatureFinder.SearchTolerance <= 0)
			{
				return false;
			}

			Pnt2D newEndPnt = new Pnt2D(newEndPoint.X, newEndPoint.Y);

			IPoint fromPoint = new PointClass() { SpatialReference = newEndPoint.SpatialReference };
			IPoint toPoint = new PointClass() { SpatialReference = newEndPoint.SpatialReference };

			double searchToleranceSquared = networkFeatureFinder.SearchTolerance *
			                                networkFeatureFinder.SearchTolerance;

			bool snapped = false;
			foreach (IFeature feature in candidateTargetEdges)
			{
				IPolyline otherPolyline = (IPolyline) feature.Shape;

				otherPolyline.QueryFromPoint(fromPoint);
				Pnt2D fromPnt = new Pnt2D(fromPoint.X, fromPoint.Y);

				double fromDistance2 = fromPnt.Dist2(newEndPnt);

				otherPolyline.QueryToPoint(toPoint);
				Pnt2D toPnt = new Pnt2D(toPoint.X, toPoint.Y);

				double toDistance2 = toPnt.Dist2(newEndPnt);

				if (fromDistance2 > searchToleranceSquared && toDistance2 > searchToleranceSquared)
				{
					continue;
				}

				if (Math.Min(fromDistance2, toDistance2) < GetXyResolutionSquared(feature))
				{
					// already snapped
					continue;
				}

				IPoint sourcePoint = fromDistance2 < toDistance2 ? fromPoint : toPoint;

				snapped = true;
				CopyCoords(sourcePoint, newEndPoint);
			}

			if (! snapped)
			{
				// Try snapping onto curve interior
				foreach (IFeature feature in candidateTargetEdges)
				{
					IPolyline otherPolyline = (IPolyline) feature.Shape;

					double distanceFromCurve =
						GeometryUtils.GetDistanceFromCurve(newEndPoint, otherPolyline, fromPoint);

					if (distanceFromCurve >= 0 &&
					    distanceFromCurve < networkFeatureFinder.SearchTolerance)
					{
						snapped = true;
						CopyCoords(fromPoint, newEndPoint);
					}
				}
			}

			return snapped;
		}

		private static double GetXyResolutionSquared(IFeature feature)
		{
			double xyResolution = GeometryUtils.GetXyResolution(feature);

			return xyResolution * xyResolution;
		}

		private static void CopyCoords(IPoint source, IPoint target)
		{
			target.PutCoords(source.X, source.Y);

			target.Z = source.Z;
		}

		private static IEnumerable<long> DeleteIntermediateJunctions(
			[NotNull] IEnumerable<IFeature> junctions,
			[CanBeNull] Predicate<IFeature> predicate,
			[NotNull] ILinearNetworkFeatureFinder linearNetworkFeatureFinder,
			[NotNull] IPolyline mergedLine)
		{
			var result = new List<long>();

			foreach (IFeature junction in junctions)
			{
				if (predicate != null && ! predicate(junction))
				{
					continue;
				}

				IPoint junctionPoint = (IPoint) junction.Shape;

				int edgeCount = linearNetworkFeatureFinder.FindEdgeFeaturesAt(junctionPoint).Count;

				if (edgeCount <= 2)
				{
					// Check if it is still needed for the merged geometry
					IPoint fromPoint = mergedLine.FromPoint;
					IPoint toPoint = mergedLine.ToPoint;

					bool neededForMergedLine =
						GeometryUtils.AreEqualInXY(junctionPoint, fromPoint) ||
						GeometryUtils.AreEqualInXY(junctionPoint, toPoint);

					if (! neededForMergedLine)
					{
						result.Add(junction.OID);
						junction.Delete();
					}
				}
			}

			return result;
		}

		[NotNull]
		private static IFeature AssignGeometryToFeature(
			[NotNull] IGeometry modifiedGeometry,
			[NotNull] IFeature originalFeature,
			bool duplicateOriginalFeature)
		{
			Assert.ArgumentCondition(! modifiedGeometry.IsEmpty,
			                         "modifiedGeometry is empty");

			IFeature resultFeature;

			if (duplicateOriginalFeature)
			{
				resultFeature = GdbObjectUtils.DuplicateFeature(
					originalFeature, true);
			}
			else
			{
				resultFeature = originalFeature;
			}

			GdbObjectUtils.SetFeatureShape(resultFeature, modifiedGeometry);

			return resultFeature;
		}
	}
}
