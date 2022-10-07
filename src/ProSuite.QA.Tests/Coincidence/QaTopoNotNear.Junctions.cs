using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using Pnt = ProSuite.Commons.Geom.Pnt;

namespace ProSuite.QA.Tests.Coincidence
{
	partial class QaTopoNotNear
	{
		private void CompleteJunctions()
		{
			foreach (KeyValuePair<RowKey, SegmentNeighbors> pair in ProcessedList)
			{
				RowKey key = pair.Key;
				if (_conflictTables.ContainsKey(key.TableIndex))
				{
					continue;
				}

				// --> junction was not searched
				var thisFeature = (IReadOnlyFeature) key.Row;
				var thisCurve = (IGeometryCollection) thisFeature.Shape;
				IIndexedSegments thisGeom =
					IndexedSegmentUtils.GetIndexedGeometry(thisFeature, false);
				Dictionary<int, SegmentPart> handledParts = new Dictionary<int, SegmentPart>();

				foreach (KeyValuePair<SegmentPart, SegmentParts> segmentPair in pair.Value)
				{
					if (segmentPair.Value.Count == 0)
					{
						continue;
					}

					SegmentPart segment = segmentPair.Key;
					if (handledParts.ContainsKey(segment.PartIndex))
					{
						continue;
					}

					int segmentsCount = thisGeom.GetPartSegmentCount(segment.PartIndex);

					IEnvelope searchBox = thisCurve.Geometry[segment.PartIndex].Envelope;

					double tolerance = UsedJunctionCoincidenceTolerance;
					searchBox.Expand(tolerance, tolerance, asRatio: false);

					Pnt start = thisGeom.GetSegment(segment.PartIndex, 0).GetStart(false);
					IBox startBox = new Box(new Pnt2D(start.X - tolerance, start.Y - tolerance),
					                        new Pnt2D(start.X + tolerance, start.Y + tolerance));

					Pnt end = thisGeom.GetSegment(segment.PartIndex, segmentsCount - 1)
					                  .GetEnd(false);
					IBox endBox = new Box(new Pnt2D(end.X - tolerance, end.Y - tolerance),
					                      new Pnt2D(end.X + tolerance, end.Y + tolerance));

					foreach (KeyValuePair<int, IReadOnlyFeatureClass> topoPair in _topoTables)
					{
						int neighborTableIdx = topoPair.Key;
						ISpatialFilter filter = _topoFilters[neighborTableIdx];
						filter.Geometry = searchBox;
						QueryFilterHelper helper = _topoHelpers[neighborTableIdx];

						foreach (IReadOnlyRow neighborRow in Search(topoPair.Value, filter, helper)
						        )
						{
							if (neighborRow.OID == key.Row.OID &&
							    neighborTableIdx == key.TableIndex)
							{
								continue;
							}

							var neighborFeature = (IReadOnlyFeature) neighborRow;
							IIndexedSegments neighborGeom =
								IndexedSegmentUtils.GetIndexedGeometry(neighborFeature, false);

							int connectedPart;
							int connectedFraction;
							if (IsConnected(neighborGeom, startBox, start,
							                JunctionCoincidenceToleranceSquare,
							                out connectedPart, out connectedFraction))
							{
								var jct =
									new Junction(thisFeature, key.TableIndex, segment.PartIndex, 0,
									             neighborFeature, neighborTableIdx, connectedPart,
									             connectedFraction);
								_junctions.Add(jct);
							}

							if (IsConnected(neighborGeom, endBox, end,
							                JunctionCoincidenceToleranceSquare,
							                out connectedPart, out connectedFraction))
							{
								var jct = new Junction(
									thisFeature, key.TableIndex, segment.PartIndex, segmentsCount,
									neighborFeature, neighborTableIdx, connectedPart,
									connectedFraction);
								_junctions.Add(jct);
							}

							if (ConnectionMode == ConnectionMode.VertexOnVertex)
							{
								for (int iSegment = 1; iSegment < segmentsCount; iSegment++)
								{
									Pnt vertex = thisGeom
									             .GetSegment(segment.PartIndex, iSegment)
									             .GetStart(false);
									IBox vertexBox =
										new Box(
											new Pnt2D(vertex.X - tolerance, vertex.Y - tolerance),
											new Pnt2D(vertex.X + tolerance, vertex.Y + tolerance));

									if (IsConnected(neighborGeom, vertexBox, vertex,
									                JunctionCoincidenceToleranceSquare,
									                out connectedPart, out connectedFraction))
									{
										var jct = new Junction(
											thisFeature, key.TableIndex, segment.PartIndex,
											iSegment,
											neighborFeature, neighborTableIdx, connectedPart,
											connectedFraction);
										_junctions.Add(jct);
									}
								}
							}
						}
					}

					handledParts.Add(segment.PartIndex, segment);
				}
			}
		}

		private bool IsConnected([NotNull] IIndexedSegments geometry,
		                         [NotNull] IBox pntsearchBox, [NotNull] Pnt center,
		                         double toleranceSquare,
		                         out int connectedPart, out int connectedFraction)
		{
			foreach (SegmentProxy segment in geometry.GetSegments(pntsearchBox))
			{
				foreach (bool atStart in GetConnectedCandidateAtStarts(geometry, segment))
				{
					Pnt endPnt = atStart ? segment.GetStart(false) : segment.GetEnd(false);
					double dx = endPnt.X - center.X;
					double dy = endPnt.Y - center.Y;
					double dist2 = dx * dx + dy * dy;
					if (dist2 < toleranceSquare)
					{
						connectedPart = segment.PartIndex;
						connectedFraction = atStart
							                    ? segment.SegmentIndex
							                    : segment.SegmentIndex + 1;
						return true;
					}
				}
			}

			connectedPart = -1;
			connectedFraction = -1;
			return false;
		}

		private IEnumerable<bool> GetConnectedCandidateAtStarts(IIndexedSegments geometry,
		                                                        SegmentProxy segment)
		{
			if (segment.SegmentIndex == 0 ||
			    ConnectionMode != ConnectionMode.EndpointOnEndpoint)
			{
				yield return true;
			}

			if (segment.SegmentIndex + 1 == geometry.GetPartSegmentCount(segment.PartIndex) ||
			    ConnectionMode != ConnectionMode.EndpointOnEndpoint)
			{
				yield return false;
			}
		}

		[NotNull]
		private static Dictionary<FeaturePoint, Dictionary<FeaturePoint, FeaturePoint>>
			GetJunctions([NotNull] List<FeaturePoint> jcts)
		{
			var jctDict = new Dictionary
				<FeaturePoint, Dictionary<FeaturePoint, FeaturePoint>>(
					new FeaturePointComparer());

			foreach (FeaturePoint junctionKey in jcts)
			{
				var junction = junctionKey as Junction;
				if (junction != null)
				{
					var j1 = new FeaturePoint(
						junction.NeighborFeature, junction.NeighborTableIndex,
						junction.NeighborPart, junction.NeighborFullFraction);

					AddJunctions(jctDict, junction, j1);
					AddJunctions(jctDict, j1, junction);
				}
				else
				{
					AddJunctions(jctDict, junctionKey, junctionKey);
				}
			}

			var complete = false;
			while (! complete)
			{
				complete = true;
				foreach (KeyValuePair<FeaturePoint, Dictionary<FeaturePoint, FeaturePoint>>
					         pair in jctDict)
				{
					FeaturePoint jctKey = pair.Key;

					foreach (FeaturePoint nbKey in pair.Value.Keys)
					{
						Dictionary<FeaturePoint, FeaturePoint> nbDict = jctDict[nbKey];
						if (! nbDict.ContainsKey(jctKey))
						{
							nbDict.Add(jctKey, jctKey);
							complete = false;
						}
					}
				}
			}

			return jctDict;
		}

		private static void AddJunctions(
			[NotNull] IDictionary<FeaturePoint, Dictionary<FeaturePoint, FeaturePoint>> jctDict,
			[NotNull] FeaturePoint j0,
			[NotNull] FeaturePoint j1)
		{
			Dictionary<FeaturePoint, FeaturePoint> junctionGroup;
			if (! jctDict.TryGetValue(j0, out junctionGroup))
			{
				junctionGroup =
					new Dictionary<FeaturePoint, FeaturePoint>(
						new FeaturePointComparer());
				jctDict.Add(j0, junctionGroup);
			}

			if (! junctionGroup.ContainsKey(j0))
			{
				junctionGroup.Add(j0, j0);
			}

			if (! junctionGroup.ContainsKey(j1))
			{
				junctionGroup.Add(j1, j1);
			}
		}
	}
}
