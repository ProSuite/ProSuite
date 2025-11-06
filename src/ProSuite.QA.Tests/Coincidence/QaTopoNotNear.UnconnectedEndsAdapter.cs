using System;
using System.Collections.Generic;
using System.Data;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Network;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Coincidence
{
	partial class QaTopoNotNear
	{
		public static bool HasInconsistentLineEnd(double lineWidth,
		                                          double segmentDirection0,
		                                          double segmentDirection1,
		                                          double segmentLength)
		{
			// invert second angle
			double betweenAngle = GetBetweenAngle(segmentDirection0,
			                                      segmentDirection1 + Math.PI);

			return HasInconsistentLineEnd(lineWidth, segmentLength, betweenAngle);
		}

		private static bool HasInconsistentLineEnd(double lineWidth,
		                                           double segmentLength,
		                                           double betweenSegmentAngle)
		{
			double segment0AngleToSegment1End = Math.Abs(betweenSegmentAngle) - Math.PI / 2;

			double ratio = Math.Abs(Math.Cos(segment0AngleToSegment1End));

			if (segment0AngleToSegment1End < 0 || segment0AngleToSegment1End > Math.PI)
			{
				// acute angle between segments
				return ratio * segmentLength < lineWidth;
			}

			return ratio * lineWidth > segmentLength;
		}

		private static double GetBetweenAngle(double firstAngle, double secondAngle)
		{
			double difference = firstAngle - secondAngle;
			while (difference < -Math.PI)
				difference += 2 * Math.PI;
			while (difference > Math.PI)
				difference -= 2 * Math.PI;
			return difference;
		}

		private class ShortSubpartError
		{
			public ShortSubpartError([NotNull] SegmentsSubpart segmentsSubpart, double length,
			                         double minLength)
			{
				SegmentsSubpart = segmentsSubpart;
				Length = length;
				MinLength = minLength;
			}

			[NotNull]
			public SegmentsSubpart SegmentsSubpart { get; }

			public double Length { get; }

			public double MinLength { get; }
		}

		private class AngleEndError
		{
			public AngleEndError([NotNull] SegmentsSubpart segmentsSubpart,
			                     [NotNull] IPnt at,
			                     [NotNull] IPnt otherEnd)
			{
				SegmentsSubpart = segmentsSubpart;
				At = at;
				OtherEnd = otherEnd;
			}

			[NotNull]
			public SegmentsSubpart SegmentsSubpart { get; }

			[NotNull]
			public IPnt At { get; }

			[NotNull]
			public IPnt OtherEnd { get; }

			//public double Angle { get; set; }
			//public double MaxAngle { get; set; }
		}

		private class LineEndsAdapter : SegmentAdapter
		{
			[CanBeNull]
			private NotReportedPairCondition NotReportedCondition { get; }

			[NotNull]
			private IFeatureDistanceProvider NearDistanceProvider { get; }

			public bool AdaptUnconnected { get; set; } = true;
			public string JunctionIsEndExpression { get; set; }

			public Func<ShortSubpartError, int> ReportShortSubpartError { get; set; }

			public Func<AngleEndError, int> ReportAngledEndError { get; set; }

			public LineEndsAdapter(
				[NotNull] IFeatureDistanceProvider nearDistanceProvider,
				bool is3D,
				[CanBeNull] NotReportedPairCondition notReportedCondition)
			{
				NotReportedCondition = notReportedCondition;
				NearDistanceProvider = nearDistanceProvider;
				Is3D = is3D;
			}

			public void AdaptLineEnds(
				[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>> splittedParts,
				[NotNull] ContinuationFinder continuationFinder, out int errorCount)
			{
				Dictionary<FeaturePoint, SegmentInfo> flatEnds =
					GetFlatEnds(splittedParts, continuationFinder, out errorCount);

				RecalcFlatEnds(flatEnds, splittedParts);

				Drop0LengthParts(EnumSegmentParts(splittedParts.Values));
			}

			private void RecalcFlatEnds(
				[NotNull] Dictionary<FeaturePoint, SegmentInfo> flatEnds,
				[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>> splittedParts)
			{
				foreach (KeyValuePair<FeaturePoint, SegmentInfo> pair in flatEnds)
				{
					SegmentInfo segmentInfo = pair.Value;
					foreach (SegmentPart segmentPart in segmentInfo.SegmentParts)
					{
						var segWithNb = (SegmentPartWithNeighbor) segmentPart;

						bool neighborIsFlatEnd;
						SegmentPair hulls = RecalcFlatEnd(pair.Key, segWithNb, flatEnds,
						                                  out neighborIsFlatEnd);

						if (! neighborIsFlatEnd)
						{
							SegmentPartWithNeighbor nbSegmentPart = FindNeighbor(
								splittedParts, pair.Key,
								segWithNb);
							if (nbSegmentPart != null)
							{
								RecalcNeighbor(nbSegmentPart, segWithNb, hulls);
							}
						}
					}
				}
			}

			[NotNull]
			private SegmentPair RecalcFlatEnd(
				[NotNull] FeaturePoint end,
				[NotNull] SegmentPartWithNeighbor segWithNb,
				[NotNull] Dictionary<FeaturePoint, SegmentInfo> flatEnds,
				out bool neighborIsFlatEnd)
			{
				IFeatureRowsDistance rowsDistance =
					NearDistanceProvider.GetRowsDistance(end.Feature, end.TableIndex);

				SegmentInfo segmentInfo;
				Find(end, segWithNb.SegmentIndex, flatEnds, out segmentInfo);

				SegmentCap startCap = segmentInfo.FlatStart
					                      ? (SegmentCap) new RectCap(0)
					                      : new RoundCap();

				SegmentCap endCap = segmentInfo.FlatEnd
					                    ? (SegmentCap) new RectCap(0)
					                    : new RoundCap();

				SegmentHull hull = CreateSegmentHull(
					(SegmentProxy) Assert.NotNull(segWithNb.SegmentProxy, "segmentproxy is null"),
					rowsDistance, startCap, endCap);

				var neighborKey = new FeaturePoint(segWithNb.NeighborFeature,
				                                   segWithNb.NeighborTableIndex,
				                                   segWithNb.PartIndex, 0);

				neighborIsFlatEnd = false;

				SegmentInfo neighborInfo;

				SegmentCap nbStartCap;
				SegmentCap nbEndCap;

				if (Find(neighborKey, segWithNb.NeighborProxy.SegmentIndex, flatEnds,
				         out neighborInfo))
				{
					neighborIsFlatEnd = true;

					nbStartCap = neighborInfo.FlatStart
						             ? (SegmentCap) new RectCap(0)
						             : new RoundCap();
					nbEndCap = neighborInfo.FlatEnd
						           ? (SegmentCap) new RectCap(0)
						           : new RoundCap();
				}
				else
				{
					nbStartCap = new RoundCap();
					nbEndCap = new RoundCap();
				}

				SegmentHull neighborhull = CreateNeighborSegmentHull(
					(SegmentProxy) segWithNb.NeighborProxy, rowsDistance, segWithNb.NeighborFeature,
					segWithNb.NeighborTableIndex, nbStartCap, nbEndCap);

				SegmentPair segPair = RecalcPart(segWithNb, hull, neighborhull);

				return segPair;
			}

			[CanBeNull]
			private SegmentPartWithNeighbor FindNeighbor(
				[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>> splittedParts,
				[NotNull] FeaturePoint segmentFeature,
				[NotNull] SegmentPartWithNeighbor segWithNb)
			{
				var neighborKey = new FeaturePoint(segWithNb.NeighborFeature,
				                                   segWithNb.NeighborTableIndex,
				                                   segWithNb.PartIndex, 0);

				List<NeighboredSegmentsSubpart> subparts;
				if (! splittedParts.TryGetValue(neighborKey, out subparts))
				{
					return null;
				}

				foreach (NeighboredSegmentsSubpart subpart in subparts)
				{
					if (subpart.FullMinFraction > segWithNb.NeighborProxy.SegmentIndex ||
					    subpart.FullMaxFraction < segWithNb.NeighborProxy.SegmentIndex)
					{
						continue;
					}

					foreach (SegmentParts segmentParts in subpart.SegmentNeighbors.Values)
					{
						foreach (SegmentPart segmentPart in segmentParts)
						{
							if (segmentPart.SegmentIndex != segWithNb.NeighborProxy.SegmentIndex)
							{
								continue;
							}

							var partWithNeighbor = (SegmentPartWithNeighbor) segmentPart;
							if (partWithNeighbor.NeighborFeature == segmentFeature.Feature &&
							    partWithNeighbor.NeighborTableIndex == segmentFeature.TableIndex &&
							    partWithNeighbor.NeighborProxy.PartIndex == segWithNb.PartIndex &&
							    partWithNeighbor.NeighborProxy.SegmentIndex ==
							    segWithNb.SegmentIndex)
							{
								return partWithNeighbor;
							}
						}
					}
				}

				return null;
			}

			private void RecalcNeighbor([NotNull] SegmentPartWithNeighbor neighbor,
			                            [NotNull] SegmentPartWithNeighbor part,
			                            [NotNull] SegmentPair hulls)
			{
				IFeatureRowsDistance rowsDistance =
					NearDistanceProvider.GetRowsDistance(part.NeighborFeature,
					                                     part.NeighborTableIndex);

				SegmentHull neighborhull = CreateSegmentHull(
					(SegmentProxy) part.NeighborProxy, rowsDistance,
					hulls.Neighbor.StartCap, hulls.Neighbor.EndCap);

				SegmentHull hull = CreateNeighborSegmentHull(
					(SegmentProxy) Assert.NotNull(part.SegmentProxy, "segmentproxy is null"),
					rowsDistance, neighbor.NeighborFeature, // == part.Feature
					neighbor.NeighborTableIndex, // == part.TableIndex
					hulls.Hull.StartCap, hulls.Hull.EndCap);

				RecalcPart(neighbor, neighborhull, hull);
			}

			private class SegmentInfo
			{
				public SegmentInfo([NotNull] SegmentParts segmentParts)
				{
					SegmentParts = segmentParts;
				}

				[NotNull]
				public SegmentParts SegmentParts { get; }

				public bool FlatStart { get; set; }

				public bool FlatEnd { get; set; }
			}

			[NotNull]
			private Dictionary<FeaturePoint, SegmentInfo> GetFlatEnds(
				[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>> splittedParts,
				[NotNull] ContinuationFinder continuationFinder, out int errorCount)
			{
				errorCount = 0;
				var flatEnds =
					new Dictionary<FeaturePoint, SegmentInfo>(new FeaturePointComparer());
				foreach (
					KeyValuePair<FeaturePoint, List<NeighboredSegmentsSubpart>> splitted in
					splittedParts)
				{
					foreach (NeighboredSegmentsSubpart segmentsSubpart in splitted.Value)
					{
						foreach (
							KeyValuePair<SegmentPart, SegmentParts> neighboredSegmentPart in
							segmentsSubpart.SegmentNeighbors)
						{
							SegmentPart key = neighboredSegmentPart.Key;
							if (key.SegmentIndex > 0 &&
							    key.SegmentIndex < segmentsSubpart.FullMaxFraction - 1)
							{
								continue;
							}

							var flatStart = false;
							var flatEnd = false;
							foreach (SegmentPart segmentPart in neighboredSegmentPart.Value)
							{
								if (! flatStart && segmentPart.FullMin <= 0)
								{
									flatStart = HandleAsFlat(segmentsSubpart, segmentPart.FullMin,
									                         continuationFinder);
								}

								if (! flatEnd && segmentPart.FullMax >=
								    segmentsSubpart.FullMaxFraction)
								{
									flatEnd = HandleAsFlat(segmentsSubpart, segmentPart.FullMax,
									                       continuationFinder);
								}
							}

							if (flatStart || flatEnd)
							{
								IFeatureRowsDistance rowsDistance =
									NearDistanceProvider.GetRowsDistance(splitted.Key.Feature,
										splitted.Key.TableIndex);
								double rowDistance = rowsDistance.GetRowDistance();

								if (flatStart)
								{
									errorCount += CreateFlatEnds(
										segmentsSubpart, rowDistance, flatEnds, continuationFinder,
										atStart: true);
								}

								if (flatEnd)
								{
									errorCount += CreateFlatEnds(
										segmentsSubpart, rowDistance, flatEnds, continuationFinder,
										atStart: false);
								}

								if (flatStart && flatEnd)
								{
									double sumLength = 0;
									double limit = 2 * rowDistance;
									foreach (SegmentProxy segmentProxy in segmentsSubpart
										         .GetSegments())
									{
										sumLength += segmentProxy.Length;
										if (sumLength > limit)
										{
											break;
										}
									}

									if (sumLength < limit && ReportShortSubpartError != null)
									{
										errorCount += ReportShortSubpartError(
											new ShortSubpartError(
												segmentsSubpart, sumLength, limit));
									}
								}
							}
						}
					}
				}

				return flatEnds;
			}

			private class SegmentProxyInfo
			{
				public SegmentProxyInfo([NotNull] SegmentProxy segmentProxy,
				                        [NotNull] NeighboredSegmentsSubpart subpart)
				{
					SegmentProxy = segmentProxy;
					Subpart = subpart;
				}

				[NotNull]
				public SegmentProxy SegmentProxy { get; }

				[NotNull]
				public NeighboredSegmentsSubpart Subpart { get; }
			}

			private int CreateFlatEnds([NotNull] NeighboredSegmentsSubpart segmentsSubpartx,
			                           double rowDistance,
			                           [NotNull] Dictionary<FeaturePoint, SegmentInfo> flatEnds,
			                           [NotNull] ContinuationFinder continuationFinder,
			                           bool atStart)
			{
				var errorCount = 0;
				double sumLength = 0;
				SegmentProxyInfo first = null;
				foreach (SegmentProxyInfo info in
				         GetSegmentProxies(segmentsSubpartx, continuationFinder, atStart))
				{
					first = first ?? info;

					var segmentPartKey = new SegmentPart(info.SegmentProxy, 0, 1, complete: true);
					NeighboredSegmentsSubpart segmentsSubpart = info.Subpart;

					SegmentParts neighboredParts;
					segmentsSubpart.SegmentNeighbors.TryGetValue(segmentPartKey,
					                                             out neighboredParts);

					var featurePointKey =
						new FeaturePoint(
							segmentsSubpart.BaseFeature, segmentsSubpart.TableIndex,
							segmentPartKey.PartIndex, segmentPartKey.SegmentIndex);

					SegmentInfo segmentInfo;
					if (! flatEnds.TryGetValue(featurePointKey, out segmentInfo))
					{
						// TODO revise: neighboredParts can be null here, but SegmentInfo property is later expected to be NotNull
						segmentInfo = new SegmentInfo(neighboredParts);
						flatEnds.Add(featurePointKey, segmentInfo);
					}

					if (atStart)
					{
						segmentInfo.FlatStart = true;
					}
					else
					{
						segmentInfo.FlatEnd = true;
					}

					if (info != first)
					{
						errorCount += VerifyAngle(first, info, rowDistance, segmentsSubpart);
					}

					sumLength += info.SegmentProxy.Length;
					if (sumLength >= rowDistance)
					{
						break;
					}
				}

				return errorCount;
			}

			private int VerifyAngle([NotNull] SegmentProxyInfo first,
			                        [NotNull] SegmentProxyInfo compare,
			                        double lineWidth,
			                        [NotNull] SegmentsSubpart subpart)
			{
				if (ReportAngledEndError == null)
				{
					return 0;
				}

				double dir0;
				IPnt pnt0;

				double dir1;
				IPnt pnt1;

				if (first.SegmentProxy.SegmentIndex < compare.SegmentProxy.SegmentIndex)
				{
					dir0 = first.SegmentProxy.GetDirectionAt(0);
					pnt0 = first.SegmentProxy.GetPointAt(0);

					dir1 = compare.SegmentProxy.GetDirectionAt(0);
					pnt1 = compare.SegmentProxy.GetPointAt(0);
				}
				else
				{
					dir0 = first.SegmentProxy.GetDirectionAt(1);
					pnt0 = first.SegmentProxy.GetPointAt(1);

					dir1 = compare.SegmentProxy.GetDirectionAt(1);
					pnt1 = compare.SegmentProxy.GetPointAt(1);
				}

				// TODO revise length/angle calculation in case of intermittent segments
				double dx = pnt0.X - pnt1.X;
				double dy = pnt0.Y - pnt1.Y;
				double segmentLength = Math.Sqrt(dx * dx + dy * dy);

				return HasInconsistentLineEnd(lineWidth, dir0, dir1, segmentLength)
					       ? ReportAngledEndError(new AngleEndError(subpart, pnt1, pnt0))
					       : 0;
			}

			private IEnumerable<SegmentProxyInfo> GetSegmentProxies(
				[NotNull] NeighboredSegmentsSubpart segmentsSubpart,
				[NotNull] ContinuationFinder continuationFinder,
				bool atStart)
			{
				FeaturePoint continuationKey;
				if (atStart)
				{
					for (int iSegment = segmentsSubpart.FullMinFraction;
					     iSegment < segmentsSubpart.FullMaxFraction;
					     iSegment++)
					{
						yield return GetSegmentproxyInfo(segmentsSubpart, iSegment);
					}

					continuationKey = new FeaturePoint(segmentsSubpart.BaseFeature,
					                                   segmentsSubpart.TableIndex,
					                                   segmentsSubpart.PartIndex,
					                                   segmentsSubpart.FullMaxFraction);
				}
				else
				{
					for (int iSegment = segmentsSubpart.FullMaxFraction - 1;
					     iSegment >= segmentsSubpart.FullMinFraction;
					     iSegment--)
					{
						yield return GetSegmentproxyInfo(segmentsSubpart, iSegment);
					}

					continuationKey = new FeaturePoint(segmentsSubpart.BaseFeature,
					                                   segmentsSubpart.TableIndex,
					                                   segmentsSubpart.PartIndex,
					                                   segmentsSubpart.FullMinFraction);
				}

				List<NeighboredSegmentsSubpart> continuations =
					continuationFinder.GetContinuations(continuationKey,
					                                    new List<SegmentsSubpart>(), false);

				if (continuations == null)
				{
					yield break;
				}

				foreach (NeighboredSegmentsSubpart continuation in continuations)
				{
					if (continuation.BaseFeature.OID != segmentsSubpart.BaseFeature.OID ||
					    continuation.TableIndex != segmentsSubpart.TableIndex ||
					    continuation.PartIndex != segmentsSubpart.PartIndex)
					{
						continue;
					}

					if (atStart &&
					    continuation.FullMinFraction != segmentsSubpart.FullMaxFraction)
					{
						continue;
					}

					if (! atStart &&
					    continuation.FullMaxFraction != segmentsSubpart.FullMinFraction)
					{
						continue;
					}

					foreach (
						SegmentProxyInfo segmentProxy in
						GetSegmentProxies(continuation, continuationFinder, atStart))
					{
						yield return segmentProxy;
					}
				}
			}

			[NotNull]
			private static SegmentProxyInfo GetSegmentproxyInfo(
				[NotNull] NeighboredSegmentsSubpart segmentsSubpart, int baseSegmentIndex)
			{
				int subpartIndex =
					segmentsSubpart.FullStartFraction < segmentsSubpart.FullEndFraction
						? baseSegmentIndex - segmentsSubpart.FullStartFraction
						: segmentsSubpart.FullStartFraction - baseSegmentIndex - 1;

				SegmentProxy segmentProxy = segmentsSubpart.GetSegment(subpartIndex);

				var key = new SegmentPart(segmentProxy, 0, 1, true);
				// SegmentPart key = new SegmentPart(segmentsSubpart.PartIndex, iSegment, 0, 1, true);
				segmentsSubpart.SegmentNeighbors.TryGetValue(key, out SegmentParts _);

				return new SegmentProxyInfo(segmentProxy, segmentsSubpart);
			}

			private bool HandleAsFlat(NeighboredSegmentsSubpart segmentPart,
			                          double fullFraction,
			                          ContinuationFinder continuationFinder)
			{
				var p = new FeaturePoint(segmentPart.BaseFeature, segmentPart.TableIndex,
				                         segmentPart.PartIndex, fullFraction);

				List<NeighboredSegmentsSubpart> continuations =
					continuationFinder.GetContinuations(p, new List<SegmentsSubpart>());
				if (continuations == null)
				{
					return AdaptUnconnected;
				}

				var junctionSegments = new List<NeighboredSegmentsSubpart>();
				foreach (NeighboredSegmentsSubpart continuation in continuations)
				{
					if (p.Feature == continuation.BaseFeature &&
					    p.TableIndex == continuation.TableIndex &&
					    p.Part == continuation.PartIndex &&
					    Math.Abs(p.FullFraction - continuation.FullStartFraction) < 0.01)
					{
						junctionSegments.Add(continuation);
						continue;
					}

					if (NotReportedCondition != null &&
					    NotReportedCondition.IsFulfilled(segmentPart.BaseFeature,
					                                     segmentPart.TableIndex,
					                                     continuation.BaseFeature,
					                                     continuation.TableIndex))
					{
						continue;
					}

					if (string.IsNullOrEmpty(JunctionIsEndExpression))
					{
						return false;
					}

					junctionSegments.Add(continuation);
				}

				if (junctionSegments.Count > 0)
				{
					List<string> tableRules = string.IsNullOrWhiteSpace(JunctionIsEndExpression)
						                          ? new List<string> { "false" }
						                          : new List<string> { JunctionIsEndExpression };

					ITableSchemaDef table = (ITableSchemaDef) junctionSegments[0].BaseFeature.Table;

					var rule = new QaConnectionRule(new[] { table }, tableRules);

					IList<QaConnectionRuleHelper> helpers = QaConnectionRuleHelper.CreateList(
						new[] { rule },
						out TableView[] tableFilterHelpers);

					foreach (NeighboredSegmentsSubpart junctionSegment in junctionSegments)
					{
						int tableIndex = junctionSegment.TableIndex;

						TableView baseHelper = tableFilterHelpers[tableIndex];

						DataRow helperRow = baseHelper.Add(junctionSegment.BaseFeature);
						Assert.NotNull(helperRow, "no row returned");

						{
							helperRow[QaConnections.StartsIn] = segmentPart.FullStartFraction <
							                                    segmentPart.FullEndFraction;
						}
					}

					foreach (QaConnectionRuleHelper ruleHelper in helpers)
					{
						// check if all rows comply to the current rule 
						int connectedElementsCount = junctionSegments.Count;
						var matchingRowsCount = 0;
						for (var tableIndex = 0; tableIndex < 1; tableIndex++)
						{
							matchingRowsCount += ruleHelper
							                     .MainRuleFilterHelpers[tableIndex]
							                     .FilteredRowCount;
						}

						Assert.True(matchingRowsCount <= connectedElementsCount,
						            "Unexpected matching rows count: {0}; total connected rows: {1}",
						            matchingRowsCount, connectedElementsCount);

						if (matchingRowsCount == connectedElementsCount &&
						    ruleHelper.VerifyCountRules())
						{
							// all rows comply to the current rule,
							// so one rule if fulfilled and no further checking needed
							return true;
						}
					}
				}

				return AdaptUnconnected;
			}

			private bool Find([NotNull] FeaturePoint key, double fraction,
			                  [NotNull] Dictionary<FeaturePoint, SegmentInfo> flatEnds,
			                  out SegmentInfo segmentInfo)
			{
				return flatEnds.TryGetValue(
					new FeaturePoint(key.Feature, key.TableIndex, key.Part, fraction),
					out segmentInfo);
			}
		}

		private class AssymetricNearAdapter : SegmentAdapter
		{
			private readonly SideDistanceProvider _nearProvider;

			public AssymetricNearAdapter([NotNull] SideDistanceProvider nearProvider, bool is3D)
			{
				_nearProvider = nearProvider;
				Is3D = is3D;
			}

			public void AdaptAssymetry(
				Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>> splittedParts)
			{
				foreach (KeyValuePair<FeaturePoint, List<NeighboredSegmentsSubpart>> pair in
				         splittedParts)
				{
					var cap = new RoundCap();
					FeaturePoint p = pair.Key;
					SideRowsDistance rowsDistance =
						_nearProvider.GetRowsDistance(p.Feature, p.TableIndex);
					List<NeighboredSegmentsSubpart> splitted = pair.Value;
					foreach (NeighboredSegmentsSubpart part in splitted)
					{
						foreach (SegmentParts segmentParts in part.SegmentNeighbors.Values)
						{
							foreach (SegmentPart segmentPart in segmentParts)
							{
								SegmentHull hull = CreateSegmentHull(
									(SegmentProxy) Assert.NotNull(
										segmentPart.SegmentProxy, "segmentproxy is null"),
									rowsDistance, cap, cap);

								var segNbPart = (SegmentPartWithNeighbor) segmentPart;
								AdaptAssymetry(rowsDistance, hull, segNbPart, cap);
							}
						}
					}
				}

				Drop0LengthParts(EnumSegmentParts(splittedParts.Values));
			}

			private void AdaptAssymetry([NotNull] SideRowsDistance rowsDistance,
			                            [NotNull] SegmentHull hull,
			                            [NotNull] SegmentPartWithNeighbor part,
			                            [NotNull] RoundCap cap)
			{
				SegmentHull nbHull = CreateNeighborSegmentHull(
					(SegmentProxy) part.NeighborProxy, rowsDistance,
					part.NeighborFeature,
					part.NeighborTableIndex, cap, cap);
				if (hull.LeftOffset == nbHull.RightOffset
				    && nbHull.LeftOffset == nbHull.RightOffset)
				{
					return;
				}

				RecalcPart(part, hull, nbHull);
			}
		}
	}
}
