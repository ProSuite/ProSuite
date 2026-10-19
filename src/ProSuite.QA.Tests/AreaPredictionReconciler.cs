using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reconciles predictions produced by overlapping, independently processed TIFFs.
	/// Complete duplicate predictions are reduced to one representative. Predictions
	/// clipped by a TIFF boundary are dissolved when matching fragments from multiple
	/// TIFFs are available; unresolved fragments are rejected.
	/// </summary>
	public class AreaPredictionReconciler
	{
		private const double BoundaryTolerance = 0.5;
		private const double DuplicateIntersectionRatio = 0.85;
		private const double DuplicateIou = 0.70;
		private const double FragmentIntersectionRatio = 0.50;

		[NotNull]
		public AreaPredictionReconciliationResult Reconcile(
			[NotNull] IList<PredictionRequestTile> requestTiles,
			[NotNull] IList<AreaPredictionJobResult> jobResults,
			[CanBeNull] IEnvelope completeExtent = null)
		{
			Assert.ArgumentNotNull(requestTiles, nameof(requestTiles));
			Assert.ArgumentNotNull(jobResults, nameof(jobResults));
			Assert.ArgumentCondition(requestTiles.Count == jobResults.Count,
			                         "Request tile and job result counts differ");
			bool releaseProcessingExtent = completeExtent == null;
			IEnvelope processingExtent = completeExtent ?? GetCompleteExtent(requestTiles);
			var candidates = new List<PredictionCandidate>();
			var accepted = new List<IPolygon>();
			try
			{
				for (var tileIndex = 0; tileIndex < requestTiles.Count; tileIndex++)
				{
					AreaPredictionJobResult job = jobResults[tileIndex];
					if (! job.Succeeded)
					{
						continue;
					}

					PredictionRequestTile tile = requestTiles[tileIndex];
					foreach (AreaPrediction prediction in job.Predictions)
					{
						IPolygon polygon = CreatePolygon(
							prediction, tile.TileInfo.SpatialReference);
						var keepPolygon = false;
						try
						{
							if (polygon.IsEmpty)
							{
								continue;
							}

							var candidate = new PredictionCandidate(
								candidates.Count, tileIndex, tile, polygon,
								processingExtent);
							if (candidate.Area > 0)
							{
								candidates.Add(candidate);
								keepPolygon = true;
							}
						}
						finally
						{
							if (! keepPolygon)
							{
								ComUtils.ReleaseComObject(polygon);
							}
						}
					}
				}

				IList<IList<PredictionCandidate>> groups = CreateGroups(candidates);
				var duplicateCount = 0;
				var boundaryCandidateCount = 0;
				var mergedFragmentGroupCount = 0;
				var rejectedFragmentCount = 0;

				foreach (IList<PredictionCandidate> group in groups)
				{
					boundaryCandidateCount +=
						group.Count(candidate => candidate.TouchesBoundary);
					IList<PredictionCandidate> complete = group
						.Where(candidate => ! candidate.TouchesBoundary)
						.ToList();

					if (complete.Count > 0)
					{
						PredictionCandidate selected = SelectBest(complete);
						accepted.Add(GeometryFactory.Clone(selected.Polygon));
						duplicateCount += group.Count - 1;
						continue;
					}

					IList<PredictionCandidate> distinctTileFragments = group
						.GroupBy(candidate => candidate.TileIndex)
						.Select(candidatesForTile =>
							        SelectBest(candidatesForTile.ToList()))
						.ToList();

					if (distinctTileFragments.Count >= 2 &&
					    TryUnionFragments(distinctTileFragments, out IPolygon merged))
					{
						accepted.Add(merged);
						mergedFragmentGroupCount++;
						duplicateCount += group.Count - distinctTileFragments.Count;
					}
					else
					{
						rejectedFragmentCount += group.Count;
					}
				}

				var statistics = new AreaPredictionReconciliationStatistics(
					candidates.Count, boundaryCandidateCount, duplicateCount,
					mergedFragmentGroupCount, rejectedFragmentCount, accepted.Count);
				return new AreaPredictionReconciliationResult(accepted, statistics);
			}
			catch
			{
				foreach (IPolygon polygon in accepted)
				{
					ComUtils.ReleaseComObject(polygon);
				}

				throw;
			}
			finally
			{
				foreach (PredictionCandidate candidate in candidates)
				{
					ComUtils.ReleaseComObject(candidate.Envelope);
					ComUtils.ReleaseComObject(candidate.Polygon);
				}

				if (releaseProcessingExtent)
				{
					ComUtils.ReleaseComObject(processingExtent);
				}
			}
		}

		[NotNull]
		private static IEnvelope GetCompleteExtent(
			[NotNull] IEnumerable<PredictionRequestTile> requestTiles)
		{
			IEnvelope result = null;
			foreach (PredictionRequestTile tile in requestTiles)
			{
				if (result == null)
				{
					result = GeometryFactory.Clone(tile.CoreExtent);
				}
				else
				{
					result.Union(tile.CoreExtent);
				}
			}

			return Assert.NotNull(result, "At least one request tile is required");
		}

		private static bool TryUnionFragments(
			[NotNull] IList<PredictionCandidate> fragments,
			out IPolygon merged)
		{
			var polygons = new List<IGeometry>(fragments.Count);
			IGeometry union = null;
			try
			{
				foreach (PredictionCandidate fragment in fragments)
				{
					polygons.Add(GeometryFactory.Clone(fragment.Polygon));
				}

				union = GeometryUtils.Union(polygons);
				merged = union as IPolygon;
				if (merged == null || merged.IsEmpty)
				{
					ComUtils.ReleaseComObject(union);
					union = null;
					merged = null;
					return false;
				}

				GeometryUtils.Simplify(merged);
				if (merged.ExteriorRingCount != 1)
				{
					ComUtils.ReleaseComObject(merged);
					union = null;
					merged = null;
					return false;
				}

				union = null;
				return true;
			}
			finally
			{
				ComUtils.ReleaseComObject(union);
				foreach (IGeometry polygon in polygons)
				{
					ComUtils.ReleaseComObject(polygon);
				}
			}
		}

		[NotNull]
		private static IList<IList<PredictionCandidate>> CreateGroups(
			[NotNull] IList<PredictionCandidate> candidates)
		{
			var parents = Enumerable.Range(0, candidates.Count).ToArray();
			IList<IGrouping<int, PredictionCandidate>> candidatesByTile = candidates
				.GroupBy(candidate => candidate.TileIndex)
				.ToList();

			for (var firstTileIndex = 0;
			     firstTileIndex < candidatesByTile.Count;
			     firstTileIndex++)
			{
				IList<PredictionCandidate> firstTileCandidates =
					candidatesByTile[firstTileIndex].ToList();
				IEnvelope firstRequestExtent =
					firstTileCandidates[0].Tile.TileInfo.Extent;

				for (var secondTileIndex = firstTileIndex + 1;
				     secondTileIndex < candidatesByTile.Count;
				     secondTileIndex++)
				{
					IList<PredictionCandidate> secondTileCandidates =
						candidatesByTile[secondTileIndex].ToList();
					IEnvelope secondRequestExtent =
						secondTileCandidates[0].Tile.TileInfo.Extent;
					if (! EnvelopesIntersect(firstRequestExtent, secondRequestExtent))
					{
						continue;
					}

					double sharedXMin = Math.Max(
						firstRequestExtent.XMin, secondRequestExtent.XMin);
					double sharedYMin = Math.Max(
						firstRequestExtent.YMin, secondRequestExtent.YMin);
					double sharedXMax = Math.Min(
						firstRequestExtent.XMax, secondRequestExtent.XMax);
					double sharedYMax = Math.Min(
						firstRequestExtent.YMax, secondRequestExtent.YMax);

					IEnumerable<PredictionCandidate> firstSharedCandidates =
						firstTileCandidates.Where(
							candidate => Intersects(
								candidate.Envelope,
								sharedXMin, sharedYMin, sharedXMax, sharedYMax));
					IList<PredictionCandidate> secondSharedCandidates =
						secondTileCandidates.Where(
							candidate => Intersects(
								candidate.Envelope,
								sharedXMin, sharedYMin, sharedXMax, sharedYMax))
						                    .ToList();

					foreach (PredictionCandidate first in firstSharedCandidates)
					{
						foreach (PredictionCandidate second in secondSharedCandidates)
						{
							if (AreMatching(first, second))
							{
								Union(parents, first.Id, second.Id);
							}
						}
					}
				}
			}

			return candidates.GroupBy(candidate => Find(parents, candidate.Id))
			                 .Select(group => (IList<PredictionCandidate>) group.ToList())
			                 .ToList();
		}

		private static bool AreMatching(PredictionCandidate first,
		                                PredictionCandidate second)
		{
			if (first.TileIndex == second.TileIndex ||
			    ! EnvelopesIntersect(first.Tile.TileInfo.Extent,
			                         second.Tile.TileInfo.Extent) ||
			    ! EnvelopesIntersect(first.Envelope, second.Envelope))
			{
				return false;
			}

			double intersectionArea = GetIntersectionArea(first.Polygon, second.Polygon);
			if (intersectionArea <= 0)
			{
				return false;
			}

			double minimumArea = Math.Min(first.Area, second.Area);
			double unionArea = first.Area + second.Area - intersectionArea;
			double intersectionRatio = intersectionArea / minimumArea;
			double iou = intersectionArea / unionArea;

			if (first.TouchesBoundary || second.TouchesBoundary)
			{
				return intersectionRatio >= FragmentIntersectionRatio;
			}

			return intersectionRatio >= DuplicateIntersectionRatio ||
			       iou >= DuplicateIou;
		}

		private static double GetIntersectionArea(IPolygon first, IPolygon second)
		{
			IGeometry intersection = null;
			try
			{
				intersection = IntersectionUtils.Intersect(
					first, second, esriGeometryDimension.esriGeometry2Dimension);
				return intersection.IsEmpty ? 0 : GeometryProperties.GetArea(intersection);
			}
			finally
			{
				ComUtils.ReleaseComObject(intersection);
			}
		}

		private static bool EnvelopesIntersect(IEnvelope first, IEnvelope second)
		{
			return first.XMin <= second.XMax && first.XMax >= second.XMin &&
			       first.YMin <= second.YMax && first.YMax >= second.YMin;
		}

		private static bool Intersects(IEnvelope envelope,
		                               double xMin, double yMin,
		                               double xMax, double yMax)
		{
			return envelope.XMin <= xMax && envelope.XMax >= xMin &&
			       envelope.YMin <= yMax && envelope.YMax >= yMin;
		}

		[NotNull]
		private static PredictionCandidate SelectBest(
			[NotNull] IList<PredictionCandidate> candidates)
		{
			return candidates.OrderByDescending(candidate => candidate.BoundaryDistance)
			                 .ThenByDescending(candidate => candidate.Area)
			                 .ThenBy(candidate => candidate.TileIndex)
			                 .First();
		}

		[NotNull]
		internal static IPolygon CreatePolygon(
			[NotNull] AreaPrediction prediction,
			[CanBeNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(prediction, nameof(prediction));
			var points = new List<WKSPointZ>(prediction.Shell.Count + 1);
			foreach (PredictionPoint point in prediction.Shell)
			{
				points.Add(new WKSPointZ { X = point.X, Y = point.Y, Z = 0 });
			}

			WKSPointZ first = points[0];
			WKSPointZ last = points[points.Count - 1];
			if (! first.X.Equals(last.X) || ! first.Y.Equals(last.Y))
			{
				points.Add(first);
			}

			IPolygon polygon = GeometryFactory.CreatePolygon(
				points.ToArray(), spatialReference);
			GeometryUtils.Simplify(polygon);
			GeometryUtils.MakeNonZAware(polygon);
			return polygon;
		}

		private static int Find(int[] parents, int value)
		{
			while (parents[value] != value)
			{
				parents[value] = parents[parents[value]];
				value = parents[value];
			}

			return value;
		}

		private static void Union(int[] parents, int first, int second)
		{
			int firstRoot = Find(parents, first);
			int secondRoot = Find(parents, second);
			if (firstRoot != secondRoot)
			{
				parents[secondRoot] = firstRoot;
			}
		}

		internal class PredictionCandidate
		{
			public PredictionCandidate(int id, int tileIndex,
			                           [NotNull] PredictionRequestTile tile,
			                           [NotNull] IPolygon polygon,
			                           [NotNull] IEnvelope completeExtent)
			{
				Id = id;
				TileIndex = tileIndex;
				Tile = tile;
				Polygon = polygon;
				Area = GeometryProperties.GetArea(polygon);

				Envelope = polygon.Envelope;
				IEnvelope requestExtent = tile.TileInfo.Extent;
				double leftDistance = Envelope.XMin - requestExtent.XMin;
				double rightDistance = requestExtent.XMax - Envelope.XMax;
				double bottomDistance = Envelope.YMin - requestExtent.YMin;
				double topDistance = requestExtent.YMax - Envelope.YMax;
				BoundaryDistance = Math.Min(
					Math.Min(leftDistance, rightDistance),
					Math.Min(bottomDistance, topDistance));
				TouchesBoundary =
					(requestExtent.XMin > completeExtent.XMin + BoundaryTolerance &&
					 leftDistance <= BoundaryTolerance) ||
					(requestExtent.XMax < completeExtent.XMax - BoundaryTolerance &&
					 rightDistance <= BoundaryTolerance) ||
					(requestExtent.YMin > completeExtent.YMin + BoundaryTolerance &&
					 bottomDistance <= BoundaryTolerance) ||
					(requestExtent.YMax < completeExtent.YMax - BoundaryTolerance &&
					 topDistance <= BoundaryTolerance);
			}

			public int Id { get; }
			public int TileIndex { get; }
			[NotNull] public PredictionRequestTile Tile { get; }
			[NotNull] public IPolygon Polygon { get; }
			[NotNull] public IEnvelope Envelope { get; }
			public double Area { get; }
			public double BoundaryDistance { get; }
			public bool TouchesBoundary { get; }
		}
	}

	public class AreaPredictionReconciliationResult
	{
		public AreaPredictionReconciliationResult(
			[NotNull] IList<IPolygon> predictions,
			[NotNull] AreaPredictionReconciliationStatistics statistics)
		{
			Predictions = predictions;
			Statistics = statistics;
		}

		[NotNull] public IList<IPolygon> Predictions { get; }
		[NotNull] public AreaPredictionReconciliationStatistics Statistics { get; }
	}

	public class AreaPredictionReconciliationStatistics
	{
		public AreaPredictionReconciliationStatistics(
			int candidateCount, int boundaryCandidateCount, int duplicateCount,
			int mergedFragmentGroupCount = 0, int rejectedFragmentCount = 0,
			int finalPredictionCount = 0)
		{
			CandidateCount = candidateCount;
			BoundaryCandidateCount = boundaryCandidateCount;
			DuplicateCount = duplicateCount;
			MergedFragmentGroupCount = mergedFragmentGroupCount;
			RejectedFragmentCount = rejectedFragmentCount;
			FinalPredictionCount = finalPredictionCount;
		}

		public int CandidateCount { get; }
		public int BoundaryCandidateCount { get; }
		public int DuplicateCount { get; }
		public int MergedFragmentGroupCount { get; }
		public int RejectedFragmentCount { get; }
		public int FinalPredictionCount { get; }
	}
}
