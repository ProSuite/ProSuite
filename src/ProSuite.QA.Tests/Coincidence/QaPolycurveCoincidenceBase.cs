using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using IPnt = ProSuite.Commons.Geom.IPnt;
using Pnt = ProSuite.Commons.Geom.Pnt;
using SegmentUtils_ = ProSuite.QA.Container.Geometry.SegmentUtils_;

namespace ProSuite.QA.Tests.Coincidence
{
	public abstract class QaPolycurveCoincidenceBase : ContainerTest
	{
		[NotNull] private readonly IEnvelope _removeBox = new EnvelopeClass();
		[NotNull] private readonly Dictionary<RowKey, SegmentNeighbors> _processedList;
		[NotNull] private readonly IFeatureDistanceProvider _distanceProvider;
		[CanBeNull] private IFeatureDistanceProvider _rightSideDistanceProvider;
		[CanBeNull] private SideDistanceProvider _sideDistanceProvider;

		private readonly bool _is3D;
		private const bool _defaultSumExpressionBasedRowDistances = true;

		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;
		private IEnvelope _allBox;
		private IList<string> _ignoreNeighborConditionsSqlFullMatrix;

		[CanBeNull]
		protected IList<string> IgnoreNeighborConditionsSqlFullMatrix
		{
			get { return _ignoreNeighborConditionsSqlFullMatrix; }
			set
			{
				if (_isIgnoredNeighborConditionsFullMatrixInitialized)
				{
					throw new InvalidOperationException(
						"IgnoredNeighborConditions already evalulated");
				}

				Assert.ArgumentCondition(value == null ||
				                         value.Count == 0 ||
				                         value.Count == 1 ||
				                         value.Count ==
				                         InvolvedTables.Count * InvolvedTables.Count,
				                         "unexpected number of IgnoredNeighborConditionsSql conditions " +
				                         "(must be 0, 1, or # of involved tables * # of involved tables");

				_ignoreNeighborConditionsSqlFullMatrix = value;
			}
		}

		private List<IgnoreRowNeighborCondition> _ignoreNeighborConditionsFullMatrix;
		private bool _isIgnoredNeighborConditionsFullMatrixInitialized;

		[NotNull] private readonly Dictionary<RowKey, HashSet<RowKey>> _handledNeighbors
			= new Dictionary<RowKey, HashSet<RowKey>>(new RowKeyComparer());

		private readonly bool _usesConstantNearDistance;

		#region Constructors

		/// <summary>
		/// Find all line-parts in feature classes, that are not near any other line
		/// </summary>
		/// <param name="featureClasses">polygon or polyline featureclasses</param>
		/// <param name="searchDistance">overall maximum allowed distance in (x,y)-units</param>
		/// <param name="nearDistanceProvider">feature dependent maximum allowed distance in (x,y)-units</param>
		/// <param name="is3D">if true, use z coordinates</param>
		/// <remarks>All feature classes must have the same spatial reference</remarks>
		protected QaPolycurveCoincidenceBase(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses,
			double searchDistance,
			[NotNull] IFeatureDistanceProvider nearDistanceProvider,
			bool is3D)
			: base(CastToTables(featureClasses))
		{
			_is3D = is3D;

			SearchDistance = searchDistance;
			_usesConstantNearDistance = nearDistanceProvider is ConstantDistanceProvider;

			_distanceProvider = nearDistanceProvider;
			_processedList = new Dictionary<RowKey, SegmentNeighbors>(new RowKeyComparer());

			KeepRows = true;

			//SumExpressionBasedRowDistances = _defaultSumExpressionBasedRowDistances;
		}

		#endregion

		public bool Is3D => _is3D;

		// TODO allow control via test parameter (--> extended desc. also for constant near tolerance)
		protected bool UsesConstantNearTolerance => _usesConstantNearDistance;

		protected IFeatureDistanceProvider NearDistanceProvider
			=> _sideDistanceProvider ?? _distanceProvider;

		protected IFeatureDistanceProvider RightSideDistanceProvider
		{
			get { return _rightSideDistanceProvider; }
			set
			{
				_rightSideDistanceProvider = value;
				_sideDistanceProvider = value == null
					                        ? null
					                        : new SideDistanceProvider(
						                        _distanceProvider,
						                        value);
			}
		}

		[NotNull]
		protected Dictionary<RowKey, SegmentNeighbors> ProcessedList => _processedList;

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			IList<IFeatureClassFilter> filters = _filter;
			if (filters == null)
			{
				InitFilter();
				filters = Assert.NotNull(_filter, "_filter");
			}

			// iterating over all needed tables
			bool bSkip = IgnoreUndirected;

			SegmentNeighbors processed0;
			var key = new RowKey(row, tableIndex);
			if (! _processedList.TryGetValue(key, out processed0))
			{
				processed0 = new SegmentNeighbors(new SegmentPartComparer());
				_processedList.Add(key, processed0);
			}

			var rowEquatable = new RowKey(row, tableIndex);
			HashSet<RowKey> handledNeighbors;
			if (! _handledNeighbors.TryGetValue(rowEquatable, out handledNeighbors))
			{
				handledNeighbors = new HashSet<RowKey>(new RowKeyComparer());
				_handledNeighbors.Add(rowEquatable, handledNeighbors);
			}

			IGeometry geom0 = ((IReadOnlyFeature) row).Shape;

			IEnvelope box0 = geom0.Envelope;
			box0.Expand(SearchDistance, SearchDistance, false);

			IFeatureRowsDistance rowsDistance =
				NearDistanceProvider.GetRowsDistance(row, tableIndex);

			var errorCount = 0;

			int involvedTableIndex = -1;
			foreach (IReadOnlyTable table in InvolvedTables)
			{
				var fcNeighbor = (IReadOnlyFeatureClass) table;
				involvedTableIndex++;
				QueryFilterHelper helper = _helper[involvedTableIndex];
				helper.MinimumOID = -1;

				if (row.Table == fcNeighbor)
				{
					bSkip = false;

					if (IgnoreUndirected)
					{
						helper.MinimumOID = row.OID;
					}
				}

				if (bSkip)
				{
					continue;
				}

				IFeatureClassFilter filter = filters[involvedTableIndex];
				filter.FilterGeometry = box0;

				foreach (IReadOnlyRow neighborRow in Search(fcNeighbor, filter, helper))
				{
					double maxNear = rowsDistance.GetAddedDistance(neighborRow,
						involvedTableIndex);

					if (maxNear <= 0)
					{
						continue;
					}

					var neighborFeature = (IReadOnlyFeature) neighborRow;

					if (IgnoreNeighbor(row, tableIndex, neighborRow, involvedTableIndex))
					{
						continue;
					}

					var neighborKey = new RowKey(neighborFeature, involvedTableIndex);

					if (! handledNeighbors.Add(neighborKey))
					{
						continue;
					}

					if (row.Table == fcNeighbor && fcNeighbor.HasOID &&
					    row.OID == neighborFeature.OID)
					{
						NeighborhoodFinder finder = GetNeighborhoodFinder(
							rowsDistance, (IReadOnlyFeature) row, tableIndex, null, 0);
						errorCount += FindSelfNeighborhood(finder, tableIndex, processed0, maxNear);
					}
					else
					{
						SegmentNeighbors processed1;
						var nbKey = new RowKey(neighborFeature, involvedTableIndex);
						if (_processedList.TryGetValue(nbKey, out processed1) == false)
						{
							processed1 = new SegmentNeighbors(new SegmentPartComparer());
							_processedList.Add(nbKey, processed1);
						}

						NeighborhoodFinder finder =
							GetNeighborhoodFinder(
								rowsDistance, (IReadOnlyFeature) row, tableIndex,
								neighborFeature, involvedTableIndex);

						errorCount += FindNeighborhood(finder, tableIndex,
						                               processed0,
						                               involvedTableIndex,
						                               processed1, maxNear);
					}
				}
			}

			return errorCount;
		}

		protected abstract NeighborhoodFinder GetNeighborhoodFinder(
			[NotNull] IFeatureRowsDistance distanceProvider,
			[NotNull] IReadOnlyFeature feature, int rowTableIndex,
			[NotNull] IReadOnlyFeature neighbor, int neighborTableIndex);

		private bool IgnoreNeighbor([NotNull] IReadOnlyRow row, int rowTableIndex,
		                            [NotNull] IReadOnlyRow neighbor, int neighborTableIndex)
		{
			EnsureIgnoreNeighborConditionsFullMatrixInitialized();

			if (_ignoreNeighborConditionsFullMatrix == null ||
			    _ignoreNeighborConditionsFullMatrix.Count == 0)
			{
				return false;
			}

			IgnoreRowNeighborCondition condition;
			if (_ignoreNeighborConditionsFullMatrix.Count == 1)
			{
				condition = _ignoreNeighborConditionsFullMatrix[0];
			}
			else
			{
				int index = rowTableIndex * InvolvedTables.Count + neighborTableIndex;
				condition = _ignoreNeighborConditionsFullMatrix[index];
			}

			return condition.IsFulfilled(row, rowTableIndex,
			                             neighbor, neighborTableIndex);
		}

		private void EnsureIgnoreNeighborConditionsFullMatrixInitialized()
		{
			if (_isIgnoredNeighborConditionsFullMatrixInitialized)
			{
				return;
			}

			if (_ignoreNeighborConditionsSqlFullMatrix != null)
			{
				_ignoreNeighborConditionsFullMatrix =
					new List<IgnoreRowNeighborCondition>(
						_ignoreNeighborConditionsSqlFullMatrix.Count);

				bool caseSensitiv = GetSqlCaseSensitivity();

				foreach (string ignoreNeighborCondition in _ignoreNeighborConditionsSqlFullMatrix)
				{
					_ignoreNeighborConditionsFullMatrix.Add(
						new IgnoreRowNeighborCondition(ignoreNeighborCondition,
						                               caseSensitiv,
						                               IsDirected));
				}
			}

			_isIgnoredNeighborConditionsFullMatrixInitialized = true;
		}

		protected abstract bool IsDirected { get; }

		protected int FindNeighborhood([NotNull] NeighborhoodFinder neighborhoodFinder,
		                               int tableIndex,
		                               [NotNull] SegmentNeighbors processed0,
		                               int neighborTableIndex,
		                               [NotNull] SegmentNeighbors processed1,
		                               double maxNear)
		{
			neighborhoodFinder.FindNeighborhood(processed0, processed1, _is3D, _allBox);

			return Check(neighborhoodFinder.Feature, tableIndex, processed0,
			             neighborhoodFinder.NeighborFeature, neighborTableIndex, processed1,
			             maxNear);
		}

		protected static void TryAssignComplete([NotNull] SegmentProxy seg1,
		                                        [NotNull] SegmentNeighbors processed1,
		                                        [NotNull] SegmentParts partsOfSeg0)
		{
			foreach (SegmentPart segmentPart in partsOfSeg0)
			{
				if (! segmentPart.Complete)
				{
					continue;
				}

				var poly1 = new SegmentPart(seg1, 0, 1, true);

				if (! processed1.ContainsKey(poly1))
				{
					var list = new SegmentParts { poly1 };

					list.IsComplete = true;
					processed1.Add(poly1, list);
				}

				break;
			}
		}

		protected int FindSelfNeighborhood([NotNull] NeighborhoodFinder finder,
		                                   int tableIndex,
		                                   [NotNull] SegmentNeighbors processedSegments,
		                                   double maxNear)
		{
			// NeighborhoodFinder finder = new NeighborhoodFinder(feature, null, _allBox, _is3D);
			finder.FindSelfNeighborHood(processedSegments, _is3D, _allBox);

			var errorCount = 0;
			errorCount += Check(finder.Feature, tableIndex, processedSegments,
			                    finder.Feature, tableIndex, processedSegments,
			                    maxNear);

			return errorCount;
		}

		private static void GetMinMax([NotNull] SegmentProxy seg0,
		                              [NotNull] IIndexedSegments segments,
		                              double searchDistanceSquared,
		                              bool partIsClosed,
		                              int partSegmentCount,
		                              bool as3D,
		                              out double min, out double max)
		{
			int partIndex = seg0.PartIndex;
			int segIndex = seg0.SegmentIndex;

			Pnt start = seg0.GetStart(as3D);

			min = -2;
			int nbIndex0 = segIndex;
			do
			{
				int nbIndex = nbIndex0 - 1;
				if (nbIndex < 0 && partIsClosed)
				{
					nbIndex = partSegmentCount - 1;
				}

				nbIndex0 = nbIndex;

				if (nbIndex0 >= 0 && nbIndex0 != segIndex)
				{
					SegmentProxy neighbor = segments.GetSegment(partIndex, nbIndex0);
					IList<double[]> limits;
					SegmentUtils_.CutCurveCircle(neighbor, start, searchDistanceSquared, as3D,
					                             out limits);

					double limit1 = 0;
					foreach (double[] limit in limits)
					{
						if (limit[1] < 1)
						{
							continue;
						}

						limit1 = limit[1];
						if (limit[0] > 0)
						{
							min = nbIndex + limit[0];
						}
					}

					if (limit1 < 1)
					{
						min = nbIndex + 1;
					}
				}
			} while (min < 0 && nbIndex0 >= 0 && nbIndex0 != segIndex);

			max = partSegmentCount;
			nbIndex0 = segIndex;
			Pnt end = seg0.GetEnd(as3D);
			do
			{
				int nbIndex = nbIndex0 + 1;
				if (nbIndex >= partSegmentCount && partIsClosed)
				{
					nbIndex = 0;
				}

				nbIndex0 = nbIndex;

				if (nbIndex0 >= partSegmentCount || nbIndex0 == segIndex)
				{
					continue;
				}

				SegmentProxy neighbor = segments.GetSegment(partIndex, nbIndex0);
				IList<double[]> limits;
				SegmentUtils_.CutCurveCircle(neighbor, end, searchDistanceSquared, as3D,
				                             out limits);

				double limit0 = 1;
				foreach (double[] limit in limits)
				{
					if (limit[0] > 0)
					{
						continue;
					}

					limit0 = limit[0];
					if (limit[1] < 1)
					{
						max = nbIndex + limit[1];
					}
				}

				if (limit0 > 0)
				{
					max = nbIndex;
				}
			} while (MathUtils.AreEqual(max, partSegmentCount) &&
			         nbIndex0 < partSegmentCount &&
			         nbIndex0 != segIndex);
		}

		protected virtual int Check(
			[NotNull] IReadOnlyFeature feat0, int tableIndex,
			[NotNull] SortedDictionary<SegmentPart, SegmentParts> processed0,
			[NotNull] IReadOnlyFeature feat1, int neighborTableIndex,
			[NotNull] SortedDictionary<SegmentPart, SegmentParts> processed1,
			double near)
		{
			return 0;
		}

		private void InitFilter()
		{
			CopyFilters(out _filter, out _helper);
			foreach (var filter in _filter)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}
		}

		private static double GetTolerance([NotNull] IReadOnlyFeature feature0,
		                                   [NotNull] IReadOnlyFeature feature1)
		{
			// TODO debug why the spatial references on the geometries sometimes become null because of preceding tests
			// TODO get appropriate tolerance for *projected* geometries

			// TODO to this for each feature pair?
			const double defaultTolerance = 0;
			return
				Math.Max(
					GeometryUtils.GetXyTolerance(
						((IReadOnlyFeatureClass) feature0.Table).SpatialReference,
						defaultTolerance),
					GeometryUtils.GetXyTolerance(
						((IReadOnlyFeatureClass) feature1.Table).SpatialReference,
						defaultTolerance));
		}

		private static bool IsDirectNeighbor([NotNull] SegmentProxy neighbor,
		                                     [NotNull] SegmentProxy source, double min,
		                                     double max)
		{
			if (source.PartIndex != neighbor.PartIndex)
			{
				return false;
			}

			int segIndex = neighbor.SegmentIndex;
			if (source.SegmentIndex == segIndex)
			{
				return true;
			}

			if (min > max) // possible if closed part
			{
				if (segIndex > min || segIndex < (int) max)
				{
					return true;
				}
			}
			else
			{
				if (segIndex > min && segIndex < (int) max)
				{
					return true;
				}
			}

			return false;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				_allBox = null;
				_processedList.Clear();
				_handledNeighbors.Clear();
			}

			if (args.AllBox != null)
			{
				if (_allBox == null)
				{
					_allBox = new EnvelopeClass();
				}

				args.AllBox.QueryEnvelope(_allBox);
				_allBox.Expand(SearchDistance, SearchDistance, false);
			}

			var remove = new List<RowKey>();
			foreach (RowKey rowKey in _processedList.Keys)
			{
				((IReadOnlyFeature) rowKey.Row).Shape.QueryEnvelope(_removeBox);
				IEnvelope box = args.CurrentEnvelope;
				if (box == null ||
				    _removeBox.XMax + SearchDistance <= box.XMax &&
				    _removeBox.YMax + SearchDistance <= box.YMax)
				{
					remove.Add(rowKey);
				}
			}

			foreach (RowKey rowKey in remove)
			{
				_processedList.Remove(rowKey);
				_handledNeighbors.Remove(rowKey);
			}

			if (args.State == TileState.Final)
			{
				_processedList.Clear();
				_handledNeighbors.Clear();
			}

			return 0;
		}

		#region Nested types

		protected abstract class NeighborhoodFinder
		{
			private readonly IFeatureRowsDistance _rowsDistance;
			private readonly IReadOnlyFeature _feature;
			private readonly double _featureNear;
			private readonly IReadOnlyFeature _neighbor;
			private readonly double _neighborNear;
			private readonly IIndexedSegments _featureGeom;
			private readonly IIndexedSegments _neighborGeom;
			private readonly double _maxNear;

			protected NeighborhoodFinder(
				[NotNull] IFeatureRowsDistance rowsDistance,
				[NotNull] IReadOnlyFeature feature, int featureTableIndex,
				[CanBeNull] IReadOnlyFeature neighbor, int neighborTableIndex)
			{
				double featureNear = rowsDistance.GetRowDistance();
				double near = neighbor != null
					              ? rowsDistance.GetNearDistance(neighbor, neighborTableIndex)
					              : rowsDistance.GetNearDistance(feature, featureTableIndex);
				_rowsDistance = rowsDistance;
				_feature = feature;
				_featureNear = featureNear;
				_neighbor = neighbor;
				_neighborNear = near;

				_maxNear = featureNear + near;

				_featureGeom = IndexedSegmentUtils.GetIndexedGeometry(feature, false);
				if (neighbor != null)
				{
					_neighborGeom = IndexedSegmentUtils.GetIndexedGeometry(neighbor, false);
				}
			}

			public IReadOnlyFeature Feature => _feature;

			public IReadOnlyFeature NeighborFeature => _neighbor;

			public IIndexedSegments FeatureGeometry => _featureGeom;

			public IIndexedSegments NeighborGeometry => _neighborGeom;

			public void FindNeighborhood([NotNull] SegmentNeighbors processed0,
			                             [NotNull] SegmentNeighbors processed1, bool is3D,
			                             [CanBeNull] IEnvelope allBox)
			{
				double tolerance = GetTolerance(_feature, _neighbor);

				_featureGeom.AllowIndexing = true;
				_neighborGeom.AllowIndexing = true;

				IEnvelope envCommon = _featureGeom.Envelope;
				envCommon.Expand(_maxNear, _maxNear, false);

				IEnvelope env1 = _neighborGeom.Envelope;
				env1.Expand(_maxNear, _maxNear, false);

				envCommon.Intersect(env1);
				if (allBox != null)
				{
					envCommon.Intersect(allBox);
				}

				// https://issuetracker02.eggits.net/browse/TOP-4090
				// The intersection can be empty due to disjoint envelopes
				// (disjoint with respect to resolution, but not with respect to tolerance 
				// -> found by filter but IEnvelope.Intersect is empty)
				if (! envCommon.IsEmpty)
				{
					WKSEnvelope wksEnv;
					envCommon.QueryWKSCoords(out wksEnv);

					IEnumerable<SegmentProxyNeighborhood> neighborhoods;
					Box commonBox = GeomUtils.CreateBox(wksEnv.XMin, wksEnv.YMin,
					                                    wksEnv.XMax, wksEnv.YMax);

					if (! _featureGeom.TryGetSegmentNeighborhoods(
						    _neighborGeom, commonBox, _maxNear,
						    out neighborhoods))
					{
						neighborhoods = Create(_featureGeom, _neighborGeom, commonBox, _maxNear);
					}

					FindNeighborhood(neighborhoods, processed0, processed1,
					                 tolerance, is3D);
				}
			}

			public void FindSelfNeighborHood([NotNull] SegmentNeighbors processedSegments,
			                                 bool is3D, IEnvelope allBox)
			{
				// TODO debug why the spatial references on the geometries sometimes become null because of preceding tests
				// TODO get appropriate tolerance for *projected* geometries
				const double defaultTolerance = 0;
				double tolerance = GeometryUtils.GetXyTolerance(
					((IReadOnlyFeatureClass) _feature.Table).SpatialReference,
					defaultTolerance);

				IBox testAreaBox = null;
				if (allBox != null)
				{
					IEnvelope testAreaEnv = _feature.Extent;
					testAreaEnv.Intersect(allBox);
					testAreaBox = ProxyUtils.CreateBox(testAreaEnv);
				}

				FindSelfNeighborhood(_featureGeom, processedSegments, testAreaBox, tolerance,
				                     is3D);
			}

			private void FindSelfNeighborhood([NotNull] IIndexedSegments geom,
			                                  [NotNull] SegmentNeighbors processedSegments,
			                                  [CanBeNull] IBox testAreaBox,
			                                  double tolerance,
			                                  bool is3D)
			{
				IEnumerable<SegmentProxy> sourceSegments = testAreaBox == null
					                                           ? geom.GetSegments()
					                                           : geom.GetSegments(testAreaBox);

				int lastSourcePart = -1;
				int partSegmentCount = -1;
				var partIsClosed = false;

				double maxNearSquared = _maxNear * _maxNear;
				var roundCap = new RoundCap();

				foreach (SegmentProxy sourceSegment in sourceSegments)
				{
					if (sourceSegment.PartIndex != lastSourcePart)
					{
						int newPartIndex = sourceSegment.PartIndex;

						partIsClosed = geom.IsPartClosed(newPartIndex);
						partSegmentCount = geom.GetPartSegmentCount(newPartIndex);

						lastSourcePart = sourceSegment.PartIndex;
					}

					double min;
					double max;
					GetMinMax(sourceSegment, geom, maxNearSquared, partIsClosed,
					          partSegmentCount, is3D, out min, out max);

					IBox sourceBox = sourceSegment.Extent;
					IBox nearBox = GeomUtils.GetExpanded(sourceBox, _maxNear);

					var sourceHull = new SegmentHull(sourceSegment, 0, roundCap, roundCap);
					IEnumerable<SegmentProxy> neighborSegments = geom.GetSegments(nearBox);

					foreach (SegmentProxy neighborSegment in neighborSegments)
					{
						var neighborSegmentPart = new SegmentPart(neighborSegment, 0, 1, true);
						SegmentParts neighborSegmentParts;
						if (
							! processedSegments.TryGetValue(
								neighborSegmentPart, out neighborSegmentParts))
						{
							neighborSegmentParts = new SegmentParts();
							processedSegments.Add(neighborSegmentPart, neighborSegmentParts);
						}

						if (neighborSegmentParts.IsComplete)
						{
							continue;
						}

						if (IsDirectNeighbor(neighborSegment, sourceSegment, min, max))
						{
							continue;
						}

						IBox neighborBox = neighborSegment.Extent;

						if (! nearBox.Intersects(neighborBox))
						{
							continue;
						}

						var neighborHull =
							new SegmentHull(neighborSegment, _maxNear, roundCap, roundCap);
						bool coincident;
						IEnumerable<double[]> limits = CutCurveHull(neighborHull, sourceHull,
							tolerance, is3D,
							out coincident);

						foreach (double[] limit in limits)
						{
							var nearSelf = false;

							double minFraction = limit[0];
							double maxFraction = limit[1];
							if (neighborSegment.PartIndex == lastSourcePart)
							{
								if (neighborSegment.SegmentIndex == (int) min)
								{
									double tMax = min - (int) min;
									if (tMax <= maxFraction)
									{
										maxFraction = tMax;
										nearSelf = true;
									}
								}

								if (neighborSegment.SegmentIndex == (int) max)
								{
									double tMin = max - (int) max;
									if (tMin >= minFraction)
									{
										minFraction = tMin;
										nearSelf = true;
									}
								}
							}

							if (minFraction >= maxFraction)
							{
								continue;
							}

							var seg = new SegmentPart(neighborSegment, minFraction, maxFraction,
							                          coincident);
							if (neighborSegment.PartIndex == lastSourcePart)
							{
								seg.NearSelf = sourceSegment;
							}

							neighborSegmentParts.Add(seg);

							if (! nearSelf)
							{
								VerifyContinue(sourceSegment, neighborSegment, processedSegments,
								               neighborSegmentParts, false);
							}
						}
					}
				}
			}

			protected virtual void NeighborsFound([NotNull] SegmentProxy sourceSeg,
			                                      [NotNull] SegmentProxy neighborSeg,
			                                      [NotNull] IList<SegmentPart> seg0Parts,
			                                      bool coincident) { }

			protected abstract bool VerifyContinue([NotNull] SegmentProxy seg0,
			                                       [NotNull] SegmentProxy seg1,
			                                       [NotNull] SegmentNeighbors processed1,
			                                       [NotNull] SegmentParts partsOfSeg0,
			                                       bool coincident);

			private void FindNeighborhood(
				[NotNull] IEnumerable<SegmentProxyNeighborhood> neighborsEnumerable,
				[NotNull] SegmentNeighbors processedSource,
				[NotNull] SegmentNeighbors processedNeighbor,
				double tolerance, bool is3D)
			{
				var roundCap = new RoundCap();
				foreach (SegmentProxyNeighborhood pair in neighborsEnumerable)
				{
					SegmentProxy sourceSeg = pair.SegmentProxy;
					var sourceHull = new SegmentHull(sourceSeg, _featureNear, roundCap, roundCap);

					var segPart0 = new SegmentPart(sourceSeg, 0, 1, true);
					SegmentParts partsOfSourceSeg;
					if (processedSource.TryGetValue(segPart0, out partsOfSourceSeg) == false)
					{
						partsOfSourceSeg = new SegmentParts();
						processedSource.Add(segPart0, partsOfSourceSeg);
					}

					if (partsOfSourceSeg.IsComplete)
					{
						continue;
					}

					IBox sourceBox = sourceSeg.Extent;
					IBox nearBox = GeomUtils.GetExpanded(sourceBox, _maxNear);

					foreach (SegmentProxy neighborSeg in pair.Neighbours)
					{
						var neighborHull = new SegmentHull(neighborSeg, _neighborNear, roundCap,
						                                   roundCap);
						IBox neighborBox = neighborSeg.Extent;

						if (! nearBox.Intersects(neighborBox))
						{
							continue;
						}

						NearSegment hullStart;
						NearSegment hullEnd;
						bool coincident;
						IList<double[]> limits = FindNeighborhood(
							sourceHull, neighborHull, is3D, tolerance,
							out hullStart, out hullEnd, out coincident);

						IList<SegmentPart> seg0Parts = GetSegmentParts(
							sourceSeg, neighborSeg, limits,
							coincident);
						partsOfSourceSeg.AddRange(seg0Parts);

						NeighborsFound(sourceSeg, neighborSeg, seg0Parts, coincident);

						if (VerifyContinue(sourceSeg, neighborSeg, processedNeighbor,
						                   partsOfSourceSeg,
						                   coincident))
						{
							continue;
						}

						if (coincident)
						{
							// TODO revise this seems to cause stack overflows
							//bool isInvers = hullStart != NearSegment.NearStart;
							//HandleNeighbors(geom0, seg0, processed0, geom1, seg1, processed1, tolerance,
							//                isInvers);
						}

						partsOfSourceSeg.IsComplete = true;
						break;
					}
				}
			}

			protected virtual IList<SegmentPart> GetSegmentParts(
				[NotNull] SegmentProxy seg0,
				[NotNull] SegmentProxy neighborSeg,
				[NotNull] IList<double[]> seg0Limits,
				bool coincident)
			{
				var addParts = new List<SegmentPart>(seg0Limits.Count);
				foreach (double[] limit in seg0Limits)
				{
					addParts.Add(new SegmentPart(seg0, limit[0], limit[1], coincident));
				}

				return addParts;
			}

			protected static IList<double[]> FindNeighborhood(
				[NotNull] SegmentHull seg0,
				[NotNull] SegmentHull seg1, bool is3D, double tolerance,
				out NearSegment hullStart, out NearSegment hullEnd, out bool coincident)
			{
				IList<double[]> limits;
				SegmentPair segmentPair = SegmentPair.Create(seg0, seg1, is3D);

				segmentPair.CutCurveHull(tolerance,
				                         out limits, out hullStart, out hullEnd, out coincident);

				return limits;
			}

			[NotNull]
			private IEnumerable<double[]> CutCurveHull([NotNull] SegmentHull curve0,
			                                           [NotNull] SegmentHull curve1,
			                                           double tolerance,
			                                           bool is3D,
			                                           out bool coincident)
			{
				IList<double[]> limits;
				NearSegment hullStart;
				NearSegment hullEnd;
				SegmentPair segmentPair = SegmentPair.Create(curve0, curve1, is3D);
				segmentPair.CutCurveHull(tolerance,
				                         out limits, out hullStart, out hullEnd, out coincident);

				return limits;
			}

			[NotNull]
			private static IEnumerable<SegmentProxyNeighborhood> Create(
				[NotNull] IIndexedSegments sourceGeom,
				[NotNull] IIndexedSegments neighborGeom,
				[NotNull] IBox commonBox,
				double searchDistance)
			{
				foreach (SegmentProxy sourceSeg in sourceGeom.GetSegments(commonBox))
				{
					IBox sourceBox = sourceSeg.Extent;
					IBox nearBox = GeomUtils.GetExpanded(sourceBox, searchDistance);
					IEnumerable<SegmentProxy> neighborSegEnum = neighborGeom.GetSegments(nearBox);
					yield return
						new SegmentProxyNeighborhood
						{
							SegmentProxy = sourceSeg,
							Neighbours = neighborSegEnum
						};
				}
			}
		}

		protected class RowKey
		{
			private readonly IReadOnlyRow _row;
			private readonly int _tableIndex;

			public RowKey([NotNull] IReadOnlyRow row, int tableIndex)
			{
				_row = row;
				_tableIndex = tableIndex;
			}

			[NotNull]
			public IReadOnlyRow Row => _row;

			public int TableIndex => _tableIndex;

			public override string ToString()
			{
				return string.Format("OID:{0}; T:{1}", Row.OID, TableIndex);
			}
		}

		protected class RowKeyComparer : IEqualityComparer<RowKey>
		{
			public bool Equals(RowKey x, RowKey y)
			{
				return x.TableIndex == y.TableIndex && x.Row.OID == y.Row.OID;
			}

			public int GetHashCode(RowKey obj)
			{
				return obj.Row.OID.GetHashCode() ^ 37 * obj.TableIndex.GetHashCode();
			}
		}

		protected class SegmentNeighbors : SortedDictionary<SegmentPart, SegmentParts>
		{
			public SegmentNeighbors(IComparer<SegmentPart> comparer) : base(comparer) { }

			public override string ToString()
			{
				return string.Format("SegmentNeighbors: {0}", Count);
			}

			public SegmentNeighbors Select([NotNull] Func<SegmentPart, bool> segmentSelect)
			{
				var selection = new SegmentNeighbors(Comparer);
				foreach (KeyValuePair<SegmentPart, SegmentParts> pair in this)
				{
					SegmentParts parts = pair.Value;

					SegmentParts selectedParts = null;
					foreach (SegmentPart part in parts)
					{
						if (! segmentSelect(part))
						{
							continue;
						}

						if (selectedParts == null)
						{
							selectedParts = new SegmentParts();
						}

						selectedParts.Add(part);
					}

					if (selectedParts != null)
					{
						selection.Add(pair.Key, selectedParts);
					}
				}

				return selection;
			}
		}

		protected class SegmentAdapter
		{
			public bool Is3D { get; protected set; }

			protected SegmentPair RecalcPart([NotNull] SegmentPart segmentPart,
			                                 [NotNull] SegmentHull hull,
			                                 [NotNull] SegmentHull neighborhull)
			{
				SegmentPair segPair = SegmentPair.Create(hull, neighborhull, Is3D);

				IList<double[]> limits;
				NearSegment startNear;
				NearSegment endNear;
				bool coincident;
				if (segPair.CutCurveHull(0, out limits, out startNear, out endNear,
				                         out coincident)
				    && limits.Count > 0
				    && limits[0][0] < 1 && limits[0][1] > 0)
				{
					segmentPart.MinFraction = limits[0][0];
					segmentPart.MaxFraction = limits[0][1];
				}
				else
				{
					segmentPart.MinFraction = segmentPart.MaxFraction;
				}

				return segPair;
			}

			protected void Drop0LengthParts([NotNull] IEnumerable<SegmentParts> enumSegmentParts)
			{
				foreach (SegmentParts segmentParts in enumSegmentParts)
				{
					Drop0LengthParts(segmentParts);
				}
			}

			protected void Drop0LengthParts([NotNull] SegmentParts segmentParts)
			{
				var keep = new List<SegmentPart>();
				foreach (SegmentPart segmentPart in segmentParts)
				{
					if (segmentPart.FullMin < segmentPart.FullMax)
					{
						keep.Add(segmentPart);
					}
				}

				if (keep.Count < segmentParts.Count)
				{
					segmentParts.Clear();
					segmentParts.AddRange(keep);
				}
			}
		}

		protected class SegmentParts : List<SegmentPart>
		{
			public bool IsComplete;

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.Append($"#:{Count} :");
				for (var i = 0; i < 3 && i < Count; i++)
				{
					this[i].AppendTo(sb, true);
					sb.Append(";");
				}

				if (Count > 4)
				{
					sb.Append("...;");
				}

				if (Count >= 4)
				{
					sb.Append($"{this[Count - 1]}; ");
				}

				return sb.ToString();
			}
		}

		protected class ConnectedSegmentParts : SegmentParts
		{
			private readonly IIndexedSegments _geom;

			public ConnectedSegmentParts([NotNull] IIndexedSegments geom,
			                             [NotNull] SegmentPart startPart)
			{
				_geom = geom;
				Add(startPart);
				StartPart = startPart;
				EndPart = startPart;
			}

			public ConnectedSegmentParts([NotNull] ConnectedSegmentParts atStart,
			                             [NotNull] ConnectedSegmentParts atEnd)
			{
				Assert.AreEqual(atStart.BaseGeometry, atEnd.BaseGeometry, "Geometry differs");
				Assert.AreEqual(atStart.PartIndex, atEnd.PartIndex, "Part index differs");

				_geom = atStart.BaseGeometry;
				AddRange(atEnd);
				AddRange(atStart);
				StartPart = atEnd[0];
				EndPart = atStart[atStart.Count - 1];
			}

			public IIndexedSegments BaseGeometry => _geom;

			public int PartIndex => this[0].PartIndex;

			public SegmentPart StartPart { get; private set; }
			public SegmentPart EndPart { get; private set; }

			public double StartFullIndex => StartPart.FullMin;

			public double EndFullIndex => EndPart.FullMax;

			public void AddPart(SegmentPart part)
			{
				Assert.True(part.PartIndex == PartIndex, "Invalid partindex");

				Add(part);
				if (part.FullMin < StartFullIndex)
				{
					StartPart = part;
				}

				if (part.FullMax > EndFullIndex)
				{
					EndPart = part;
				}
			}
		}

		protected class SubClosedCurve
		{
			[NotNull] private readonly Subcurve _curve1;
			[CanBeNull] private readonly Subcurve _curve2;

			public SubClosedCurve([NotNull] Subcurve subcurve)
			{
				_curve1 = subcurve;
			}

			public SubClosedCurve([NotNull] Subcurve atStart, [NotNull] Subcurve atEnd)
			{
				Assert.True(atStart.BaseGeometry == atEnd.BaseGeometry, "base geometries differ");
				Assert.True(atStart.PartIndex == atEnd.PartIndex, "part indices differ");
				Assert.True(atStart.BaseGeometry.IsPartClosed(atStart.PartIndex), "no closed part");
				Assert.True(MathUtils.AreEqual(atStart.StartFullIndex, 0), "Invalid start part");
				Assert.True(MathUtils.AreEqual(atEnd.EndFullIndex,
				                               atEnd.BaseGeometry.GetPartSegmentCount(
					                               atEnd.PartIndex)),
				            "Invalid end part");

				_curve2 = atStart;
				_curve1 = atEnd;
			}

			public SubClosedCurve([NotNull] IIndexedSegments baseGeometry,
			                      int partIndex,
			                      double startFullIndex,
			                      double endFullIndex)
			{
				int partSegmentCount = baseGeometry.GetPartSegmentCount(partIndex);

				var startIndex = (int) startFullIndex;
				if (startIndex == partSegmentCount)
				{
					startIndex--;
				}

				var endIndex = (int) endFullIndex;
				if (endIndex == partSegmentCount)
				{
					endIndex--;
				}

				Assert.True(startIndex >= 0, "Invalid start index: {0}", startIndex);
				Assert.True(endIndex >= 0, "Invalid end index: {0}", endIndex);

				Assert.True(startIndex < partSegmentCount,
				            "Invalid start index for part segment count {0}: {1}",
				            partSegmentCount, startIndex);
				Assert.True(endIndex < partSegmentCount,
				            "Invalid end index for part segment count {0}: {1}",
				            partSegmentCount, endIndex);

				if (startFullIndex <= endFullIndex)
				{
					_curve1 = new Subcurve(baseGeometry, partIndex,
					                       startIndex,
					                       startFullIndex - startIndex,
					                       endIndex,
					                       endFullIndex - endIndex);
				}
				else
				{
					_curve1 = new Subcurve(baseGeometry, partIndex,
					                       startIndex,
					                       startFullIndex - startIndex,
					                       partSegmentCount - 1,
					                       1);

					_curve2 = new Subcurve(baseGeometry,
					                       partIndex,
					                       0, 0,
					                       endIndex,
					                       endFullIndex - endIndex);
				}
			}

			[NotNull]
			public IIndexedSegments BaseGeometry => _curve1.BaseGeometry;

			public override string ToString()
			{
				return string.Format("Subcurve {0:N3} {1:N3}", StartFullIndex, EndFullIndex);
			}

			[NotNull]
			public IPolyline GetGeometry()
			{
				IPolyline line = _curve1.GetGeometry();
				if (_curve2 == null)
				{
					return line;
				}

				IPolyline atStart = _curve2.GetGeometry();
				((ISegmentCollection) line).AddSegmentCollection((ISegmentCollection) atStart);
				return line;
			}

			public WKSEnvelope GetWksEnvelope()
			{
				WKSEnvelope box = _curve1.GetWksEnvelope();
				if (_curve2 == null)
				{
					return box;
				}

				WKSEnvelope atStart = _curve2.GetWksEnvelope();

				box.XMin = Math.Min(atStart.XMin, box.XMin);
				box.XMax = Math.Max(atStart.XMax, box.XMax);
				box.YMin = Math.Min(atStart.YMin, box.YMin);
				box.YMax = Math.Max(atStart.YMax, box.YMax);

				return box;
			}

			public double GetLength()
			{
				double length = _curve1.GetLength();
				if (_curve2 != null)
				{
					length += _curve2.GetLength();
				}

				return length;
			}

			public int PartIndex => _curve1.PartIndex;

			public double StartFullIndex => _curve1.StartFullIndex;

			public double EndFullIndex
			{
				get
				{
					if (_curve2 != null)
					{
						return _curve2.EndFullIndex;
					}

					return _curve1.EndFullIndex;
				}
			}

			private double EndCircularFullIndex
			{
				get
				{
					double endCircularIdx;
					if (StartFullIndex <= EndFullIndex)
					{
						endCircularIdx = EndFullIndex;
					}
					else
					{
						int parts = _curve1.BaseGeometry.GetPartSegmentCount(PartIndex);
						endCircularIdx = EndFullIndex + parts;
					}

					return endCircularIdx;
				}
			}

			public bool IsCompleteClosedPart()
			{
				return MathUtils.AreEqual(StartFullIndex, 0) &&
				       _curve1.BaseGeometry.IsPartClosed(PartIndex) &&
				       MathUtils.AreEqual(EndFullIndex,
				                          _curve1.BaseGeometry.GetPartSegmentCount(PartIndex));
			}

			public bool IsWithin([NotNull] SubClosedCurve isWithin)
			{
				if (isWithin.PartIndex != PartIndex)
				{
					return false;
				}

				if (IsCompleteClosedPart())
				{
					// any other subcurve is within
					return true;
				}

				double isWithinEnd = isWithin.EndCircularFullIndex;
				if (StartFullIndex > isWithin.StartFullIndex)
				{
					int parts = _curve1.BaseGeometry.GetPartSegmentCount(PartIndex);
					isWithinEnd += parts;
				}

				bool inside = isWithinEnd <= EndCircularFullIndex;

				return inside;
			}

			public IEnumerable<SubClosedCurve> Difference(double startFullIndex,
			                                              double endFullIndex)
			{
				if (StartFullIndex < startFullIndex)
				{
					yield return
						new SubClosedCurve(BaseGeometry, PartIndex, StartFullIndex,
						                   Math.Min(EndFullIndex, startFullIndex));
				}

				if (EndFullIndex > endFullIndex)
				{
					yield return
						new SubClosedCurve(BaseGeometry, PartIndex,
						                   Math.Max(StartFullIndex, endFullIndex), EndFullIndex);
				}
			}
		}

		protected class Subcurve : IHasPolyline
		{
			private readonly IIndexedSegments _baseGeometry;
			private readonly int _partIndex;
			private readonly int _startSegmentIndex;
			private readonly double _startFraction;
			private int _endSegmentIndex;
			private double _endFraction;

			// private bool _nearSelf;

			private IPnt _startPoint;
			private IPnt _endPoint;

			private IPolyline _polyline;

			public Subcurve([NotNull] IIndexedSegments baseGeometry,
			                int partIndex,
			                int startSegmentIndex, double startFraction,
			                int endSegmentIndex, double endFraction)
			{
				_baseGeometry = baseGeometry;
				_endFraction = endFraction;
				_endSegmentIndex = endSegmentIndex;
				_startFraction = startFraction;
				_startSegmentIndex = startSegmentIndex;
				_partIndex = partIndex;
			}

			[NotNull]
			public IIndexedSegments BaseGeometry => _baseGeometry;

			public int PartIndex => _partIndex;

			public int StartSegmentIndex => _startSegmentIndex;

			public double StartFraction => _startFraction;

			public double StartFullIndex => _startSegmentIndex + _startFraction;

			public int EndSegmentIndex
			{
				get { return _endSegmentIndex; }
				set { _endSegmentIndex = value; }
			}

			public double EndFraction
			{
				get { return _endFraction; }
				set { _endFraction = value; }
			}

			public double EndFullIndex => _endSegmentIndex + _endFraction;

			[NotNull]
			public IPnt EndPoint
			{
				get
				{
					if (_endPoint == null)
					{
						SegmentProxy p = _baseGeometry.GetSegment(_partIndex, _endSegmentIndex);
						_endPoint = p.GetPointAt(_endFraction);
					}

					return _endPoint;
				}
			}

			[NotNull]
			public IPnt GetPreEndPoint()
			{
				IPnt preEnd;
				if (_endSegmentIndex == _startSegmentIndex)
				{
					preEnd = StartPoint;
				}
				else
				{
					SegmentProxy segmentProxy =
						_baseGeometry.GetSegment(_partIndex, _endSegmentIndex);
					preEnd = segmentProxy.GetStart(false);
				}

				return preEnd;
			}

			[NotNull]
			public IPnt StartPoint
			{
				get
				{
					if (_startPoint == null)
					{
						SegmentProxy p = _baseGeometry.GetSegment(_partIndex, _startSegmentIndex);
						_startPoint = p.GetPointAt(_startFraction);
					}

					return _startPoint;
				}
			}

			[NotNull]
			public IPnt GetPostStartPoint()
			{
				IPnt postStart;

				if (_endSegmentIndex == _startSegmentIndex)
				{
					postStart = EndPoint;
				}
				else
				{
					SegmentProxy segmentProxy = _baseGeometry.GetSegment(_partIndex,
						_startSegmentIndex);
					postStart = segmentProxy.GetEnd(false);
				}

				return postStart;
			}

			public double GetLength()
			{
				double length =
					IndexedSegmentUtils.GetLength(
						_baseGeometry, _partIndex, _startSegmentIndex, _startFraction,
						_endSegmentIndex, _endFraction);
				return length;
			}

			public IPolyline Polyline => _polyline ?? (_polyline = GetGeometry());

			public IPolyline GetGeometry()
			{
				IPolyline curve = _baseGeometry.GetSubpart(_partIndex,
				                                           _startSegmentIndex, _startFraction,
				                                           _endSegmentIndex, _endFraction);
				return curve;
			}

			public WKSEnvelope GetWksEnvelope()
			{
				WKSEnvelope envelope = IndexedSegmentUtils.GetEnvelope(
					_baseGeometry, _partIndex, _startSegmentIndex, _startFraction,
					_endSegmentIndex, _endFraction);
				return envelope;
			}

			[NotNull]
			public static IList<Subcurve> GetMissingSubcurves(
				[NotNull] IReadOnlyFeature feature,
				[NotNull] IIndexedSegments geometry,
				[NotNull] SortedDictionary<SegmentPart, SegmentParts> partlyMissing,
				[CanBeNull] IPnt maxProcessed)
			{
				var result = new List<Subcurve>();

				int currentPartIndex = -1;
				int currentSegmentIndex = -2;
				int startSegmentIndex = -1;
				double startFraction = 0;
				double currentFraction = 0;
				foreach (KeyValuePair<SegmentPart, SegmentParts> pair in partlyMissing)
				{
					SegmentPart segmentPart = pair.Key;
					SegmentParts neighbors = pair.Value;

					if (maxProcessed != null && segmentPart.SegmentProxy != null)
					{
						IPnt maxSegment = segmentPart.SegmentProxy.Max;
						if (maxProcessed.X <= maxSegment.X || maxProcessed.Y <= maxSegment.Y)
						{
							// do not handle segments that are not fully processed
							continue;
						}
					}

					if (segmentPart.PartIndex != currentPartIndex ||
					    segmentPart.SegmentIndex != currentSegmentIndex + 1)
					{
						TryAddSegment(result, feature, geometry, currentPartIndex,
						              startSegmentIndex,
						              startFraction,
						              currentSegmentIndex, currentFraction);

						startSegmentIndex = segmentPart.SegmentIndex;
						startFraction = 0;
					}

					currentPartIndex = segmentPart.PartIndex;

					neighbors.Sort(new SegmentPartComparer());

					currentSegmentIndex = segmentPart.SegmentIndex;
					currentFraction = 0;

					foreach (SegmentPart part in neighbors)
					{
						if (part.MinFraction > currentFraction)
						{
							TryAddSegment(result, feature, geometry, currentPartIndex,
							              startSegmentIndex,
							              startFraction,
							              currentSegmentIndex, part.MinFraction);
						}

						currentFraction = Math.Max(currentFraction, part.MaxFraction);
						startFraction = currentFraction;
						startSegmentIndex = currentSegmentIndex;
					}

					currentFraction = 1;
				}

				TryAddSegment(result, feature, geometry, currentPartIndex,
				              startSegmentIndex,
				              startFraction,
				              currentSegmentIndex, currentFraction);

				return result;
			}

			[CanBeNull]
			// ReSharper disable once UnusedMethodReturnValue.Local
			private static Subcurve TryAddSegment([NotNull] ICollection<Subcurve> curveList,
			                                      [NotNull] IReadOnlyFeature feature,
			                                      [NotNull] IIndexedSegments geom,
			                                      int partIndex, int startSegment,
			                                      double startFraction, int endSegment,
			                                      double endFraction)
			{
				if (partIndex < 0)
				{
					return null;
				}

				if (startSegment > endSegment)
				{
					return null;
				}

				if (startSegment == endSegment && startFraction >= endFraction)
				{
					return null;
				}

				var subcurve = new Subcurve(geom, partIndex, startSegment, startFraction,
				                            endSegment, endFraction);
				curveList.Add(subcurve);

				return subcurve;
			}
		}

		protected class IgnoreRowNeighborCondition : RowPairCondition
		{
			private const bool _undefinedConstraintIsFulfilled = true;

			public IgnoreRowNeighborCondition([CanBeNull] string ignoreNeighborCondition,
			                                  bool caseSensitivity,
			                                  bool isDirected) :
				base(ignoreNeighborCondition,
				     isDirected,
				     _undefinedConstraintIsFulfilled,
				     caseSensitivity) { }
		}

		protected interface IFeatureDistanceProvider
		{
			IFeatureRowsDistance GetRowsDistance([NotNull] IReadOnlyRow row1, int tableIndex);
		}

		protected interface IPairDistanceProvider
		{
			IPairRowsDistance GetRowsDistance([NotNull] IReadOnlyRow row1, int tableIndex);
		}

		protected interface IConstantDistanceProvider
		{
			bool TryGetConstantDistance(out double distance);
		}

		protected interface IPairRowsDistance
		{
			double GetAddedDistance([NotNull] IReadOnlyRow neighborRow, int neighborTableIndex);
		}

		protected interface IFeatureRowsDistance : IPairRowsDistance
		{
			double GetRowDistance();

			double GetNearDistance([NotNull] IReadOnlyRow neighborRow, int neighborTableIndex);
		}

		protected interface IAssymetricFeatureRowsDistance : IFeatureRowsDistance
		{
			double GetLeftSideDistance();

			double GetRightSideDistance();

			double GetLeftSideDistance([NotNull] IReadOnlyRow neighborRow, int neighborTableIndex);

			double GetRightSideDistance([NotNull] IReadOnlyRow neighborRow, int neighborTableIndex);
		}

		protected static SegmentHull CreateSegmentHull(
			[NotNull] SegmentProxy segment, IFeatureRowsDistance rowsDistance,
			[NotNull] SegmentCap startCap, [NotNull] SegmentCap endCap)
		{
			var assym = rowsDistance as IAssymetricFeatureRowsDistance;
			SegmentHull hull = assym != null
				                   ? new SegmentHull(segment, assym.GetLeftSideDistance(),
				                                     assym.GetRightSideDistance(), startCap, endCap)
				                   : new SegmentHull(segment, rowsDistance.GetRowDistance(),
				                                     startCap, endCap);
			return hull;
		}

		protected static SegmentHull CreateNeighborSegmentHull(
			[NotNull] SegmentProxy segment, IFeatureRowsDistance rowsDistance,
			[NotNull] IReadOnlyFeature neighborFeature, int neighborTableIndex,
			[NotNull] SegmentCap startCap, [NotNull] SegmentCap endCap)
		{
			var assym = rowsDistance as IAssymetricFeatureRowsDistance;
			SegmentHull hull =
				assym != null
					? new SegmentHull(
						segment, assym.GetLeftSideDistance(neighborFeature, neighborTableIndex),
						assym.GetRightSideDistance(neighborFeature, neighborTableIndex), startCap,
						endCap)
					: new SegmentHull(
						segment, rowsDistance.GetNearDistance(neighborFeature, neighborTableIndex),
						startCap, endCap);
			return hull;
		}

		protected class ConstantFeatureDistanceProvider : ConstantDistanceProvider,
		                                                  IFeatureDistanceProvider,
		                                                  IFeatureRowsDistance
		{
			public ConstantFeatureDistanceProvider(double featureDistance)
				: base(featureDistance, featureDistance) { }

			public IFeatureRowsDistance GetRowsDistance(IReadOnlyRow row1, int tableIndex)
			{
				return this;
			}

			double IFeatureRowsDistance.GetRowDistance() => SelfDistance;

			double IFeatureRowsDistance.GetNearDistance(IReadOnlyRow neighbor, int tableIndex)
			{
				return NeighborDistance;
			}

			double IPairRowsDistance.GetAddedDistance(IReadOnlyRow neighbor, int tableIndex)
			{
				return SelfDistance + NeighborDistance;
			}
		}

		protected class ConstantPairDistanceProvider : ConstantDistanceProvider,
		                                               IPairDistanceProvider,
		                                               IPairRowsDistance
		{
			public ConstantPairDistanceProvider(double pairDistance)
				: base(pairDistance, 0) { }

			public IPairRowsDistance GetRowsDistance(IReadOnlyRow row1, int tableIndex)
			{
				return this;
			}

			double IPairRowsDistance.GetAddedDistance(IReadOnlyRow row, int tableIndex)
			{
				return SelfDistance;
			}
		}

		protected abstract class ConstantDistanceProvider : IConstantDistanceProvider
		{
			public double SelfDistance { get; }
			public double NeighborDistance { get; }

			protected ConstantDistanceProvider(double selfDistance, double neighborDistance)
			{
				SelfDistance = selfDistance;
				NeighborDistance = neighborDistance;
			}

			bool IConstantDistanceProvider.TryGetConstantDistance(out double distance)
			{
				distance = SelfDistance + NeighborDistance;
				return true;
			}
		}

		protected class SideDistanceProvider : IFeatureDistanceProvider
		{
			private readonly IFeatureDistanceProvider _defaultDistanceProvider;
			private readonly IFeatureDistanceProvider _rightSideDistanceProvider;

			public SideDistanceProvider([NotNull] IFeatureDistanceProvider defaultDistanceProvider,
			                            [NotNull]
			                            IFeatureDistanceProvider rightSideDistanceProvider)
			{
				_defaultDistanceProvider = defaultDistanceProvider;
				_rightSideDistanceProvider = rightSideDistanceProvider;
			}

			IFeatureRowsDistance IFeatureDistanceProvider.GetRowsDistance(
				IReadOnlyRow row, int tableIndex)
				=> GetRowsDistance(row, tableIndex);

			public SideRowsDistance GetRowsDistance(IReadOnlyRow row, int tableIndex)
			{
				return new SideRowsDistance(
					_defaultDistanceProvider.GetRowsDistance(row, tableIndex),
					_rightSideDistanceProvider.GetRowsDistance(row, tableIndex));
			}
		}

		protected class SideRowsDistance : IAssymetricFeatureRowsDistance
		{
			private readonly IFeatureRowsDistance _defaultDistance;
			private readonly IFeatureRowsDistance _rightSideDistance;
			private readonly IList<IFeatureRowsDistance> _rowsDistances;
			private readonly double _maxRowDistance;

			public SideRowsDistance([NotNull] IFeatureRowsDistance defaultDistance,
			                        [NotNull] IFeatureRowsDistance rightSideDistance)
			{
				_defaultDistance = defaultDistance;
				_rightSideDistance = rightSideDistance;

				_rowsDistances = new List<IFeatureRowsDistance>();
				_rowsDistances.Add(_defaultDistance);
				_rowsDistances.Add(_rightSideDistance);
				_maxRowDistance = GetRowDistance();
			}

			public double GetLeftSideDistance() => _defaultDistance.GetRowDistance();

			public double GetRightSideDistance() => _rightSideDistance.GetRowDistance();

			public double GetLeftSideDistance(IReadOnlyRow neighborfeature, int neighborTableIndex)
				=> _defaultDistance.GetNearDistance(neighborfeature, neighborTableIndex);

			public double
				GetRightSideDistance(IReadOnlyRow neighborfeature, int neighborTableIndex) =>
				_rightSideDistance.GetNearDistance(neighborfeature, neighborTableIndex);

			double IFeatureRowsDistance.GetRowDistance() => _maxRowDistance;

			private double GetRowDistance()
			{
				double maxDistance = 0;
				foreach (IFeatureRowsDistance featureRowsDistance in _rowsDistances)
				{
					double distance = featureRowsDistance.GetRowDistance();
					maxDistance = Math.Max(maxDistance, distance);
				}

				return maxDistance;
			}

			public double GetNearDistance(IReadOnlyRow neighborRow, int neighborTableIndex)
			{
				double maxDistance = 0;
				foreach (IFeatureRowsDistance featureRowsDistance in _rowsDistances)
				{
					double distance =
						featureRowsDistance.GetNearDistance(neighborRow, neighborTableIndex);
					maxDistance = Math.Max(maxDistance, distance);
				}

				return maxDistance;
			}

			public double GetAddedDistance(IReadOnlyRow neighborRow, int neighborTableIndex)
			{
				double maxDistance = 0;
				foreach (IFeatureRowsDistance featureRowsDistance in _rowsDistances)
				{
					double distance =
						featureRowsDistance.GetAddedDistance(neighborRow, neighborTableIndex);
					maxDistance = Math.Max(maxDistance, distance);
				}

				return maxDistance;
			}
		}

		protected class FactorDistanceProvider : IPairDistanceProvider,
		                                         IConstantDistanceProvider
		{
			private readonly double _factor;
			[NotNull] private readonly IFeatureDistanceProvider _baseDistanceProvider;

			public FactorDistanceProvider(double factor,
			                              [NotNull] IFeatureDistanceProvider
				                              baseDistanceProvider)
			{
				_factor = factor;
				_baseDistanceProvider = baseDistanceProvider;
			}

			public IPairRowsDistance GetRowsDistance(IReadOnlyRow row, int tableIndex)
			{
				IPairRowsDistance baseRowsDistance =
					_baseDistanceProvider.GetRowsDistance(row, tableIndex);
				return new FactorRowsDistance(_factor, baseRowsDistance);
			}

			bool IConstantDistanceProvider.TryGetConstantDistance(out double distance)
			{
				var baseProvider =
					_baseDistanceProvider as IConstantDistanceProvider;
				if (baseProvider == null)
				{
					distance = double.NaN;
					return false;
				}

				double baseDistance;
				bool success = baseProvider.TryGetConstantDistance(out baseDistance);
				distance = _factor * baseDistance;
				return success;
			}
		}

		protected class FactorRowsDistance : IPairRowsDistance
		{
			private readonly double _factor;
			[NotNull] private readonly IPairRowsDistance _baseRowsDistance;

			public FactorRowsDistance(double factor,
			                          [NotNull] IPairRowsDistance baseRowsDistance)
			{
				_factor = factor;
				_baseRowsDistance = baseRowsDistance;
			}

			public double GetAddedDistance(IReadOnlyRow neighborRow, int neighborTableIndex)
			{
				return _factor *
				       _baseRowsDistance.GetAddedDistance(neighborRow, neighborTableIndex);
			}
		}

		protected class ExpressionBasedDistanceProvider : IFeatureDistanceProvider,
		                                                  IPairDistanceProvider
		{
			[NotNull] private readonly ICollection<IReadOnlyFeatureClass> _featureClasses;
			[NotNull] private readonly List<string> _expressionsSql;

			[NotNull] private Func<int, bool> _getSqlCaseSensitivityForTableIndex;
			[CanBeNull] private List<DoubleFieldExpression> _expressions;

			public ExpressionBasedDistanceProvider(
				[NotNull] IEnumerable<string> expressions,
				[NotNull] ICollection<IReadOnlyFeatureClass> featureClasses)
			{
				Assert.ArgumentNotNull(expressions, nameof(expressions));
				Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

				_featureClasses = featureClasses;
				_getSqlCaseSensitivityForTableIndex = GetDefaultCaseSensity;

				_expressionsSql = expressions.ToList();

				Assert.ArgumentCondition(
					_expressionsSql.Count == 1 || _expressionsSql.Count == featureClasses.Count,
					"unexpected number of expressions " +
					"(must be 1 or # of tables)");
			}

			public ExpressionBasedDistanceProvider(
				ExpressionBasedDistanceProviderDefinition definition)
				: this(definition.Expressions,
				       definition.FeatureClasses.Cast<IReadOnlyFeatureClass>().ToList()) { }

			[NotNull]
			protected List<DoubleFieldExpression> Expressions
				=> _expressions ?? (_expressions = GetExpressions(_expressionsSql));

			private bool GetDefaultCaseSensity(int tableIndex)
			{
				return false;
			}

			[NotNull]
			public Func<int, bool> GetSqlCaseSensitivityForTableIndex
			{
				get { return _getSqlCaseSensitivityForTableIndex; }
				set { _getSqlCaseSensitivityForTableIndex = value; }
			}

			IPairRowsDistance IPairDistanceProvider.GetRowsDistance(
				IReadOnlyRow row1, int tableIndex)
			{
				return GetRowsDistance(row1, tableIndex);
			}

			IFeatureRowsDistance IFeatureDistanceProvider.GetRowsDistance(IReadOnlyRow row1,
				int tableIndex)
			{
				return GetRowsDistance(row1, tableIndex);
			}

			public ExpressionBasedRowsDistance GetRowsDistance(IReadOnlyRow row1, int tableIndex)
			{
				double distance1 = Expressions[tableIndex].GetDouble(row1) ?? 0;
				return GetExpressionBasedRowsDistance(row1, tableIndex, distance1);
			}

			[NotNull]
			protected virtual ExpressionBasedRowsDistance GetExpressionBasedRowsDistance(
				[NotNull] IReadOnlyRow row1, int tableIndex, double distance1)
			{
				return new ExpressionBasedRowsDistance(row1, tableIndex, distance1, Expressions);
			}

			[NotNull]
			private List<DoubleFieldExpression> GetExpressions(
				[NotNull] IList<string> expressions)
			{
				var result = new List<DoubleFieldExpression>();
				var tableIndex = 0;

				foreach (IReadOnlyFeatureClass featureClass in _featureClasses)
				{
					int expressionIndex = expressions.Count == 1
						                      ? 0
						                      : tableIndex;
					string expression = expressions[expressionIndex];

					result.Add(new DoubleFieldExpression(
						           (IReadOnlyTable) featureClass, expression,
						           caseSensitive: GetSqlCaseSensitivityForTableIndex(tableIndex)));
					tableIndex++;
				}

				return result;
			}
		}

		protected class ExpressionBasedRowsDistance : IFeatureRowsDistance
		{
			private readonly double _distance1;
			[NotNull] private readonly List<DoubleFieldExpression> _expressions;
			private static readonly bool _combined = false;

			public ExpressionBasedRowsDistance(
				[NotNull] IReadOnlyRow row1, int tableIndex1, double distance1,
				[NotNull] List<DoubleFieldExpression> expressions)
			{
				Row1 = row1;
				TableIndex1 = tableIndex1;
				_distance1 = distance1;
				_expressions = expressions;
			}

			[NotNull]
			[PublicAPI]
			public IReadOnlyRow Row1 { get; }

			[PublicAPI]
			public int TableIndex1 { get; }

			public double GetRowDistance() => _combined ? 0 : _distance1;

			public virtual double GetNearDistance(IReadOnlyRow neighborRow, int neighborTableIndex)
			{
				double nearDistance;
				if (_combined)
				{
					nearDistance = Row1 == neighborRow
						               ? _distance1 * 2
						               : _distance1 +
						                 (_expressions[neighborTableIndex].GetDouble(neighborRow) ??
						                  0);
				}
				else
				{
					nearDistance = Row1 == neighborRow
						               ? _distance1
						               : _expressions[neighborTableIndex].GetDouble(neighborRow) ??
						                 0;
				}

				return nearDistance;
			}

			public virtual double GetAddedDistance(IReadOnlyRow neighborRow, int neighborTableIndex)
			{
				if (Row1 == neighborRow)
				{
					return _distance1 * 2;
				}

				return _distance1 + (_expressions[neighborTableIndex].GetDouble(neighborRow) ?? 0);
			}
		}

		#endregion
	}
}
