using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Tests.Coincidence
{
	partial class QaTopoNotNear
	{
		private class CoincidenceError
		{
			public SegmentPartWithNeighbor DroppedPart { get; set; }
			public NeighboredSegmentsSubpart UsedPart { get; set; }
		}

		private class CoincidentPartsAdapter
		{
			private IFeatureDistanceProvider NearDistanceProvider { get; }

			[CanBeNull]
			private Func<CoincidenceError, int> ReportCoincidenceError { get; }

			public CoincidentPartsAdapter(
				[NotNull] IFeatureDistanceProvider nearDistanceProvider,
				[CanBeNull] Func<CoincidenceError, int> reportCoincidenceError)
			{
				NearDistanceProvider = nearDistanceProvider;
				ReportCoincidenceError = reportCoincidenceError;
			}

			public void DropCoincidentParts(
				[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>>
					splittedParts, out int errorCount)
			{
				var dropParts = new HashSet<FeaturePoint>(new FeaturePointComparer());

				errorCount = 0;
				foreach (List<NeighboredSegmentsSubpart> featureParts in splittedParts.Values)
				{
					int partsErrorCount;
					FindDropParts(featureParts, dropParts, out partsErrorCount);
					errorCount += partsErrorCount;
				}

				if (dropParts.Count > 0)
				{
					DropParts(splittedParts, dropParts);
				}
			}

			private void FindDropParts(List<NeighboredSegmentsSubpart> featureParts,
			                           HashSet<FeaturePoint> dropParts, out int errorCount)
			{
				List<NeighboredSegmentsSubpart> nonCoincidentParts;
				FindDropParts(featureParts, dropParts, out nonCoincidentParts, out errorCount);

				if (featureParts.Count != nonCoincidentParts.Count)
				{
					featureParts.Clear();
					featureParts.AddRange(nonCoincidentParts);
				}
			}

			private void FindDropParts(
				[NotNull] List<NeighboredSegmentsSubpart> featureParts,
				[NotNull] HashSet<FeaturePoint> dropParts,
				out List<NeighboredSegmentsSubpart> nonCoincidentParts, out int errorCount)
			{
				nonCoincidentParts = new List<NeighboredSegmentsSubpart>(featureParts.Count);

				errorCount = 0;
				foreach (NeighboredSegmentsSubpart featurePart in featureParts)
				{
					int partErrorCount;
					FindDropParts(featurePart, dropParts, nonCoincidentParts, out partErrorCount);
					errorCount += partErrorCount;
				}
			}

			private void FindDropParts(NeighboredSegmentsSubpart featurePart,
			                           HashSet<FeaturePoint> dropParts,
			                           List<NeighboredSegmentsSubpart> nonCoincidentParts,
			                           out int errorCount)
			{
				Dictionary<SegmentPartWithNeighbor, List<SegmentPartWithNeighbor>>
					coincidents = null;
				SegmentNeighbors segmentsNeighbors = featurePart.SegmentNeighbors;

				var first = true;
				NearDistanceProvider.GetRowsDistance(featurePart.BaseFeature,
				                                     featurePart.TableIndex);

				for (int i = featurePart.FullMinFraction; i < featurePart.FullMaxFraction; i++)
				{
					if (! first && coincidents == null)
					{
						break;
					}

					var key = new SegmentPart(featurePart.PartIndex, i, 0, 1, true);
					SegmentParts segmentNeighbors;
					if (! segmentsNeighbors.TryGetValue(key, out segmentNeighbors))
					{
						coincidents = null;
						break;
					}

					var remaining =
						new Dictionary<SegmentPartWithNeighbor, List<SegmentPartWithNeighbor>>();

					foreach (SegmentPart segmentPart in segmentNeighbors)
					{
						var segmentNeighbor = (SegmentPartWithNeighbor) segmentPart;
						if (segmentNeighbor.NeighborIsCoincident)
						{
							if (first)
							{
								remaining.Add(
									segmentNeighbor,
									new List<SegmentPartWithNeighbor> {segmentNeighbor});
							}
							else
							{
								int segmentIndex = i - featurePart.FullMinFraction;
								FindCoincident(coincidents, segmentNeighbor, segmentIndex,
								               remaining);
							}
						}
					}

					coincidents = remaining;
					if (coincidents.Count == 0)
					{
						coincidents = null;
					}

					first = false;
				}

				if (coincidents == null)
				{
					nonCoincidentParts.Add(featurePart);
					errorCount = 0;
					return;
				}

				// Check if is lowest part
				bool keepPart = CheckKeepPart(featurePart, coincidents, out errorCount);

				if (keepPart)
				{
					DropCoincidentParts(featurePart, coincidents, dropParts, nonCoincidentParts);
				}
			}

			private static void FindCoincident(
				[NotNull] Dictionary<SegmentPartWithNeighbor, List<SegmentPartWithNeighbor>>
					coincidents,
				[NotNull] SegmentPartWithNeighbor segmentNeighbor, int segmentIndex,
				[NotNull] Dictionary<SegmentPartWithNeighbor, List<SegmentPartWithNeighbor>>
					remaining)
			{
				foreach (KeyValuePair<SegmentPartWithNeighbor,
					         List<SegmentPartWithNeighbor>> pair in coincidents)
				{
					SegmentPartWithNeighbor startCoincident = pair.Key;

					if (startCoincident.NeighborTableIndex == segmentNeighbor.NeighborTableIndex &&
					    startCoincident.NeighborFeature.OID ==
					    segmentNeighbor.NeighborFeature.OID &&
					    startCoincident.NeighborProxy.PartIndex ==
					    segmentNeighbor.NeighborProxy.PartIndex &&
					    Math.Abs(startCoincident.NeighborProxy.SegmentIndex -
					             segmentNeighbor.NeighborProxy.SegmentIndex) == segmentIndex)
					{
						pair.Value.Add(segmentNeighbor);
						// TODO "key already exists" when testing across multiple tiles
						remaining.Add(startCoincident, pair.Value);
						break;
					}
				}
			}

			private void DropCoincidentParts(
				[NotNull] NeighboredSegmentsSubpart featurePart,
				[NotNull] Dictionary<SegmentPartWithNeighbor, List<SegmentPartWithNeighbor>>
					coincidents,
				[NotNull] HashSet<FeaturePoint> dropParts,
				[NotNull] List<NeighboredSegmentsSubpart> nonCoincidentParts)
			{
				foreach (List<SegmentPartWithNeighbor> coincidentParts in coincidents.Values)
				{
					foreach (SegmentPartWithNeighbor coincidentPart in coincidentParts)
					{
						dropParts.Add(coincidentPart.CreateNeighborFeaturePoint());
					}
				}

				// TODO? : transfer SegmentParts of coincident neighbor (due to differing offsets)

				var dropNeighbors =
					new Dictionary<SegmentPartWithNeighbor, SegmentPartWithNeighbor>();
				foreach (List<SegmentPartWithNeighbor> coincidentParts in
				         coincidents.Values)
				{
					foreach (
						SegmentPartWithNeighbor coincidentPart in
						coincidentParts)
					{
						dropNeighbors.Add(coincidentPart, coincidentPart);
					}
				}

				foreach (
					SegmentParts segmentParts in
					featurePart.SegmentNeighbors.Values)
				{
					List<SegmentPart> remaining = new SegmentParts();
					foreach (SegmentPart segmentPart in segmentParts)
					{
						var part = (SegmentPartWithNeighbor) segmentPart;
						if (! dropNeighbors.ContainsKey(part))
						{
							remaining.Add(part);
						}
					}

					segmentParts.Clear();
					segmentParts.AddRange(remaining);
				}

				nonCoincidentParts.Add(featurePart);
			}

			private bool CheckKeepPart(
				[NotNull] NeighboredSegmentsSubpart featurePart,
				[NotNull] Dictionary<SegmentPartWithNeighbor, List<SegmentPartWithNeighbor>>
					coincidents, out int errorCount)
			{
				double featureAura = NearDistanceProvider.GetRowsDistance(
					                                         featurePart.BaseFeature,
					                                         featurePart.TableIndex)
				                                         .GetRowDistance();

				var keepPart = true;
				List<CoincidenceError> errorParts = null;
				foreach (SegmentPartWithNeighbor neighbor in coincidents.Keys)
				{
					double neighborAura =
						NearDistanceProvider.GetRowsDistance(
							                    neighbor.NeighborFeature,
							                    neighbor.NeighborTableIndex)
						                    .GetRowDistance();

					int d = Compare(featurePart, featureAura, neighbor,
					                neighborAura);
					if (d != 0)
					{
						keepPart = d < 0;
						if (keepPart && neighborAura > 0)
						{
							errorParts = errorParts ?? new List<CoincidenceError>();
							errorParts.Add(new CoincidenceError
							               {
								               DroppedPart = neighbor,
								               UsedPart = featurePart
							               });
						}
					}

					if (! keepPart)
					{
						break;
					}
				}

				if (keepPart && errorParts?.Count > 0)
				{
					errorCount = 0;
					Func<CoincidenceError, int> reportError = ReportCoincidenceError;
					if (reportError != null)
					{
						foreach (CoincidenceError errorPart in errorParts)
						{
							errorCount += reportError(errorPart);
						}
					}
				}
				else
				{
					errorCount = 0;
				}

				return keepPart;
			}

			private static void DropParts(
				[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>> splittedParts,
				[NotNull] HashSet<FeaturePoint> dropParts)
			{
				// TODO? : transfer SegmentParts of coincident neighbor (due to differing offsets), (Add needed Info to dropParts values)

				foreach (List<NeighboredSegmentsSubpart> featureParts in splittedParts.Values)
				{
					foreach (NeighboredSegmentsSubpart featurePart in featureParts)
					{
						foreach (KeyValuePair<SegmentPart, SegmentParts> pair
						         in featurePart.SegmentNeighbors)
						{
							SegmentPart segmentKey = pair.Key;
							SegmentParts neighboredParts = pair.Value;
							if (dropParts.Contains(featurePart.CreateFeaturePoint(segmentKey)))
							{
								neighboredParts.Clear();
								continue;
							}

							var remaining = new List<SegmentPart>(neighboredParts.Count);

							foreach (SegmentPart segmentPart in neighboredParts)
							{
								var part = (SegmentPartWithNeighbor) segmentPart;
								if (! dropParts.Contains(part.CreateNeighborFeaturePoint()))
								{
									remaining.Add(part);
								}
							}

							if (remaining.Count < neighboredParts.Count)
							{
								neighboredParts.Clear();
								neighboredParts.AddRange(remaining);
							}
						}
					}
				}
			}
		}
	}
}
