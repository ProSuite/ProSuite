using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Tests.Coincidence
{
	partial class QaTopoNotNear
	{
		private class SubpartComparer : IEqualityComparer<SegmentsSubpart>
		{
			public bool Equals(SegmentsSubpart x, SegmentsSubpart y)
			{
				return x.BaseFeature.OID == y.BaseFeature.OID
				       && x.BaseFeature.Table == y.BaseFeature.Table
				       && x.PartIndex == y.PartIndex
				       && x.FullMinFraction == y.FullMinFraction
				       && x.FullMaxFraction == y.FullMaxFraction;
			}

			public int GetHashCode(SegmentsSubpart obj)
			{
				return obj.BaseFeature.OID.GetHashCode() ^ 37 * obj.PartIndex.GetHashCode() ^
				       29 * obj.FullMinFraction.GetHashCode();
			}
		}

		private class SegmentsSubpart
		{
			public SegmentsSubpart([NotNull] IReadOnlyFeature baseFeature,
			                       int tableIndex,
			                       [NotNull] IIndexedSegments baseSegments,
			                       int partIndex,
			                       int fullStart, int fullEnd)
			{
				BaseFeature = baseFeature;
				TableIndex = tableIndex;
				BaseSegments = baseSegments;
				PartIndex = partIndex;
				FullStartFraction = fullStart;
				FullEndFraction = fullEnd;

				FullMinFraction = Math.Min(FullStartFraction, FullEndFraction);
				FullMaxFraction = Math.Max(FullStartFraction, FullEndFraction);
			}

			public override string ToString()
			{
				string txt = string.Format("SegmentsSubpart: OID:{0}, P:{1}, {2:N1}->{3:N1}",
				                           BaseFeature.OID, PartIndex, FullStartFraction,
				                           FullEndFraction);
				return txt;
			}

			[NotNull]
			public IReadOnlyFeature BaseFeature { get; }

			public int TableIndex { get; }

			[NotNull]
			public IIndexedSegments BaseSegments { get; }

			public int PartIndex { get; }

			public int FullMinFraction { get; }

			public int FullMaxFraction { get; }

			public int FullStartFraction { get; }

			public int FullEndFraction { get; }

			[NotNull]
			public IEnumerable<SegmentProxy> GetSegments()
			{
				if (FullStartFraction < FullEndFraction)
				{
					for (int iSegment = FullStartFraction; iSegment < FullEndFraction; iSegment++)
					{
						yield return BaseSegments.GetSegment(PartIndex, iSegment);
					}
				}
				else
				{
					for (int iSegment = FullStartFraction - 1;
					     iSegment >= FullEndFraction;
					     iSegment--)
					{
						yield return BaseSegments.GetSegment(PartIndex, iSegment);
					}
				}
			}

			public IEnumerable<SegmentProxy> GetSegments([NotNull] IBox box)
			{
				foreach (SegmentProxy segment in BaseSegments.GetSegments(box))
				{
					if (segment.PartIndex != PartIndex)
					{
						continue;
					}

					if (segment.SegmentIndex >= FullMaxFraction ||
					    segment.SegmentIndex < FullMinFraction)
					{
						continue;
					}

					yield return segment;
				}
			}

			public int GetSegmentCount()
			{
				return FullMaxFraction - FullMinFraction;
			}

			public bool IsClosed()
			{
				if (FullMinFraction > 0 ||
				    FullMaxFraction < BaseSegments.GetPartSegmentCount(PartIndex))
				{
					return false;
				}

				return BaseSegments.IsPartClosed(PartIndex);
			}

			[NotNull]
			public SegmentProxy GetSegment(int segmentIndex)
			{
				int baseIndex = FullStartFraction < FullEndFraction
					                ? FullStartFraction + segmentIndex
					                : FullStartFraction - segmentIndex - 1;
				return BaseSegments.GetSegment(PartIndex, baseIndex);
			}

			[NotNull]
			public FeaturePoint GetSegmentAsFeaturePoint(int segmentIndex)
			{
				SegmentProxy segment = GetSegment(segmentIndex);
				return new FeaturePoint(BaseFeature, TableIndex, PartIndex,
				                        segment.SegmentIndex);
			}

			public int GetSegmentIndex([NotNull] ISegmentProxy segment)
			{
				int baseIndex = FullStartFraction < FullEndFraction
					                ? segment.SegmentIndex - FullStartFraction
					                : FullStartFraction - segment.SegmentIndex - 1;
				return baseIndex;
			}

			[NotNull]
			public IPolyline GetSubpart(int startSegment,
			                            double startFraction,
			                            int endSegment,
			                            double endFraction)
			{
				IPolyline subPart = BaseSegments.GetSubpart(PartIndex,
				                                            FullMinFraction + startSegment,
				                                            startFraction,
				                                            FullMinFraction + endSegment,
				                                            endFraction);
				if (FullStartFraction > FullEndFraction)
				{
					subPart.ReverseOrientation();
				}

				return subPart;
			}

			public FeaturePoint CreateFeaturePoint(SegmentPart segmentKey)
			{
				return new FeaturePoint(BaseFeature, TableIndex, segmentKey.PartIndex,
				                        segmentKey.SegmentIndex);
			}
		}

		private class NeighboredSegmentsSubpart : SegmentsSubpart
		{
			public NeighboredSegmentsSubpart([NotNull] SegmentsSubpart part,
			                                 [NotNull] SegmentNeighbors neighbors)
				: base(part.BaseFeature, part.TableIndex, part.BaseSegments,
				       part.PartIndex, part.FullStartFraction, part.FullEndFraction)
			{
				SegmentNeighbors = neighbors;
			}

			[NotNull]
			public SegmentNeighbors SegmentNeighbors { get; }

			[CanBeNull]
			public SubClosedCurve GetSubcurveAt(double value)
			{
				IList<SubClosedCurve> connected;
				GetSubcurves(BaseSegments, SegmentNeighbors.Values, 0, 0, false, out connected,
				             out IList<SubClosedCurve> _);

				foreach (SubClosedCurve candidate in connected)
				{
					if (candidate.StartFullIndex <= value && candidate.EndFullIndex >= value)
					{
						return candidate;
					}
				}

				return null;
			}
		}

		private class ConnectedSegmentsSubpart : NeighboredSegmentsSubpart
		{
			public ConnectedSegmentsSubpart([NotNull] SegmentsSubpart part,
			                                [NotNull] SegmentNeighbors neighbors,
			                                [NotNull] SubClosedCurve connected) :
				base(part, neighbors)
			{
				ConnectedCurve = connected;
			}

			[NotNull]
			public SubClosedCurve ConnectedCurve { get; }

			public ConnectedSegmentsSubpart Reverse()
			{
				return new ConnectedSegmentsSubpart(
					new SegmentsSubpart(BaseFeature, TableIndex, BaseSegments, PartIndex,
					                    FullEndFraction, FullStartFraction),
					SegmentNeighbors, ConnectedCurve);
			}
		}

		private class ConnectedLinesSegment : SegmentProxy
		{
			private readonly SegmentProxy _baseProxy;

			public ConnectedLinesSegment(int partIndex, int segmentIndex,
			                             [NotNull] SegmentProxy baseProxy)
				: base(partIndex, segmentIndex)
			{
				_baseProxy = baseProxy;
			}

			public override ISpatialReference SpatialReference => _baseProxy.SpatialReference;

			public override double Length => _baseProxy.Length;

			public override SegmentProxy GetSubCurve(double fromRatio, double toRatio)
			{
				return _baseProxy.GetSubCurve(fromRatio, toRatio);
			}

			public override void QueryOffset(Pnt point, out double offset, out double along)
			{
				_baseProxy.QueryOffset(point, out offset, out along);
			}

			public override WKSEnvelope GetSubCurveBox(double fromRatio, double toRatio)
			{
				return _baseProxy.GetSubCurveBox(fromRatio, toRatio);
			}

			public override IPnt Min => _baseProxy.Min;

			public override IPnt Max => _baseProxy.Max;

			public override bool IsLinear => _baseProxy.IsLinear;

			public override Pnt GetStart(bool as3D)
			{
				return _baseProxy.GetStart(as3D);
			}

			public override Pnt GetEnd(bool as3D)
			{
				return _baseProxy.GetEnd(as3D);
			}

			public override IPnt GetPointAt(double fraction)
			{
				return _baseProxy.GetPointAt(fraction);
			}

			public override IPnt GetPointAt(double fraction, bool as3D)
			{
				return _baseProxy.GetPointAt(fraction, as3D);
			}

			public override IPolyline GetPolyline(bool forceCreation)
			{
				return _baseProxy.GetPolyline(forceCreation);
			}

			public override double GetDirectionAt(double fraction)
			{
				return _baseProxy.GetDirectionAt(fraction);
			}
		}

		private class DirectedMinMaxConnected
		{
			[NotNull]
			public ConnectedSegmentParts Parts { get; }

			public double Min { get; }

			public double Max { get; }

			public DirectedMinMaxConnected([NotNull] ConnectedSegmentParts parts,
			                               double min, double max)
			{
				Parts = parts;
				Min = min;
				Max = max;
			}

			public override string ToString()
			{
				return $"{Parts} [{Min:N1},{Max:N1}]";
			}
		}

		private class ConnectedPartsNeighbor
		{
			private readonly NeighboredSegmentsSubpart _subpart;
			private readonly int _subpartIndex;
			private readonly ContinuationFinder _continuationFinder;
			private readonly IList<NeighboredSegmentsSubpart> _preNeighbors;

			public ConnectedPartsNeighbor(
				[NotNull] NeighboredSegmentsSubpart subpart,
				int subpartIndex,
				[NotNull] IList<NeighboredSegmentsSubpart> preNeighbors,
				[NotNull] ContinuationFinder continuationFinder)
			{
				_subpart = subpart;
				_subpartIndex = subpartIndex;
				_preNeighbors = preNeighbors;
				_continuationFinder = continuationFinder;
			}

			public int TableIndex => _subpart.TableIndex;

			[NotNull]
			public IReadOnlyFeature BaseFeature => _subpart.BaseFeature;

			[NotNull]
			public SegmentProxy Segment => _subpart.GetSegment(_subpartIndex);

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.Append($"S:{Segment.SegmentIndex} at ");
				foreach (NeighboredSegmentsSubpart subpart in _preNeighbors)
				{
					sb.Append($"OID:{subpart.BaseFeature.OID}, P:{subpart.PartIndex}, ");
					sb.Append($"{subpart.FullStartFraction}->{subpart.FullEndFraction};");
				}

				return sb.ToString();
			}

			public void CompleteCircle(IList<ConnectedSegmentsSubpart> circleToComplete)
			{
				// create dictionary to filter neighbored segments
				List<SegmentsSubpart> circleSegmentRanges =
					circleToComplete.Cast<SegmentsSubpart>()
					                .ToList();

				// ReSharper disable once RedundantEnumerableCastCall
				circleSegmentRanges.AddRange(_preNeighbors.Cast<SegmentsSubpart>());

				var circlePoints =
					new Dictionary<FeaturePoint, List<SegmentsSubpart>>(
						new FeaturePointComparer());

				foreach (SegmentsSubpart circlePart in circleSegmentRanges)
				{
					var p = new FeaturePoint(circlePart.BaseFeature, circlePart.TableIndex,
					                         circlePart.PartIndex, 0);
					List<SegmentsSubpart> ranges;
					if (! circlePoints.TryGetValue(p, out ranges))
					{
						ranges = new List<SegmentsSubpart>();
						circlePoints.Add(p, ranges);
					}

					ranges.Add(circlePart);
				}

				for (int i = _preNeighbors.Count - 1; i >= 0; i--)
				{
					// create missing circle parts
					NeighboredSegmentsSubpart preNeighbor = _preNeighbors[i];
					var prePart = new SegmentsSubpart(preNeighbor.BaseFeature,
					                                  preNeighbor.TableIndex,
					                                  preNeighbor.BaseSegments,
					                                  preNeighbor.PartIndex,
					                                  preNeighbor.FullEndFraction,
					                                  preNeighbor.FullEndFraction);
					var subCurve = new SubClosedCurve(preNeighbor.BaseSegments,
					                                  preNeighbor.PartIndex,
					                                  preNeighbor.FullEndFraction,
					                                  preNeighbor.FullStartFraction);

					// filter neighbored segments
					var filtered = new SegmentNeighbors(new SegmentPartComparer());
					foreach (KeyValuePair<SegmentPart, SegmentParts> allNeighborsPair
					         in preNeighbor.SegmentNeighbors)
					{
						SegmentPart key = allNeighborsPair.Key;
						var circleParts = new SegmentParts();

						foreach (SegmentPart segmentPart in allNeighborsPair.Value)
						{
							var neighboredPart = (SegmentPartWithNeighbor) segmentPart;
							var p = new FeaturePoint(neighboredPart.NeighborFeature,
							                         neighboredPart.NeighborTableIndex,
							                         neighboredPart.NeighborProxy.PartIndex, 0);

							int segment = neighboredPart.NeighborProxy.SegmentIndex;
							List<SegmentsSubpart> ranges;
							if (! circlePoints.TryGetValue(p, out ranges))
							{
								continue;
							}

							foreach (SegmentsSubpart range in ranges)
							{
								if (range.FullMinFraction <= segment &&
								    segment < range.FullMaxFraction)
								{
									circleParts.Add(segmentPart);
								}
							}
						}

						filtered.Add(key, circleParts);
					}

					var preConnected =
						new ConnectedSegmentsSubpart(prePart, filtered, subCurve);
					circleToComplete.Add(preConnected);
				}
			}

			public bool EqualPreNeighbors(ConnectedPartsNeighbor other)
			{
				return _preNeighbors == other._preNeighbors;
			}

			public IEnumerable<IGeometry> GetGeometry()
			{
				int tableIndex = -1;
				int oid = -1;
				IIndexedSegments baseSegments = null;
				int partIndex = -1;
				var minIndex = 0;
				var maxIndex = 0;

				foreach (NeighboredSegmentsSubpart neighbor in _preNeighbors)
				{
					if (neighbor.TableIndex != tableIndex || neighbor.BaseFeature.OID != oid ||
					    neighbor.PartIndex != partIndex ||
					    neighbor.FullMinFraction > maxIndex)
					{
						if (baseSegments != null)
						{
							yield return baseSegments.GetSubpart(
								partIndex, minIndex, 0, maxIndex - 1, 1);
						}

						tableIndex = neighbor.TableIndex;
						oid = neighbor.BaseFeature.OID;
						baseSegments = neighbor.BaseSegments;

						partIndex = neighbor.PartIndex;
						minIndex = neighbor.FullMinFraction;
					}

					maxIndex = neighbor.FullMaxFraction;
				}

				if (baseSegments != null)
				{
					yield return baseSegments.GetSubpart(partIndex, minIndex, 0, maxIndex - 1, 1);
				}
			}

			[NotNull]
			public List<ConnectedPartsNeighbor> GetNextNeighbors()
			{
				var nexts = new List<ConnectedPartsNeighbor>();
				if (_subpartIndex < _subpart.GetSegmentCount() - 1)
				{
					nexts.Add(new ConnectedPartsNeighbor(_subpart, _subpartIndex + 1,
					                                     _preNeighbors,
					                                     _continuationFinder));
					return nexts;
				}

				var search = new FeaturePoint(
					_subpart.BaseFeature, _subpart.TableIndex,
					_subpart.PartIndex, _subpart.FullEndFraction);

				List<NeighboredSegmentsSubpart> continuations =
					_continuationFinder.GetContinuations(search, _preNeighbors,
					                                     breakOnExcludeFound: false);

				if (continuations == null)
				{
					return new List<ConnectedPartsNeighbor>();
				}

				foreach (NeighboredSegmentsSubpart continuation in continuations)
				{
					IList<NeighboredSegmentsSubpart> preNeighbors =
						new List<NeighboredSegmentsSubpart>(_preNeighbors);
					preNeighbors.Add(continuation);
					nexts.Add(new ConnectedPartsNeighbor(continuation, 0, preNeighbors,
					                                     _continuationFinder));
				}

				return nexts;
			}

			[NotNull]
			public IList<ConnectedSegmentParts> GetConnectedLimits(
				[NotNull] IIndexedSegments baseSegments,
				[CanBeNull] SegmentParts segmentParts)
			{
				if (segmentParts == null)
				{
					return new List<ConnectedSegmentParts>();
				}

				SegmentProxy filterProxy = Segment;
				var filteredParts = new SegmentParts();
				foreach (SegmentPart segmentPart in segmentParts)
				{
					var part = (SegmentPartWithNeighbor) segmentPart;
					if (part.NeighborTableIndex == TableIndex &&
					    part.NeighborFeature.OID == BaseFeature.OID &&
					    part.NeighborProxy.PartIndex == filterProxy.PartIndex &&
					    part.NeighborProxy.SegmentIndex == filterProxy.SegmentIndex)
					{
						filteredParts.Add(part);
					}
				}

				IList<ConnectedSegmentParts> connecteds;
				GetConnectedParts(baseSegments, new[] {filteredParts}, 0, 0, false,
				                  out connecteds, out IList<ConnectedSegmentParts> _);

				return connecteds;
			}

			public bool IsEndEqual(ConnectedPartsContinuer nextConnected)
			{
				if (nextConnected.TableIndex != TableIndex)
				{
					return false;
				}

				if (nextConnected.BaseFeature.OID != BaseFeature.OID)
				{
					return false;
				}

				if (nextConnected.PartIndex != Segment.PartIndex)
				{
					return false;
				}

				if (nextConnected.SegmentIndex != Segment.SegmentIndex)
				{
					return false;
				}

				return true;
			}
		}

		private class ConnectedPartsContinuer
		{
			private readonly ContinuationFinder _continuationFinder;

			public ConnectedPartsContinuer([NotNull] NeighboredSegmentsSubpart subpart,
			                               int segmentIndex,
			                               [NotNull] ContinuationFinder continuationFinder)
			{
				SegmentsSubpart = subpart;
				SegmentIndex = segmentIndex;
				_continuationFinder = continuationFinder;
			}

			public int TableIndex => SegmentsSubpart.TableIndex;

			[NotNull]
			public IReadOnlyFeature BaseFeature => SegmentsSubpart.BaseFeature;

			public int PartIndex => SegmentsSubpart.PartIndex;

			public int FullMinFraction => SegmentsSubpart.FullMinFraction;

			public int SegmentIndex { get; }

			public override string ToString()
			{
				return $"OID:{BaseFeature.OID}, P:{PartIndex}, S:{SegmentIndex}";
			}

			[NotNull]
			public NeighboredSegmentsSubpart SegmentsSubpart { get; }

			[NotNull]
			public List<ConnectedPartsContinuer> GetNextConnecteds<TSegmentsSubpart>(
				[NotNull] IList<TSegmentsSubpart> excludeList)
				where TSegmentsSubpart : SegmentsSubpart
			{
				var nexts = new List<ConnectedPartsContinuer>();

				if (SegmentIndex + 1 < SegmentsSubpart.GetSegmentCount())
				{
					var next = new ConnectedPartsContinuer(
						SegmentsSubpart, SegmentIndex + 1, _continuationFinder);
					nexts.Add(next);
					return nexts;
				}

				var search = new FeaturePoint(BaseFeature, TableIndex, SegmentsSubpart.PartIndex,
				                              SegmentsSubpart.FullEndFraction);

				List<NeighboredSegmentsSubpart> continuations =
					_continuationFinder.GetContinuations(
						search, excludeList, breakOnExcludeFound: true);

				if (continuations == null)
				{
					return nexts;
				}

				foreach (NeighboredSegmentsSubpart continuation in continuations)
				{
					var next = new ConnectedPartsContinuer(continuation, 0,
					                                       _continuationFinder);
					nexts.Add(next);
				}

				return nexts;
			}
		}

		private class ConnectedPartsGrower
		{
			private IList<SegmentNeighborsWithSortedParts> _neighbors;

			private int _lastIndex;
			private double _lastFraction;

			public ConnectedPartsGrower([NotNull] NeighboredSegmentsSubpart start)
			{
				Subparts = new List<NeighboredSegmentsSubpart> {start};
				_neighbors = new List<SegmentNeighborsWithSortedParts>
				             {
					             new SegmentNeighborsWithSortedParts(
						             new SegmentPartComparer(),
						             new SegmentPartWithNeighborComparer())
				             };
				_lastIndex = 0;
				_lastFraction = 0;
			}

			private ConnectedPartsGrower() { }

			public ConnectedPartsGrower Copy()
			{
				var copy = new ConnectedPartsGrower();

				copy.Subparts = new List<NeighboredSegmentsSubpart>(Subparts);
				copy._neighbors = new List<SegmentNeighborsWithSortedParts>(_neighbors);
				copy._lastIndex = _lastIndex;
				copy._lastFraction = _lastFraction;

				return copy;
			}

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.Append($"{_lastIndex},{_lastFraction:N2} of ");
				foreach (NeighboredSegmentsSubpart subpart in Subparts)
				{
					string txt = string.Format("OID:{0}, P:{1}, S:{2}->{3}",
					                           subpart.BaseFeature.OID, subpart.PartIndex,
					                           subpart.FullStartFraction,
					                           subpart.FullEndFraction);

					sb.Append($"{txt};");
				}

				return sb.ToString();
			}

			public ConnectedPartsNeighbor CircularNeighbor { get; set; }

			public IList<NeighboredSegmentsSubpart> Subparts { get; private set; }

			public void Add([NotNull] ConnectedPartsContinuer nextConnected)
			{
				SegmentsSubpart lastPart = Subparts[Subparts.Count - 1];
				if (lastPart.TableIndex != nextConnected.TableIndex ||
				    lastPart.BaseFeature.OID != nextConnected.BaseFeature.OID ||
				    lastPart.PartIndex != nextConnected.PartIndex ||
				    lastPart.FullMinFraction != nextConnected.FullMinFraction)
				{
					Assert.AreEqual(0, nextConnected.SegmentIndex,
					                "Invalid segment index for new subpart");

					Subparts.Add(nextConnected.SegmentsSubpart);
					_neighbors.Add(
						new SegmentNeighborsWithSortedParts(
							new SegmentPartComparer(),
							new SegmentPartWithNeighborComparer()));
				}
				else
				{
					Assert.AreEqual(_lastIndex + 1, nextConnected.SegmentIndex,
					                "Invalid segment index for new subpart");
				}

				_lastIndex = nextConnected.SegmentIndex;

				_lastFraction = 0;
			}

			public IList<ConnectedSegmentParts> GetLastConnectedParts(
				[NotNull] ConnectedPartsNeighbor neighbored)
			{
				NeighboredSegmentsSubpart lastPart = Subparts[Subparts.Count - 1];

				SegmentProxy lastSegment = lastPart.GetSegment(_lastIndex);

				var key = new SegmentPart(lastSegment, 0, 1, true);
				SegmentParts allSegmentParts;
				lastPart.SegmentNeighbors.TryGetValue(key, out allSegmentParts);

				IList<ConnectedSegmentParts> connectedParts =
					neighbored.GetConnectedLimits(lastPart.BaseSegments, allSegmentParts);

				return connectedParts;
			}

			public bool Add([NotNull] ConnectedPartsNeighbor neighbored, bool addToPrevious,
			                IList<ConnectedSegmentParts> neighboredConnectedParts)
			{
				if (addToPrevious)
				{
					AddToPreviousParts(neighbored, addToLastFraction: false);
				}

				IList<ConnectedSegmentParts> connectedParts =
					neighboredConnectedParts ?? GetLastConnectedParts(neighbored);
				if (connectedParts.Count == 0)
				{
					return false;
				}

				NeighboredSegmentsSubpart lastPart = Subparts[Subparts.Count - 1];
				SegmentNeighborsWithSortedParts lastNeighbors = _neighbors[Subparts.Count - 1];

				IList<DirectedMinMaxConnected> connectedLimits = GetSortedLimits(connectedParts,
					lastPart);

				var addedAny = false;
				foreach (DirectedMinMaxConnected connectedLimit in connectedLimits)
				{
					if (connectedLimit.Min > _lastFraction)
					{
						return false;
					}

					addedAny = true;
					double max = connectedLimit.Max >= connectedLimit.Min ? connectedLimit.Max : 1;
					_lastFraction = Math.Max(_lastFraction, max);

					foreach (SegmentPart segmentPart in connectedLimit.Parts)
					{
						lastNeighbors.AddSorted(segmentPart);
					}
				}

				return addedAny;
			}

			public bool AddToPreviousParts([NotNull] ConnectedPartsNeighbor neighbored,
			                               bool addToLastFraction)
			{
				var addedAny = false;
				for (var iPart = 0; iPart < Subparts.Count; iPart++)
				{
					NeighboredSegmentsSubpart prePart = Subparts[iPart];
					SegmentNeighborsWithSortedParts preNeighbors = _neighbors[iPart];

					int preSegmentCount = prePart.GetSegmentCount();
					for (var preIndex = 0; preIndex < preSegmentCount; preIndex++)
					{
						if (iPart == Subparts.Count - 1)
						{
							if (preIndex > _lastIndex)
							{
								continue;
							}

							if (preIndex == _lastIndex && ! addToLastFraction)
							{
								continue;
							}
						}

						SegmentProxy preSegment = prePart.GetSegment(preIndex);

						var preKey = new SegmentPart(preSegment, 0, 1, true);
						SegmentParts allPreParts;
						prePart.SegmentNeighbors.TryGetValue(preKey, out allPreParts);

						IList<ConnectedSegmentParts> preConnectedParts =
							neighbored.GetConnectedLimits(prePart.BaseSegments, allPreParts);

						if (preConnectedParts.Count == 0)
						{
							continue;
						}

						if (iPart == Subparts.Count - 1 && preIndex == _lastIndex)
						{
							IList<DirectedMinMaxConnected> connectedLimits =
								GetSortedLimits(preConnectedParts,
								                prePart);

							var remaining =
								new List<ConnectedSegmentParts>(connectedLimits.Count);
							foreach (DirectedMinMaxConnected connectedLimit in connectedLimits)
							{
								if (connectedLimit.Max <= _lastFraction)
								{
									remaining.Add(connectedLimit.Parts);
								}
							}

							preConnectedParts = remaining;
						}

						foreach (ConnectedSegmentParts preConnectedPart in preConnectedParts)
						{
							foreach (SegmentPart part in preConnectedPart)
							{
								if (preNeighbors.AddSorted(part))
								{
									addedAny = true;
								}
							}
						}
					}
				}

				return addedAny;
			}

			[NotNull]
			private static List<DirectedMinMaxConnected> GetSortedLimits(
				[NotNull] IList<ConnectedSegmentParts> connectedParts,
				[NotNull] NeighboredSegmentsSubpart lastPart)
			{
				var connectedLimits = new List<DirectedMinMaxConnected>(connectedParts.Count);

				foreach (ConnectedSegmentParts connected in connectedParts)
				{
					var segmentIndex = (int) connected.StartFullIndex;
					double min = connected.StartFullIndex - segmentIndex;
					double max = connected.EndFullIndex - segmentIndex;

					if (lastPart.FullEndFraction < lastPart.FullStartFraction)
					{
						double invertMin = 1 - max;
						double invertMax = 1 - min;

						min = invertMin;
						max = invertMax;
					}

					var minMax = new DirectedMinMaxConnected(connected, min, max);
					connectedLimits.Add(minMax);
				}

				connectedLimits.Sort((x, y) => x.Min.CompareTo(y.Min));
				return connectedLimits;
			}

			public bool IsFullyCovered()
			{
				return _lastFraction >= 1;
			}

			[NotNull]
			public ConnectedLines GetConnectedLines()
			{
				var connectedParts = new List<ConnectedSegmentsSubpart>();

				int partCount = Subparts.Count;

				for (var partIndex = 0; partIndex < partCount; partIndex++)
				{
					NeighboredSegmentsSubpart part = Subparts[partIndex];

					double min;
					double max;
					if (partIndex != partCount - 1)
					{
						min = part.FullMinFraction;
						max = part.FullMaxFraction;
					}
					else
					{
						if (part.FullStartFraction <= part.FullMinFraction)
						{
							min = part.FullStartFraction;
							max = part.FullStartFraction + _lastIndex + _lastFraction;
						}
						else
						{
							min = part.FullStartFraction - _lastIndex - _lastFraction;
							max = part.FullStartFraction;
						}
					}

					var curve = new SubClosedCurve(part.BaseSegments, part.PartIndex, min,
					                               max);
					var connectedPart =
						new ConnectedSegmentsSubpart(
							part, _neighbors[partIndex].AsSegmentNeighbors(),
							curve);

					connectedParts.Add(connectedPart);
				}

				var connected = new ConnectedLines(connectedParts);
				return connected;
			}
		}

		private class NeighborsContinuations
		{
			private readonly IList<ConnectedSegmentsSubpart> _segments;

			private Dictionary<SegmentPartWithNeighbor, List<SegmentPartWithNeighborEx>>
				_relatedParts;

			private Dictionary<FeaturePoint, SegmentPartWithNeighbor> _segmentPoints;

			public NeighborsContinuations(IList<ConnectedLinesEx> allLines)
			{
				var segments = new List<ConnectedSegmentsSubpart>();
				foreach (ConnectedLinesEx line in allLines)
				{
					segments.AddRange(line.Line.BaseSegments);
				}

				_segments = segments;

				InitNeighborHood();
			}

			public void SetRelationIndexToConnecteds(
				ContinuationFinder continuationFinder, int relationIndex)
			{
				var connected =
					new Dictionary<SegmentPartWithNeighbor, bool>(new NeighborSegmentComparer());

				foreach (ConnectedSegmentsSubpart seg in _segments)
				{
					foreach (SegmentParts neighbors in seg.SegmentNeighbors.Values)
					{
						foreach (SegmentPart neighbor in neighbors)
						{
							var segmentPart = (SegmentPartWithNeighbor) neighbor;
							if (segmentPart.MinRelationIndex == relationIndex)
							{
								FindConnectedParts(
									new SegmentPartWithNeighborEx(seg, segmentPart),
									continuationFinder, connected);
							}
						}
					}
				}

				SetRelatedParts(connected.Keys, relationIndex);
			}

			private void SetRelatedParts(IEnumerable<SegmentPartWithNeighbor> withNeighbors,
			                             int relationIndex)
			{
				foreach (SegmentPartWithNeighbor withNeighbor in withNeighbors)
				{
					foreach (SegmentPartWithNeighborEx segment in _relatedParts[withNeighbor])
					{
						segment.SegmentPart.MinRelationIndex = Math.Min(
							segment.SegmentPart.MinRelationIndex, relationIndex);
					}
				}
			}

			private void InitNeighborHood()
			{
				var nbSegmentCmp = new NeighborSegmentComparer();

				IList<ConnectedSegmentsSubpart> segments = _segments;
				var neighborParts =
					new Dictionary<SegmentPartWithNeighbor, List<SegmentPartWithNeighborEx>>(
						nbSegmentCmp);

				var selfParts =
					new Dictionary<FeaturePoint, SegmentPartWithNeighbor>(
						new FeaturePointComparer());

				foreach (ConnectedSegmentsSubpart seg in segments)
				{
					foreach (SegmentParts neighbors in seg.SegmentNeighbors.Values)
					{
						foreach (SegmentPart neighbor in neighbors)
						{
							var segmentPart = (SegmentPartWithNeighbor) neighbor;
							selfParts[segmentPart.CreateNeighborFeaturePoint()] = segmentPart;

							var nbSeg = segmentPart.NeighborProxy as SegmentProxy;
							if (nbSeg == null)
							{
								continue;
							}

							List<SegmentPartWithNeighborEx> neighborList;
							if (! neighborParts.TryGetValue(segmentPart, out neighborList))
							{
								neighborList = new List<SegmentPartWithNeighborEx>();
								neighborParts.Add(segmentPart, neighborList);
							}

							neighborList.Add(new SegmentPartWithNeighborEx(seg, segmentPart));
						}
					}
				}

				_relatedParts = neighborParts;
				_segmentPoints = selfParts;
			}

			private void FindConnectedParts(
				SegmentPartWithNeighborEx search, ContinuationFinder continuationFinder,
				Dictionary<SegmentPartWithNeighbor, bool> connected)
			{
				connected[search.SegmentPart] = true;

				var neighbors = new List<FeaturePoint>();

				IReadOnlyFeature nbFeature = search.SegmentPart.NeighborFeature;
				int nbTableIdx = search.SegmentPart.NeighborTableIndex;

				var searchSeg = (SegmentProxy) search.SegmentPart.NeighborProxy;
				ISegmentProxy nextSeg = searchSeg.GetNextSegment(nbFeature.Shape);
				if (nextSeg != null)
				{
					neighbors.Add(new FeaturePoint(nbFeature, nbTableIdx, nextSeg.PartIndex,
					                               nextSeg.SegmentIndex));
				}

				ISegmentProxy preSeg = searchSeg.GetPreviousSegment(nbFeature.Shape);
				if (preSeg != null)
				{
					neighbors.Add(new FeaturePoint(nbFeature, nbTableIdx, preSeg.PartIndex,
					                               preSeg.SegmentIndex));
				}

				List<NeighboredSegmentsSubpart> startContinuations =
					continuationFinder.GetContinuations(
						search.SegmentPart.CreateNeighborFeaturePoint(),
						new List<SegmentsSubpart>());
				neighbors.AddRange(GetFirstSegments(startContinuations));

				List<NeighboredSegmentsSubpart> endContinuations =
					continuationFinder.GetContinuations(
						search.SegmentPart.CreateNeighborFeaturePoint(atEnd: true),
						new List<SegmentsSubpart>());
				neighbors.AddRange(GetFirstSegments(endContinuations));

				var newConnecteds = new List<SegmentPartWithNeighbor>();
				foreach (FeaturePoint neighborPoint in neighbors)
				{
					SegmentPartWithNeighbor neighborSegment;
					if (! _segmentPoints.TryGetValue(neighborPoint, out neighborSegment))
					{
						continue;
					}

					bool alreadyConnected;
					if (! connected.TryGetValue(neighborSegment, out alreadyConnected))
					{
						alreadyConnected = false;
					}

					if (! alreadyConnected)
					{
						newConnecteds.Add(neighborSegment);
						connected[neighborSegment] = true;
					}
				}

				foreach (SegmentPartWithNeighbor newConnected in newConnecteds)
				{
					SegmentPartWithNeighborEx newSearch = _relatedParts[newConnected].First();
					FindConnectedParts(newSearch, continuationFinder, connected);
				}
			}

			private IEnumerable<FeaturePoint> GetFirstSegments(
				[CanBeNull] List<NeighboredSegmentsSubpart> continuations)
			{
				if (continuations == null)
				{
					yield break;
				}

				foreach (NeighboredSegmentsSubpart continuation in continuations)
				{
					foreach (SegmentProxy segment in continuation.GetSegments())
					{
						var neighborPoint = new FeaturePoint(continuation.BaseFeature,
						                                     continuation.TableIndex,
						                                     segment.PartIndex,
						                                     segment.SegmentIndex);
						yield return neighborPoint;
						break;
					}
				}
			}
		}

		private class ContinuationFinder
		{
			private readonly Dictionary<FeaturePoint, Dictionary<FeaturePoint, FeaturePoint>>
				_groupedJunctions;

			private readonly Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>>
				_splittedParts;

			public ContinuationFinder(
				[NotNull] Dictionary<FeaturePoint, Dictionary<FeaturePoint, FeaturePoint>>
					groupedJunctions,
				[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>> splittedParts)
			{
				_groupedJunctions = groupedJunctions;
				_splittedParts = splittedParts;
			}

			[CanBeNull]
			public SegmentParts GetSegments([NotNull] FeaturePoint segment)
			{
				var featurekey = new FeaturePoint(segment.Feature, segment.TableIndex,
				                                  segment.Part, 0);
				List<NeighboredSegmentsSubpart> subparts;
				if (! _splittedParts.TryGetValue(featurekey, out subparts))
				{
					return null;
				}

				var segKey = new SegmentPart(segment.Part, (int) segment.FullFraction, 0, 1, true);

				foreach (NeighboredSegmentsSubpart subpart in subparts)
				{
					if (subpart.FullMinFraction > segment.FullFraction ||
					    subpart.FullMaxFraction <= segment.FullFraction)
					{
						continue;
					}

					SegmentParts segmentParts;
					if (subpart.SegmentNeighbors.TryGetValue(segKey, out segmentParts))
					{
						return segmentParts;
					}
				}

				return null;
			}

			[CanBeNull]
			public List<NeighboredSegmentsSubpart> GetContinuations<TSegmentsSubpart>(
				[NotNull] FeaturePoint search, [NotNull] IList<TSegmentsSubpart> excludeList,
				bool breakOnExcludeFound = false)
				where TSegmentsSubpart : SegmentsSubpart
			{
				Dictionary<FeaturePoint, FeaturePoint> grouped;
				if (! _groupedJunctions.TryGetValue(search, out grouped))
				{
					return null;
				}

				List<NeighboredSegmentsSubpart> continuations = GetContinuations(grouped,
					excludeList,
					breakOnExcludeFound);

				return continuations;
			}

			[CanBeNull]
			private List<NeighboredSegmentsSubpart> GetContinuations<TSegmentsSubpart>(
				[NotNull] Dictionary<FeaturePoint, FeaturePoint> jcts,
				[NotNull] IList<TSegmentsSubpart> excludeList, bool breakOnExcludeFound)
				where TSegmentsSubpart : SegmentsSubpart
			{
				var continuations =
					new List<NeighboredSegmentsSubpart>();
				foreach (FeaturePoint jct in jcts.Keys)
				{
					var partKey = new FeaturePoint(jct.Feature, jct.TableIndex, jct.Part, 0);
					List<NeighboredSegmentsSubpart> splitParts;

					if (! _splittedParts.TryGetValue(partKey, out splitParts))
					{
						continue;
					}

					foreach (NeighboredSegmentsSubpart part in splitParts)
					{
						if (part.BaseFeature != jct.Feature ||
						    part.PartIndex != jct.Part)
						{
							continue;
						}

						bool startAtJunction =
							Math.Abs(part.FullStartFraction - jct.FullFraction) < _epsi;
						bool endAtJunction =
							Math.Abs(part.FullEndFraction - jct.FullFraction) < _epsi;

						if (! startAtJunction && ! endAtJunction)
						{
							continue;
						}

						var exists = false;
						foreach (TSegmentsSubpart exclude in excludeList)
						{
							if (exclude.BaseFeature == part.BaseFeature &&
							    exclude.PartIndex == part.PartIndex &&
							    exclude.FullMinFraction == part.FullMinFraction)
							{
								if (breakOnExcludeFound &&
								    exclude != excludeList[excludeList.Count - 1])
								{
									return null;
								}

								exists = true;
								break;
							}
						}

						if (exists)
						{
							continue;
						}

						if (startAtJunction)
						{
							continuations.Add(part);
						}

						if (endAtJunction)
						{
							var invertedPart =
								new SegmentsSubpart(
									part.BaseFeature, part.TableIndex, part.BaseSegments,
									part.PartIndex, part.FullEndFraction, part.FullStartFraction);

							continuations.Add(new NeighboredSegmentsSubpart(
								                  invertedPart, part.SegmentNeighbors));
						}
					}
				}

				return continuations;
			}
		}

		private class SegmentNeighborsWithSortedParts :
			SortedDictionary<SegmentPart, SimpleSet<SegmentPart>>
		{
			[NotNull] private readonly IEqualityComparer<SegmentPart> _partsSortComparer;

			public SegmentNeighborsWithSortedParts(
				[NotNull] IComparer<SegmentPart> keyComparer,
				[NotNull] IEqualityComparer<SegmentPart> partsSortComparer)
				: base(keyComparer)
			{
				_partsSortComparer = partsSortComparer;
			}

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.Append($"#: {Count}: ");
				var i = 0;
				SegmentPart lastKey = null;
				SimpleSet<SegmentPart> lastParts = null;
				foreach (KeyValuePair<SegmentPart, SimpleSet<SegmentPart>> pair in this)
				{
					lastKey = pair.Key;
					lastParts = pair.Value;

					i++;
					if (i > 3)
					{
						continue;
					}

					Append(sb, lastKey, lastParts);
				}

				if (i > 4)
				{
					sb.Append("...;");
				}

				if (i >= 4)
				{
					Append(sb, lastKey, lastParts);
				}

				return sb.ToString();
			}

			private void Append(StringBuilder sb, SegmentPart key, SimpleSet<SegmentPart> parts)
			{
				sb.Append($"P:{key.PartIndex},");
				var i = 0;
				SegmentPart lastPart = null;
				foreach (SegmentPart segmentPart in parts)
				{
					lastPart = segmentPart;
					i++;
					if (i > 3)
					{
						continue;
					}

					sb.Append($"[{lastPart.FullMin:N1},{lastPart.FullMax:N1}],");
				}

				if (i > 4)
				{
					sb.Append("..,");
				}

				if (i >= 4)
				{
					sb.Append($"[{Assert.NotNull(lastPart).FullMin:N1},{lastPart.FullMax:N1}],");
				}

				sb.Remove(sb.Length - 1, 1);
				sb.Append(";");
			}

			public SegmentNeighbors AsSegmentNeighbors()
			{
				var segmentNeighbors = new SegmentNeighbors(Comparer);
				foreach (KeyValuePair<SegmentPart, SimpleSet<SegmentPart>> pair in this)
				{
					var parts = new SegmentParts();
					parts.AddRange(pair.Value);
					segmentNeighbors.Add(pair.Key, parts);
				}

				return segmentNeighbors;
			}

			public bool AddSorted(SegmentPart part)
			{
				var key = new SegmentPart(part.PartIndex, part.SegmentIndex, 0, 1, true);
				SimpleSet<SegmentPart> parts;
				if (! TryGetValue(key, out parts))
				{
					parts = new SimpleSet<SegmentPart>(_partsSortComparer);
					Add(key, parts);
				}

				return parts.TryAdd(part);
			}
		}

		private class ConnectedLinesEx
		{
			private Dictionary<FeaturePoint, List<ConnectedSegmentsSubpart>> _segmentsDict;
			private Box _joined;
			private Dictionary<FeaturePoint, ConnectedSegmentsSubpart> _joinedParts;

			public ConnectedLinesEx([NotNull] ConnectedLines line, double minLength,
			                        double? reportedLength = null)
			{
				Line = line;
				Length = reportedLength ?? line.GetSubLength(0);
				Box = line.GetExtent();

				MinLength = minLength;
			}

			[NotNull]
			public ConnectedLines Line { get; }

			public double Length { get; }

			public double MinLength { get; }

			[CanBeNull]
			public ConnectedPartsNeighbor CircularNeighbor { get; set; }

			public bool IsCircular => CircularNeighbor != null;

			public bool WithinNear { get; set; }

			[NotNull]
			public Box Box { get; }

			// needed for tile management
			[NotNull]
			public Box JoinedBox => _joined ?? (_joined = Box.Clone());

			[NotNull]
			public Dictionary<FeaturePoint, ConnectedSegmentsSubpart> JoinedParts
			{
				get
				{
					if (_joinedParts == null)
					{
						var joinedParts =
							new Dictionary<FeaturePoint, ConnectedSegmentsSubpart>(
								new FeaturePointComparer());
						Include(joinedParts, Line.BaseSegments);
						_joinedParts = joinedParts;
					}

					return _joinedParts;
				}
			}

			public override string ToString()
			{
				return Line.ToString();
			}

			public void Include([NotNull] ConnectedLinesEx joined)
			{
				Include(JoinedParts, joined.Line.BaseSegments);
				JoinedBox.Include(joined.JoinedBox);
			}

			private static void Include(
				[NotNull] Dictionary<FeaturePoint, ConnectedSegmentsSubpart> joinedParts,
				[NotNull] IEnumerable<ConnectedSegmentsSubpart> parts)
			{
				foreach (ConnectedSegmentsSubpart toJoin in parts)
				{
					var key = new FeaturePoint(
						toJoin.BaseFeature, toJoin.TableIndex,
						toJoin.PartIndex, toJoin.FullMinFraction);
					if (! joinedParts.ContainsKey(key))
					{
						joinedParts.Add(key, toJoin);
					}
				}
			}

			[NotNull]
			private Dictionary<FeaturePoint, List<ConnectedSegmentsSubpart>> SegmentsDict
			{
				get
				{
					if (_segmentsDict == null)
					{
						var dict = new Dictionary<FeaturePoint, List<ConnectedSegmentsSubpart>>(
							new FeaturePointComparer());

						foreach (ConnectedSegmentsSubpart part in Line.BaseSegments)
						{
							var key = new FeaturePoint(
								part.BaseFeature, part.TableIndex,
								part.PartIndex, part.FullMinFraction);

							List<ConnectedSegmentsSubpart> parts;
							if (! dict.TryGetValue(key, out parts))
							{
								parts = new List<ConnectedSegmentsSubpart>();
								dict.Add(key, parts);
							}

							parts.Add(part);
						}

						_segmentsDict = dict;
					}

					return _segmentsDict;
				}
			}

			[NotNull]
			public IEnumerable<ConnectedSegmentsSubpart> GetParts(
				[NotNull] ConnectedSegmentsSubpart search)
			{
				var key = new FeaturePoint(
					search.BaseFeature, search.TableIndex,
					search.PartIndex, search.FullMinFraction);

				List<ConnectedSegmentsSubpart> parts;
				if (! SegmentsDict.TryGetValue(key, out parts))
				{
					yield break;
				}

				foreach (ConnectedSegmentsSubpart part in parts)
				{
					yield return part;
				}
			}
		}

		private class ConnectedLines : IIndexedSegments
		{
			public ConnectedLines([NotNull] IList<ConnectedSegmentsSubpart> baseSegments)
			{
				BaseSegments = baseSegments;
			}

			public bool IsWithinDistance([NotNull] IFeatureDistanceProvider distanceProvider)
			{
				foreach (ConnectedSegmentsSubpart subpart in BaseSegments)
				{
					if (subpart.FullMaxFraction - subpart.FullMinFraction >
					    subpart.SegmentNeighbors.Count)
					{
						return false;
					}
				}

				var cap = new RoundCap();

				foreach (ConnectedSegmentsSubpart subpart in BaseSegments)
				{
					foreach (KeyValuePair<SegmentPart, SegmentParts> pair in
					         subpart.SegmentNeighbors)
					{
						var first = true;
						SegmentHull segmentHull = null;

						var allLimits = new List<double[]>();
						foreach (SegmentPart segmentPart in pair.Value)
						{
							if (first)
							{
								segmentHull =
									new SegmentHull(
										Assert.NotNull((SegmentProxy) segmentPart.SegmentProxy), 0,
										cap,
										cap);
								first = false;
							}

							var neighboredPart = (SegmentPartWithNeighbor) segmentPart;

							IFeatureRowsDistance rowsDistance =
								distanceProvider.GetRowsDistance(neighboredPart.NeighborFeature,
								                                 neighboredPart.NeighborTableIndex);

							double rowDistance = rowsDistance.GetRowDistance();

							var neighborProxy = (SegmentProxy) neighboredPart.NeighborProxy;

							var segmentPair =
								new SegmentPair2D(
									segmentHull,
									new SegmentHull(neighborProxy, rowDistance, cap, cap));
							IList<double[]> limits;
							bool coincident;
							segmentPair.CutCurveHull(0, out limits, out _, out _, out coincident);
							if (coincident)
							{
								return true;
							}

							allLimits = CombineLimits(allLimits, limits);

							if (allLimits.Count > 0 && allLimits[0][0] <= 0 && allLimits[0][1] >= 0)
							{
								break;
							}
						}

						if (allLimits.Count <= 0 || allLimits[0][0] > 0 || allLimits[0][1] < 1)
						{
							return false;
						}
					}
				}

				return true;
			}

			[NotNull]
			private static List<double[]> CombineLimits([NotNull] List<double[]> allLimits,
			                                            [NotNull] IList<double[]> add)
			{
				allLimits.AddRange(add);
				allLimits.Sort((x, y) => x[0].CompareTo(y[0]));
				var result = new List<double[]>();
				double[] current = null;
				foreach (double[] limit in allLimits)
				{
					if (current == null || current[1] < limit[0])
					{
						result.Add(limit);
						current = limit;
					}
					else
					{
						current[1] = Math.Max(current[1], limit[1]);
					}
				}

				while (result.Count > 0 && result[0][1] < 0)
				{
					result.RemoveAt(0);
				}

				return result;
			}

			public override string ToString()
			{
				var s = new StringBuilder();
				foreach (ConnectedSegmentsSubpart part in BaseSegments)
				{
					s.AppendFormat("OID:{0}, P:{1}, S:{2:N2}->{3:N2}; ",
					               part.BaseFeature.OID, part.PartIndex,
					               part.ConnectedCurve.StartFullIndex,
					               part.ConnectedCurve.EndFullIndex);
				}

				return s.ToString();
			}

			public SegmentPairRelation RelevantSegment { get; set; }

			public void Dispose() { }

			public IEnvelope Envelope
			{
				get { throw new NotImplementedException(); }
			}

			public bool AllowIndexing { get; set; }

			[NotNull]
			public IList<ConnectedSegmentsSubpart> BaseSegments { get; }

			public bool Handled { get; set; }

			public IEnumerable<SegmentProxy> GetSegments()
			{
				var baseSegmentIndex = 0;
				foreach (ConnectedSegmentsSubpart baseSegments in BaseSegments)
				{
					foreach (SegmentProxy segment in baseSegments.GetSegments())
					{
						yield return
							new ConnectedLinesSegment(0,
							                          baseSegments.GetSegmentIndex(segment) +
							                          baseSegmentIndex, segment);
					}

					baseSegmentIndex += baseSegments.GetSegmentCount();
				}
			}

			public IEnumerable<SegmentProxy> GetSegments(IBox box)
			{
				var baseSegmentIndex = 0;
				foreach (ConnectedSegmentsSubpart baseSegments in BaseSegments)
				{
					foreach (SegmentProxy segment in baseSegments.GetSegments(box))
					{
						yield return
							new ConnectedLinesSegment(0,
							                          baseSegments.GetSegmentIndex(segment) +
							                          baseSegmentIndex, segment);
					}

					baseSegmentIndex += baseSegments.GetSegmentCount();
				}
			}

			public SegmentProxy GetSegment(int partIndex, int segmentIndex)
			{
				int baseIndex;
				int baseSegmentIndex;
				GetBaseSegment(partIndex, segmentIndex, out baseIndex, out baseSegmentIndex);
				return new ConnectedLinesSegment(partIndex, segmentIndex,
				                                 BaseSegments[baseIndex].GetSegment(
					                                 baseSegmentIndex));
			}

			private void GetBaseSegment(int partIndex, int segmentIndex,
			                            out int baseIndex,
			                            out int baseSegmentIndex)
			{
				if (partIndex != 0)
				{
					throw new InvalidProgramException("Invalid partIndex " + partIndex);
				}

				var startSegment = 0;
				for (var i = 0; i < BaseSegments.Count; i++)
				{
					SegmentsSubpart baseSegments = BaseSegments[i];

					int baseSegmentsCount = baseSegments.GetSegmentCount();
					if (startSegment + baseSegmentsCount > segmentIndex)
					{
						baseIndex = i;
						baseSegmentIndex = segmentIndex - startSegment;
						return;
					}

					startSegment += baseSegmentsCount;
				}

				throw new InvalidProgramException("Invalid segmentIndex " + segmentIndex);
			}

			public bool IsPartClosed(int part)
			{
				if (BaseSegments.Count == 1)
				{
					return BaseSegments[0].IsClosed();
				}

				return false;
			}

			public int GetPartsCount()
			{
				return 1;
			}

			public int GetPartSegmentCount(int part)
			{
				if (part != 0)
				{
					throw new InvalidOperationException("Invalid part " + part);
				}

				var segmentCount = 0;
				foreach (ConnectedSegmentsSubpart baseSegments in BaseSegments)
				{
					segmentCount += baseSegments.GetSegmentCount();
				}

				return segmentCount;
			}

			public IPolyline GetSubpart(int partIndex, int startSegmentIndex,
			                            double startFraction,
			                            int endSegmentIndex, double endFraction)
			{
				int baseStartIndex;
				int baseStartSegment;
				GetBaseSegment(partIndex, startSegmentIndex, out baseStartIndex,
				               out baseStartSegment);

				int baseEndIndex;
				int baseEndSegment;
				GetBaseSegment(partIndex, endSegmentIndex, out baseEndIndex, out baseEndSegment);

				if (baseStartIndex == baseEndIndex)
				{
					return BaseSegments[baseStartIndex].GetSubpart(baseStartSegment,
						startFraction,
						baseEndSegment, endFraction);
				}

				IList<IGeometry> lines = new List<IGeometry>(baseEndIndex - baseStartIndex + 1);
				SegmentsSubpart start = BaseSegments[baseStartIndex];
				lines.Add(start.GetSubpart(baseStartSegment, startFraction,
				                           start.GetSegmentCount() - 1, 1.0));
				for (int baseIndex = baseStartIndex + 1; baseIndex < baseEndIndex; baseIndex++)
				{
					SegmentsSubpart between = BaseSegments[baseIndex];
					lines.Add(between.GetSubpart(0, 0.0, between.GetSegmentCount() - 1, 1.0));
				}

				SegmentsSubpart end = BaseSegments[baseStartIndex];
				lines.Add(end.GetSubpart(0, 0.0, baseEndSegment, endFraction));

				return (IPolyline) GeometryFactory.CreateUnion(lines, 0);
			}

			public bool TryGetSegmentNeighborhoods(IIndexedSegments neighborSegments,
			                                       IBox commonBox,
			                                       double searchDistance,
			                                       out IEnumerable<SegmentProxyNeighborhood>
				                                       neighborhoods)
			{
				throw new NotImplementedException();
			}

			[NotNull]
			public static List<NeighboredSegmentsSubpart> Combine(
				[NotNull] IList<ConnectedSegmentsSubpart> connectedParts,
				[CanBeNull] NeighboredSegmentsSubpart continuation)
			{
				List<NeighboredSegmentsSubpart> parts = GetFilteredParts(connectedParts,
					continuation);

				if (continuation != null)
				{
					var lineFeatures = new Dictionary<IReadOnlyFeature, List<SegmentsSubpart>>();

					foreach (ConnectedSegmentsSubpart linePart in connectedParts)
					{
						List<SegmentsSubpart> segmentParts;
						if (! lineFeatures.TryGetValue(linePart.BaseFeature, out segmentParts))
						{
							segmentParts = new List<SegmentsSubpart>();
							lineFeatures.Add(linePart.BaseFeature, segmentParts);
						}

						segmentParts.Add(linePart);
					}

					SegmentNeighbors copyNeighbors = continuation.SegmentNeighbors.Select(
						p =>
						{
							var neighboredPart =
								p as
									SegmentPartWithNeighbor;
							if (neighboredPart == null
							   )
							{
								return false;
							}

							List<SegmentsSubpart>
								featureSubparts;
							if (
								! lineFeatures
									.TryGetValue(
										neighboredPart
											.NeighborFeature,
										out
										featureSubparts)
							)
							{
								return true;
							}

							foreach (
								SegmentsSubpart
									featureSubpart
								in featureSubparts)
							{
								if (featureSubpart
									    .PartIndex ==
								    p.PartIndex &&
								    featureSubpart
									    .FullMinFraction <=
								    p.SegmentIndex &&
								    featureSubpart
									    .FullMaxFraction >
								    p.SegmentIndex)
								{
									return false;
								}
							}

							return true;
						});

					var continuationCopy = new NeighboredSegmentsSubpart(continuation,
						copyNeighbors);

					parts.Add(continuationCopy);
				}

				return parts;
			}

			[NotNull]
			private static List<NeighboredSegmentsSubpart> GetFilteredParts<T>(
				[NotNull] IList<T> connectedParts,
				[CanBeNull] SegmentsSubpart partsToRemove)
				where T : NeighboredSegmentsSubpart
			{
				var parts = new List<NeighboredSegmentsSubpart>();
				foreach (T part in connectedParts)
				{
					SegmentNeighbors copyNeighbors = part.SegmentNeighbors.Select(
						p =>
						{
							var neighboredPart =
								p as SegmentPartWithNeighbor;
							if (neighboredPart == null)
							{
								return false;
							}

							if (partsToRemove == null ||
							    partsToRemove.BaseFeature !=
							    neighboredPart.NeighborFeature
							   )
							{
								return true;
							}

							ISegmentProxy neighborSegment = neighboredPart.NeighborProxy;
							return partsToRemove.PartIndex != neighborSegment.PartIndex ||
							       partsToRemove.FullMinFraction > neighborSegment.SegmentIndex ||
							       partsToRemove.FullMaxFraction <= neighborSegment.SegmentIndex;
						});

					var copy = new NeighboredSegmentsSubpart(part, copyNeighbors);

					parts.Add(copy);
				}

				return parts;
			}

			public double GetSubLength(int startSegment)
			{
				double length = 0;
				for (int iSegments = startSegment; iSegments < BaseSegments.Count; iSegments++)
				{
					length += BaseSegments[iSegments].ConnectedCurve.GetLength();
				}

				return length;
			}

			[NotNull]
			public Box GetExtent()
			{
				Box allBox = null;
				foreach (ConnectedSegmentsSubpart part in BaseSegments)
				{
					WKSEnvelope env = part.ConnectedCurve.GetWksEnvelope();
					var box = new Box(new Pnt2D(env.XMin, env.YMin),
					                  new Pnt2D(env.XMax, env.YMax));
					if (allBox == null)
					{
						allBox = box;
					}
					else
					{
						allBox.Include(box);
					}
				}

				Assert.NotNull(allBox);
				return allBox;
			}

			internal void RecalculateConnectedCurves()
			{
				var recalceds = new List<ConnectedSegmentsSubpart>(BaseSegments.Count);

				foreach (ConnectedSegmentsSubpart linePart in BaseSegments)
				{
					double min = double.MaxValue;
					double max = double.MinValue;
					foreach (SegmentParts parts in linePart.SegmentNeighbors.Values)
					{
						foreach (SegmentPart part in parts)
						{
							min = Math.Min(part.FullMin, min);
							max = Math.Max(part.FullMax, max);
						}
					}

					var newLinePart = new SegmentsSubpart(
						linePart.BaseFeature, linePart.TableIndex, linePart.BaseSegments,
						linePart.PartIndex, linePart.FullStartFraction, linePart.FullEndFraction);
					var recalced = new ConnectedSegmentsSubpart(
						newLinePart, linePart.SegmentNeighbors,
						new SubClosedCurve(linePart.BaseSegments, linePart.PartIndex, min, max));
					recalceds.Add(recalced);
				}

				BaseSegments.Clear();
				foreach (ConnectedSegmentsSubpart recalced in recalceds)
				{
					BaseSegments.Add(recalced);
				}
			}
		}

		protected class FeaturePoint
		{
			public FeaturePoint([NotNull] IReadOnlyFeature feature, int tableIndex, int part,
			                    double fullFraction)
			{
				Feature = feature;
				TableIndex = tableIndex;
				Part = part;
				FullFraction = fullFraction;
			}

			[NotNull]
			public IReadOnlyFeature Feature { get; }

			public int TableIndex { get; }

			public int Part { get; }

			public double FullFraction { get; }

			public override string ToString()
			{
				return $"{Feature.OID} at {Part}/{FullFraction}";
			}
		}

		private class NeighborPartComparer : IEqualityComparer<SegmentPartWithNeighbor>
		{
			public bool Equals(SegmentPartWithNeighbor x, SegmentPartWithNeighbor y)
			{
				if (x == y)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				return x.NeighborFeature.OID == y.NeighborFeature.OID
				       && x.NeighborTableIndex == y.NeighborTableIndex
				       && x.NeighborProxy.PartIndex == y.NeighborProxy.PartIndex;
			}

			public int GetHashCode(SegmentPartWithNeighbor obj)
			{
				return obj.NeighborFeature.OID.GetHashCode() ^
				       37 * obj.NeighborProxy.PartIndex.GetHashCode();
			}
		}

		private class NeighborSegmentComparer : IEqualityComparer<SegmentPartWithNeighbor>
		{
			public bool Equals(SegmentPartWithNeighbor x, SegmentPartWithNeighbor y)
			{
				if (x == y)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				return x.NeighborFeature.OID == y.NeighborFeature.OID
				       && x.NeighborTableIndex == y.NeighborTableIndex
				       && x.NeighborProxy.PartIndex == y.NeighborProxy.PartIndex
				       && x.NeighborProxy.SegmentIndex == y.NeighborProxy.SegmentIndex;
			}

			public int GetHashCode(SegmentPartWithNeighbor obj)
			{
				return obj.NeighborFeature.OID.GetHashCode() ^
				       37 * obj.NeighborProxy.PartIndex.GetHashCode() ^
				       29 * obj.NeighborProxy.SegmentIndex.GetHashCode();
			}
		}

		private class FeaturePointComparer : IEqualityComparer<FeaturePoint>
		{
			public bool Equals(FeaturePoint x, FeaturePoint y)
			{
				if (x == y)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				return x.Feature.OID == y.Feature.OID
				       && x.TableIndex == y.TableIndex
				       && x.Part == y.Part
				       && Math.Abs(x.FullFraction - y.FullFraction) < _epsi;
			}

			public int GetHashCode(FeaturePoint obj)
			{
				return obj.Feature.OID.GetHashCode() ^ 37 * obj.Part.GetHashCode() ^
				       29 * obj.FullFraction.GetHashCode();
			}
		}

		private class Junction : FeaturePoint
		{
			[NotNull]
			public IReadOnlyFeature NeighborFeature { get; }

			public int NeighborTableIndex { get; }

			public int NeighborPart { get; }

			public double NeighborFullFraction { get; }

			public Junction([NotNull] IReadOnlyFeature feature, int tableIndex,
			                int part, double fullFraction,
			                [NotNull] IReadOnlyFeature neighborFeature, int neighborTableIndex,
			                int neighborPart, double neighborFullFraction)
				: base(feature, tableIndex, part, fullFraction)
			{
				NeighborFeature = neighborFeature;
				NeighborTableIndex = neighborTableIndex;
				NeighborPart = neighborPart;
				NeighborFullFraction = neighborFullFraction;
			}

			public override string ToString()
			{
				return
					string.Format(
						"{0} at {1}/{2}-> {3} at {4}/{5}",
						Feature.OID, Part, GetPositionString(FullFraction),
						NeighborFeature.OID, NeighborPart,
						GetPositionString(NeighborFullFraction));
			}

			private string GetPositionString(double fullFraction)
			{
				return string.Format("{0:N3}", fullFraction);
			}
		}

		private class Join
		{
			[NotNull] private readonly ConnectedLines _connected;
			[NotNull] private readonly ContinuationFinder _continuationFinder;

			public Join([NotNull] ConnectedLines connected,
			            [NotNull] ContinuationFinder continuationFinder)
			{
				_connected = connected;
				_continuationFinder = continuationFinder;
			}

			[NotNull]
			public IEnumerable<ConnectedLines> Continue(
				double startLength,
				[NotNull] Dictionary<SegmentRelation, double> minLengths,
				[NotNull] SegmentPairRelation relevantSegment)
			{
				SegmentsSubpart lastSubpart =
					_connected.BaseSegments[_connected.BaseSegments.Count - 1];
				var search = new FeaturePoint(
					lastSubpart.BaseFeature, lastSubpart.TableIndex,
					lastSubpart.PartIndex, lastSubpart.FullEndFraction);

				List<NeighboredSegmentsSubpart> continuations =
					_continuationFinder.GetContinuations(search, _connected.BaseSegments,
					                                     breakOnExcludeFound: true);

				if (continuations == null || continuations.Count == 0)
				{
					var pre = new ConnectedLines(_connected.BaseSegments);
					pre.RelevantSegment = relevantSegment;
					yield return pre;

					yield break;
				}

				ConnectedLines combinedStart = _connected;
				var connectedReturned = false;

				foreach (NeighboredSegmentsSubpart continuation in continuations)
				{
					List<NeighboredSegmentsSubpart> combinedParts =
						ConnectedLines.Combine(combinedStart.BaseSegments, continuation);

					bool complete;
					List<ConnectedSegmentsSubpart> startConnected = GetStartConnected(combinedParts,
						out complete);

					if (! complete)
					{
						if (! connectedReturned)
						{
							var pre = new ConnectedLines(_connected.BaseSegments);
							pre.RelevantSegment = null;
							yield return pre;
							connectedReturned = true;
						}
					}

					if (startConnected == null)
					{
						continue;
					}

					NeighboredSegmentsSubpart rawContinuation =
						combinedParts[combinedParts.Count - 1];
					SubClosedCurve neighboredContinuation =
						rawContinuation.GetSubcurveAt(rawContinuation.FullStartFraction);

					if (neighboredContinuation == null)
					{
						yield return
							new ConnectedLines(startConnected) {RelevantSegment = relevantSegment};
						continue;
					}

					SegmentNeighbors full = rawContinuation.SegmentNeighbors;
					SegmentNeighbors selection =
						full.Select(part => part.FullMax <= neighboredContinuation.EndFullIndex &&
						                    part.FullMin >= neighboredContinuation.StartFullIndex);
					var trimmedContinuation =
						new ConnectedSegmentsSubpart(rawContinuation, selection,
						                             neighboredContinuation);

					if (AreReverse(startConnected[startConnected.Count - 1], trimmedContinuation))
					{
						if (! connectedReturned)
						{
							var pre = new ConnectedLines(_connected.BaseSegments);
							pre.RelevantSegment = null;
							yield return pre;
							connectedReturned = true;
						}

						continue;
					}

					var connected = new ConnectedLines(startConnected);
					connected.BaseSegments.Add(trimmedContinuation);

					var relevantRelationCanditates = new List<SegmentRelation>(minLengths.Keys);

					var trimmedParts = new List<SegmentPart>();
					foreach (
						SegmentParts segmentNeighbor in trimmedContinuation.SegmentNeighbors.Values)
					{
						trimmedParts.AddRange(segmentNeighbor);
					}

					SegmentPairRelation trimmedRelation = GetRelevantSegment(
						relevantRelationCanditates, trimmedParts);
					SegmentPairRelation combinedActivRelation =
						GetRelevantRelation(relevantRelationCanditates, trimmedRelation,
						                    relevantSegment);

					if (neighboredContinuation.EndFullIndex -
					    neighboredContinuation.StartFullIndex <
					    trimmedContinuation.GetSegmentCount())
					{
						connected.RelevantSegment = combinedActivRelation;
						yield return connected;
					}
					else
					{
						double addLength = trimmedContinuation.ConnectedCurve.GetLength();
						double sumLength = startLength + addLength;

						Dictionary<SegmentRelation, double> combinedMinLengths = GetMinLengths(
							new[] {trimmedContinuation}, relevantRelationCanditates);

						double combinedMinLength = 0;
						foreach (double minLength in combinedMinLengths.Values)
						{
							combinedMinLength = Math.Max(minLength, combinedMinLength);
						}

						if (sumLength > combinedMinLength)
						{
							connected.RelevantSegment = combinedActivRelation;
							yield return connected;
						}
						else
						{
							var join = new Join(connected, _continuationFinder);
							foreach (ConnectedLines continued in
							         join.Continue(sumLength, combinedMinLengths,
							                       combinedActivRelation))
							{
								yield return continued;
							}
						}
					}
				}
			}

			private static bool AreReverse([NotNull] SegmentsSubpart end,
			                               [NotNull] SegmentsSubpart continuation)
			{
				double dirContination;
				if (continuation.FullStartFraction < continuation.FullEndFraction)
				{
					SegmentProxy x = continuation.BaseSegments.GetSegment(
						continuation.PartIndex,
						continuation.FullStartFraction);
					dirContination = x.GetDirectionAt(0);
				}
				else
				{
					SegmentProxy x = continuation.BaseSegments.GetSegment(
						continuation.PartIndex,
						continuation.FullStartFraction - 1);
					dirContination = x.GetDirectionAt(1) - Math.PI;
				}

				double dirEnd;
				if (end.FullStartFraction < end.FullEndFraction)
				{
					SegmentProxy x = end.BaseSegments.GetSegment(
						end.PartIndex, end.FullEndFraction - 1);
					dirEnd = x.GetDirectionAt(1);
				}
				else
				{
					SegmentProxy x = end.BaseSegments.GetSegment(
						end.PartIndex, end.FullEndFraction);
					dirEnd = x.GetDirectionAt(0) - Math.PI;
				}

				double dDir = Math.Abs(dirContination - dirEnd - Math.PI);
				while (dDir > 4)
				{
					dDir = Math.Abs(dDir - 2 * Math.PI);
				}

				bool reverse = dDir < _epsi;
				return reverse;
			}

			[CanBeNull]
			private static List<ConnectedSegmentsSubpart> GetStartConnected(
				[NotNull] List<NeighboredSegmentsSubpart> parts,
				out bool isComplete)
			{
				var result = new List<ConnectedSegmentsSubpart>();

				for (int partIndex = parts.Count - 2; partIndex >= 0; partIndex--)
				{
					NeighboredSegmentsSubpart baseSegments = parts[partIndex];

					IList<SubClosedCurve> connectedParts;
					GetSubcurves(baseSegments.BaseSegments, baseSegments.SegmentNeighbors.Values, 0,
					             0, false, out connectedParts, out IList<SubClosedCurve> _);

					SubClosedCurve endPart = null;
					foreach (SubClosedCurve endPartCandidate in connectedParts)
					{
						if (Math.Abs(endPartCandidate.StartFullIndex -
						             baseSegments.FullEndFraction) <
						    _epsi ||
						    Math.Abs(endPartCandidate.EndFullIndex - baseSegments.FullEndFraction) <
						    _epsi)
						{
							endPart = endPartCandidate;
							break;
						}
					}

					if (endPart == null)
					{
						if (partIndex == parts.Count - 2)
						{
							isComplete = false;
							return null;
						}

						isComplete = false;
						return null;
					}

					if (connectedParts.Count > 1)
					{
						SegmentNeighbors full = baseSegments.SegmentNeighbors;
						SegmentNeighbors selection =
							full.Select(part => part.FullMax <= endPart.EndFullIndex &&
							                    part.FullMin >= endPart.StartFullIndex);

						isComplete = false;
						return null;
					}

					result.Add(new ConnectedSegmentsSubpart(baseSegments,
					                                        baseSegments.SegmentNeighbors,
					                                        endPart));

					if (endPart.EndFullIndex < baseSegments.FullMaxFraction ||
					    endPart.StartFullIndex > baseSegments.FullMinFraction)
					{
						if (partIndex > 0)
						{
							isComplete = false;
							return null;
						}
					}
				}

				isComplete = true;
				result.Reverse();
				return result;
			}
		}

		private sealed class TopoNeighborhoodFinder : NeighborhoodFinder
		{
			private readonly int _tableIndex;
			private readonly int _neighborTableIndex;
			[NotNull] private readonly IList<FeaturePoint> _junctions;
			private readonly double _junctionCoincidenceSquare;
			private readonly ConnectionMode _connectionMode;
			private readonly bool _searchJunctions;

			public TopoNeighborhoodFinder(
				[NotNull] IFeatureRowsDistance distanceProvider,
				[NotNull] IReadOnlyFeature feature, int tableIndex,
				[CanBeNull] IReadOnlyFeature neighbor, int neighborTableIndex,
				[NotNull] IList<FeaturePoint> junctions,
				double junctionCoincidenceSquare,
				ConnectionMode connectionMode, bool searchJunctions)
				: base(distanceProvider, feature, tableIndex, neighbor, neighborTableIndex)
			{
				_tableIndex = tableIndex;
				_neighborTableIndex = neighborTableIndex;
				_junctions = junctions;
				_junctionCoincidenceSquare = junctionCoincidenceSquare;
				_connectionMode = connectionMode;
				_searchJunctions = searchJunctions;
			}

			protected override IList<SegmentPart> GetSegmentParts(SegmentProxy seg0,
				SegmentProxy neighborSeg,
				IList<double[]> seg0Limits,
				bool coincident)
			{
				var result = new List<SegmentPart>(seg0Limits.Count);

				foreach (double[] limit in seg0Limits)
				{
					double min = limit[0];
					double max = limit[1];

					if (max < 0)
					{
						continue;
					}

					if (min > 1)
					{
						continue;
					}

					// TODO? : Add Near / FeatureNear to SegmentPartWithNeighbor
					var part = new SegmentPartWithNeighbor(
						seg0, min, max, coincident,
						NeighborFeature, _neighborTableIndex, neighborSeg);

					result.Add(part);
				}

				return result;
			}

			protected override bool VerifyContinue(SegmentProxy seg0, SegmentProxy seg1,
			                                       SegmentNeighbors processed1,
			                                       SegmentParts partsOfSeg0, bool coincident)
			{
				return true;
			}

			[NotNull]
			private static IEnumerable<SegmentPart> GetEndPoints(
				[NotNull] ISegmentProxy segment,
				[NotNull] IIndexedSegments featureGeometry)
			{
				if (segment.PartIndex == 0 && segment.SegmentIndex == 0)
				{
					yield return new SegmentPart(segment, 0, 0, false);
				}

				if (segment.PartIndex == featureGeometry.GetPartsCount() - 1 &&
				    segment.SegmentIndex ==
				    featureGeometry.GetPartSegmentCount(segment.PartIndex) - 1)
				{
					yield return new SegmentPart(segment, 1, 1, false);
				}
			}

			[NotNull]
			private static IEnumerable<SegmentPart> GetVertices([NotNull] ISegmentProxy segment)
			{
				yield return new SegmentPart(segment, 0, 0, false);
				yield return new SegmentPart(segment, 1, 1, false);
			}

			private void GetJunctions([NotNull] SegmentProxy seg0,
			                          [NotNull] SegmentProxy seg1,
			                          [NotNull] IList<SegmentPart> seg0Parts)
			{
				const bool as3D = false;
				IEnumerable<SegmentPart> end0Parts;
				if (_connectionMode == ConnectionMode.EndpointOnEndpoint ||
				    _connectionMode == ConnectionMode.EndpointOnVertex)
				{
					end0Parts = GetEndPoints(seg0, FeatureGeometry);
				}
				else if (_connectionMode == ConnectionMode.VertexOnVertex)
				{
					end0Parts = GetVertices(seg0);
				}
				else
				{
					throw new InvalidOperationException("Unhandled Connection mode " +
					                                    _connectionMode);
				}

				foreach (SegmentPart end0Part in end0Parts)
				{
					IPnt seg0Pt = null;

					var found = false;
					foreach (SegmentPart seg0Part in seg0Parts)
					{
						if (seg0Part.MinFraction <= end0Part.MinFraction &&
						    seg0Part.MaxFraction >= end0Part.MaxFraction)
						{
							found = true;
							break;
						}
					}

					if (! found)
					{
						continue;
					}

					double minDistanceSquare = _junctionCoincidenceSquare;
					SegmentPart nearestEnd1Part = null;

					IEnumerable<SegmentPart> end1Parts;
					if (_connectionMode == ConnectionMode.EndpointOnEndpoint)
					{
						end1Parts = GetEndPoints(seg1, NeighborGeometry);
					}
					else if (_connectionMode == ConnectionMode.EndpointOnVertex ||
					         _connectionMode == ConnectionMode.VertexOnVertex)
					{
						end1Parts = GetVertices(seg1);
					}
					else
					{
						throw new InvalidOperationException(
							"Unhandled _connectionMode for endParts: " +
							_connectionMode);
					}

					foreach (SegmentPart end1Part in end1Parts)
					{
						if (seg0Pt == null)
						{
							seg0Pt = seg0.GetPointAt(end0Part.MinFraction, as3D);
						}

						IPnt seg1Pt = seg1.GetPointAt(end1Part.MinFraction, as3D);
						double dx = seg0Pt.X - seg1Pt.X;
						double dy = seg0Pt.Y - seg1Pt.Y;
						double dist2 = dx * dx + dy * dy;

						if (dist2 <= minDistanceSquare)
						{
							minDistanceSquare = dist2;
							nearestEnd1Part = end1Part;
						}
					}

					if (nearestEnd1Part != null)
					{
						var jct =
							new Junction(Feature, _tableIndex, end0Part.PartIndex, end0Part.FullMin,
							             NeighborFeature, _neighborTableIndex,
							             nearestEnd1Part.PartIndex,
							             nearestEnd1Part.FullMin);
						_junctions.Add(jct);
					}
				}
			}

			protected override void NeighborsFound(SegmentProxy seg0,
			                                       SegmentProxy seg1,
			                                       IList<SegmentPart> seg0Parts,
			                                       bool coincident)
			{
				if (_searchJunctions)
				{
					GetJunctions(seg0, seg1, seg0Parts);
				}
			}
		}

		private class SegmentPartWithNeighborEx
		{
			public SegmentPartWithNeighborEx([NotNull] ConnectedSegmentsSubpart part,
			                                 [NotNull] SegmentPartWithNeighbor segmentPart)
			{
				Part = part;
				SegmentPart = segmentPart;
			}

			[NotNull]
			public SegmentPartWithNeighbor SegmentPart { get; }

			[NotNull]
			public ConnectedSegmentsSubpart Part { get; }

			public override string ToString()
			{
				return $"OID {Part.BaseFeature.OID}, {SegmentPart}";
			}

			public bool IsPartConnected(SegmentPartWithNeighborEx candidate,
			                            ContinuationFinder continuationFinder)
			{
				if (Part.BaseFeature.OID == candidate.Part.BaseFeature.OID &&
				    Part.TableIndex == candidate.Part.TableIndex &&
				    Part.PartIndex == candidate.Part.PartIndex)
				{
					if (SegmentPart.FullMin > candidate.SegmentPart.FullMax)
					{
						return false;
					}

					if (SegmentPart.FullMax < candidate.SegmentPart.FullMin)
					{
						return false;
					}

					return true;
				}

				var start = new FeaturePoint(Part.BaseFeature, Part.TableIndex, Part.PartIndex,
				                             SegmentPart.FullMin);
				if (IsConnected(start, candidate, continuationFinder))
				{
					return true;
				}

				var end = new FeaturePoint(Part.BaseFeature, Part.TableIndex, Part.PartIndex,
				                           SegmentPart.FullMax);
				if (IsConnected(end, candidate, continuationFinder))
				{
					return true;
				}

				return false;
			}

			private bool IsConnected(FeaturePoint p, SegmentPartWithNeighborEx candidate,
			                         ContinuationFinder continuationFinder)
			{
				List<NeighboredSegmentsSubpart> continuations = continuationFinder
					.GetContinuations(
						p, new List<SegmentsSubpart>(), false);
				if (continuations == null)
				{
					return false;
				}

				foreach (NeighboredSegmentsSubpart continuation in continuations)
				{
					if (candidate.Part.BaseFeature.OID != continuation.BaseFeature.OID ||
					    candidate.Part.TableIndex != continuation.TableIndex ||
					    candidate.Part.PartIndex != continuation.PartIndex)
					{
						continue;
					}

					if (Math.Abs(candidate.SegmentPart.FullMin - continuation.FullMinFraction) <
					    _epsi)
					{
						return true;
					}

					if (Math.Abs(candidate.SegmentPart.FullMax - continuation.FullMaxFraction) <
					    _epsi)
					{
						return true;
					}
				}

				return false;
			}
		}

		private class SegmentPairRelation
		{
			public SegmentPairRelation(SegmentPartWithNeighbor segment,
			                           SegmentRelation relation)
			{
				Segment = segment;
				Relation = relation;
			}

			[NotNull]
			public SegmentRelation Relation { get; }

			[NotNull]
			public SegmentPartWithNeighbor Segment { get; }

			public override string ToString()
			{
				return $"{Relation.GetType().Name}, {Segment}";
			}
		}

		protected sealed class SegmentPartWithNeighbor : SegmentPart
		{
			public SegmentPartWithNeighbor(
				[NotNull] ISegmentProxy segmentProxy,
				double minFraction, double maxFraction, bool complete,
				[NotNull] IReadOnlyFeature neighborFeature, int neighborTableIndex,
				[NotNull] ISegmentProxy neighborProxy) :
				base(segmentProxy, minFraction, maxFraction, complete)
			{
				NeighborFeature = neighborFeature;
				NeighborTableIndex = neighborTableIndex;
				NeighborProxy = neighborProxy;

				NeighborIsCoincident = complete;
			}

			public bool NeighborIsCoincident { get; }

			public IReadOnlyFeature NeighborFeature { get; }

			public int NeighborTableIndex { get; }

			[NotNull]
			public ISegmentProxy NeighborProxy { get; }

			public bool IsConnected { get; set; }
			public bool IsNotReported { get; set; }
			public int MinRelationIndex { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				AppendToString(sb, shortString: false);
				sb.Append(";");
				return sb.ToString();
			}

			protected override void AppendToString(StringBuilder sb, bool shortString)
			{
				base.AppendToString(sb, shortString);
				sb.Append(
					$" | Nb: OID {NeighborFeature.OID}, P {NeighborProxy.PartIndex}, S {NeighborProxy.SegmentIndex}");
			}

			public RowKey CreateNeighborRowKey()
			{
				return new RowKey(NeighborFeature, NeighborTableIndex);
			}

			public FeaturePoint CreateNeighborFeaturePoint(bool atEnd = false)
			{
				return new FeaturePoint(
					NeighborFeature, NeighborTableIndex, NeighborProxy.PartIndex,
					NeighborProxy.SegmentIndex + (atEnd ? 1 : 0));
			}
		}

		private class SegmentPartWithNeighborComparer : IEqualityComparer<SegmentPart>,
		                                                IComparer<SegmentPart>
		{
			private readonly SegmentPartComparer _baseComparer = new SegmentPartComparer();

			public int Compare(SegmentPart x, SegmentPart y)
			{
				if (x == y)
				{
					return 0;
				}

				int d = _baseComparer.Compare(x, y);
				if (d != 0)
				{
					return d;
				}

				var xN = x as SegmentPartWithNeighbor;
				var yN = y as SegmentPartWithNeighbor;

				if (xN == null)
				{
					if (yN == null)
					{
						return d;
					}

					return 1;
				}

				if (yN == null)
				{
					return -1;
				}

				d = xN.NeighborTableIndex.CompareTo(yN.NeighborTableIndex);
				if (d != 0)
				{
					return d;
				}

				d = xN.NeighborFeature.OID.CompareTo(yN.NeighborFeature.OID);
				if (d != 0)
				{
					return d;
				}

				d = xN.NeighborProxy.PartIndex.CompareTo(yN.NeighborProxy.PartIndex);
				if (d != 0)
				{
					return d;
				}

				d = xN.NeighborProxy.SegmentIndex.CompareTo(yN.NeighborProxy.SegmentIndex);
				return d;
			}

			public bool Equals(SegmentPart x, SegmentPart y)
			{
				return Compare(x, y) == 0;
			}

			public int GetHashCode(SegmentPart obj)
			{
				int code = obj.FullMin.GetHashCode();
				if (obj is SegmentPartWithNeighbor nbObj)
				{
					code = code ^
					       7 * (nbObj.NeighborFeature.OID.GetHashCode() ^
					            7 * nbObj.NeighborProxy.SegmentIndex.GetHashCode());
				}

				return code;
			}
		}
	}
}
