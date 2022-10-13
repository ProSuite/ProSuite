using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Tests.Coincidence
{
	partial class QaTopoNotNear
	{
		private class ErrorCleanup
		{
			private IList<SegmentRelation> SegmentRelationsToCheck { get; }
			[CanBeNull] private readonly AllNotReportedPairConditions _notReportedCondition;

			public ErrorCleanup([NotNull] IList<SegmentRelation> segmentRelationsToCheck,
			                    [CanBeNull] AllNotReportedPairConditions notReportedCondition)
			{
				SegmentRelationsToCheck = segmentRelationsToCheck;
				_notReportedCondition = notReportedCondition;
			}

			public IEnumerable<ConnectedLinesEx> CleanupNotReportedPairs(
				ConnectedLinesEx errorCandidate)
			{
				if (_notReportedCondition == null)
				{
					yield return errorCandidate;
					yield break;
				}

				AllNotReportedPairConditions notReportedCondition = _notReportedCondition;

				ConnectedLines subConnected = null;
				SegmentNeighbors subNeighbors = null;
				SegmentPairRelation subRelevantSegment = null;
				int minRelationIndex = SegmentRelationsToCheck.Count;
				foreach (
					ConnectedSegmentsSubpart allParts in errorCandidate.Line.BaseSegments)
				{
					IEnumerable<SegmentPartWithNeighbor> neighbors;
					double limit;
					Func<SegmentPartWithNeighbor, double, bool> checkLimit;
					Func<SegmentPartWithNeighbor, double, double> getLimit;
					Func<double, bool> checkEndLimit;
					if (allParts.FullStartFraction > allParts.FullEndFraction)
					{
						var sorted =
							new List<SegmentPartWithNeighbor>(GetNeighbors(allParts));
						sorted.Sort((x, y) => -x.FullMax.CompareTo(y.FullMax));
						neighbors = sorted;

						limit = allParts.FullMaxFraction;
						checkLimit = (x, l) => x.FullMax < l;
						getLimit = (x, l) => Math.Min(l, x.FullMin);
						checkEndLimit = l => l > allParts.FullMinFraction;
					}
					else
					{
						neighbors = GetNeighbors(allParts);
						limit = allParts.FullMinFraction;
						checkLimit = (x, l) => x.FullMin > l;
						getLimit = (x, l) => Math.Max(l, x.FullMax);
						checkEndLimit = l => l < allParts.FullMaxFraction;
					}

					ConnectedSegmentsSubpart subSubparts = null;
					foreach (SegmentPartWithNeighbor segmentPart in neighbors)
					{
						if (notReportedCondition.IsFulfilled(
							    allParts.BaseFeature, allParts.TableIndex,
							    segmentPart.NeighborFeature,
							    segmentPart.NeighborTableIndex))
						{
							continue;
						}

						if (checkLimit(segmentPart, limit) && subConnected != null)
						{
							subConnected.RelevantSegment =
								errorCandidate.Line.RelevantSegment;
							yield return GetClean(subConnected, errorCandidate);
							subConnected = null;
						}

						if (subConnected == null)
						{
							subConnected =
								new ConnectedLines(new List<ConnectedSegmentsSubpart>());
							subRelevantSegment = null;
							subSubparts = null;
						}

						if (subRelevantSegment == null ||
						    segmentPart.MinRelationIndex <
						    subRelevantSegment.Segment.MinRelationIndex)
						{
							subRelevantSegment = new SegmentPairRelation(
								segmentPart,
								SegmentRelationsToCheck[segmentPart.MinRelationIndex]);
						}

						if (subSubparts == null)
						{
							subNeighbors = new SegmentNeighbors(new SegmentPartComparer());

							var subCurve = new SubClosedCurve(allParts.ConnectedCurve.BaseGeometry,
							                                  allParts.ConnectedCurve.PartIndex,
							                                  segmentPart.FullMin,
							                                  segmentPart.FullMax);

							subSubparts = new ConnectedSegmentsSubpart(
								allParts, subNeighbors, subCurve);

							subConnected.BaseSegments.Add(subSubparts);
						}

						SegmentParts parts;
						var key = new SegmentPart(
							Assert.NotNull(segmentPart.SegmentProxy), 0, 1, true);
						if (! subNeighbors.TryGetValue(key, out parts))
						{
							parts = new SegmentParts();
							subNeighbors.Add(key, parts);
						}

						parts.Add(segmentPart);

						int relationIndex = segmentPart.MinRelationIndex;
						if (relationIndex < minRelationIndex)
						{
							minRelationIndex = relationIndex;
						}

						limit = getLimit(segmentPart, limit);
					}

					if (checkEndLimit(limit) && subConnected != null)
					{
						subConnected.RelevantSegment = errorCandidate.Line.RelevantSegment;
						yield return GetClean(subConnected, errorCandidate);
						subConnected = null;
					}
				}

				if (subConnected != null)
				{
					subConnected.RelevantSegment = errorCandidate.Line.RelevantSegment;
					yield return GetClean(subConnected, errorCandidate);
				}
			}

			private ConnectedLinesEx GetClean(ConnectedLines line, ConnectedLinesEx orig)
			{
				line.RecalculateConnectedCurves();
				var clean = new ConnectedLinesEx(line, orig.MinLength, orig.Length);
				clean.WithinNear = orig.WithinNear;
				return clean;
			}

			private IEnumerable<SegmentPartWithNeighbor> GetNeighbors(
				NeighboredSegmentsSubpart subparts)
			{
				foreach (
					KeyValuePair<SegmentPart, SegmentParts> segmentNeighbor in
					subparts.SegmentNeighbors)
				{
					foreach (SegmentPart neighbor in segmentNeighbor.Value)
					{
						var segmentPart = (SegmentPartWithNeighbor) neighbor;
						yield return segmentPart;
					}
				}
			}
		}
	}
}
