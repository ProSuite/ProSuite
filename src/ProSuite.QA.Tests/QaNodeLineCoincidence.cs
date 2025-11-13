using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaNodeLineCoincidence : ContainerTest
	{
		private readonly double _withinPolylineTolerance;
		private readonly bool _ignoreNearEndpoints;
		private readonly bool _is3D;
		private IEnvelope _searchEnvelopeTemplate;
		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;
		private readonly IPoint _pointTemplate;
		private readonly IPoint _neighborPointTemplate;
		private readonly int _tableCount;
		private readonly double _xyTolerance;
		private readonly ISpatialReference _spatialReference;
		private readonly bool _nodesHaveZ;
		private readonly List<bool> _hasZ = new List<bool>();
		private readonly List<esriGeometryType> _shapeTypes = new List<esriGeometryType>();
		private readonly esriGeometryType _nodeClassShapeType;
		private readonly string _shapeFieldName;
		private readonly IDictionary<int, double> _nearTolerancesByTableIndex;

		private IEnvelope _tileEnvelope;

		// oids of processed multi-part polylines (they must not be processed once per intersected tile)
		private HashSet<long> _processedMultipartPolylines;

		private const double _defaultCoincidenceTolerance = -1;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NodeTooCloseToLine_WithinFeature =
				"NodeTooCloseToLine.WithinFeature";

			public const string NodeTooCloseToLine_BetweenFeatures =
				"NodeTooCloseToLine.BetweenFeatures";

			public Code() : base("NodeLineCoincidence") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaNodeLineCoincidence_0))]
		public QaNodeLineCoincidence(
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_nodeClass))] [NotNull]
				IReadOnlyFeatureClass nodeClass,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearClasses))] [NotNull]
				IList<IReadOnlyFeatureClass> nearClasses,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_near))]
				double near)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(nodeClass, nearClasses, near, false) { }

		[Doc(nameof(DocStrings.QaNodeLineCoincidence_1))]
		public QaNodeLineCoincidence(
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_nodeClass))] [NotNull]
				IReadOnlyFeatureClass nodeClass,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearClasses))] [NotNull]
				IList<IReadOnlyFeatureClass> nearClasses,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaNodeLineCoincidence_ignoreNearEndpoints))]
				bool ignoreNearEndpoints)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(nodeClass, nearClasses, near, ignoreNearEndpoints, false) { }

		[Doc(nameof(DocStrings.QaNodeLineCoincidence_1))]
		public QaNodeLineCoincidence(
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nodeClass))] [NotNull]
			IReadOnlyFeatureClass nodeClass,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> nearClasses,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_ignoreNearEndpoints))]
			bool ignoreNearEndpoints,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_is3D))]
			bool is3D)
			: this(nodeClass, nearClasses, new[] { near }, near, ignoreNearEndpoints, is3D) { }

		[Doc(nameof(DocStrings.QaNodeLineCoincidence_3))]
		public QaNodeLineCoincidence(
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nodeClass))] [NotNull]
			IReadOnlyFeatureClass nodeClass,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> nearClasses,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_nearTolerances))] [NotNull]
			IList<double> nearTolerances,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_withinPolylineTolerance))]
			double withinPolylineTolerance,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_ignoreNearEndpoints))]
			bool ignoreNearEndpoints,
			[Doc(nameof(DocStrings.QaNodeLineCoincidence_is3D))]
			bool is3D) :
			base(CastToTables(new[] { nodeClass }, nearClasses))
		{
			Assert.ArgumentNotNull(nodeClass, nameof(nodeClass));
			Assert.ArgumentNotNull(nearClasses, nameof(nearClasses));
			Assert.ArgumentNotNull(nearTolerances, nameof(nearTolerances));
			Assert.ArgumentCondition(
				nearTolerances.Count == 1 || nearTolerances.Count == nearClasses.Count,
				"Invalid number of near tolerances: either one tolerance must be specified (used for all), " +
				"or one tolerance per nearClasses (in the same order)");

			_withinPolylineTolerance = withinPolylineTolerance;
			_ignoreNearEndpoints = ignoreNearEndpoints;
			_is3D = is3D;
			_xyTolerance = GeometryUtils.GetXyTolerance(nodeClass.SpatialReference);
			_spatialReference = (nodeClass).SpatialReference;
			_nodeClassShapeType = nodeClass.ShapeType;
			_shapeFieldName = nodeClass.ShapeFieldName;

			_nearTolerancesByTableIndex = GetTolerancesByTableIndex(
				nearClasses, nearTolerances,
				withinPolylineTolerance);

			SearchDistance = nearTolerances.Max();
			CoincidenceTolerance = _defaultCoincidenceTolerance;

			_filter = null;

			_tableCount = InvolvedTables.Count;

			_pointTemplate = new PointClass();
			_neighborPointTemplate = new PointClass();

			_nodesHaveZ = DatasetUtils.GetGeometryDef(nodeClass).HasZ;
			for (var tableIndex = 0; tableIndex < _tableCount; tableIndex++)
			{
				var featureClass = InvolvedTables[tableIndex] as IReadOnlyFeatureClass;

				_hasZ.Add(featureClass != null && DatasetUtils.GetGeometryDef(featureClass).HasZ);
				_shapeTypes.Add(featureClass?.ShapeType ?? esriGeometryType.esriGeometryNull);
			}
		}

		[InternallyUsedTest]
		public QaNodeLineCoincidence(QaNodeLineCoincidenceDefinition definition)
			: this((IReadOnlyFeatureClass) definition.NodeClass,
			       definition.NearClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.NearTolerances.Cast<double>().ToList(),
			       definition.WithinPolylineTolerance, definition.IgnoreNearEndpoints,
			       definition.Is3D)
		{
			CoincidenceTolerance = definition.CoincidenceTolerance;
		}

		[TestParameter(_defaultCoincidenceTolerance)]
		[Doc(nameof(DocStrings.QaNodeLineCoincidence_CoincidenceTolerance))]
		public double CoincidenceTolerance { get; set; }

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			_tileEnvelope = parameters.TileEnvelope;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			// preparing
			if (_filter == null)
			{
				InitFilter();
			}

			if (tableIndex > 0)
			{
				// continue only for nodes (table index = 0)
				return NoError;
			}

			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			var errorCount = 0;

			IGeometry shape = feature.Shape;
			if (_nodeClassShapeType == esriGeometryType.esriGeometryPolyline &&
			    _withinPolylineTolerance > 0)
			{
				var parts = (IGeometryCollection) shape;
				if (parts.GeometryCount > 1)
				{
					errorCount += CheckWithinMultipartPolyline(feature, parts,
					                                           _withinPolylineTolerance);
				}
			}

			double? lineLength = _nodeClassShapeType == esriGeometryType.esriGeometryPolyline
				                     ? (double?) ((IPolyline) shape).Length
				                     : null;

			if (lineLength <= SearchDistance)
			{
				// special treatment for lines with length <= search distance
				errorCount += CheckShortLineNodes(feature, tableIndex, shape, lineLength.Value);
			}
			else
			{
				foreach (IPoint node in GetNodes(shape, _pointTemplate))
				{
					// Ignore the nodes outside the current tile box
					// (to avoid tiling artefacts due to ignoring connected features fully outside the tile)
					if (IsOutsideTile(node))
					{
						continue;
					}

					errorCount += CheckNode(feature, tableIndex, node);
				}
			}

			return errorCount;
		}

		protected override int CompleteTileCore(TileInfo tileInfo)
		{
			if (tileInfo.State == TileState.Final)
			{
				_processedMultipartPolylines = null;
			}

			return NoError;
		}

		private bool IsOutsideTile([NotNull] IGeometry geometry)
		{
			return _tileEnvelope != null &&
			       ((IRelationalOperator) _tileEnvelope).Disjoint(geometry);
		}

		[NotNull]
		private static IDictionary<int, double> GetTolerancesByTableIndex(
			[NotNull] ICollection<IReadOnlyFeatureClass> nearClasses,
			[NotNull] IList<double> nearTolerances,
			double withinPolylineTolerance)
		{
			var result = new Dictionary<int, double>(nearClasses.Count);

			const int nodeClassIndex = 0;
			result.Add(nodeClassIndex, withinPolylineTolerance);

			for (var nearClassIndex = 0; nearClassIndex < nearClasses.Count; nearClassIndex++)
			{
				int globalTableIndex = nearClassIndex + 1;

				double nearTolerance = nearTolerances.Count == 1
					                       ? nearTolerances[0]
					                       : nearTolerances[nearClassIndex];
				result.Add(globalTableIndex, nearTolerance);
			}

			return result;
		}

		private int CheckWithinMultipartPolyline(
			[NotNull] IReadOnlyFeature feature,
			[NotNull] IGeometryCollection pathCollection,
			double searchDistance)
		{
			long oid = feature.OID;

			if (_processedMultipartPolylines != null &&
			    _processedMultipartPolylines.Contains(oid))
			{
				return NoError;
			}

			if (_processedMultipartPolylines == null)
			{
				_processedMultipartPolylines = new HashSet<long>();
			}

			_processedMultipartPolylines.Add(oid);

			// multi-part polyline; search among different paths
			return CheckPaths(feature, pathCollection, searchDistance);
		}

		private int CheckPaths([NotNull] IReadOnlyFeature feature,
		                       [NotNull] IGeometryCollection pathCollection,
		                       double searchDistance)
		{
			List<IPolyline> paths = GetPaths(pathCollection);

			var errorCount = 0;

			int pathCount = paths.Count;
			for (var i = 0; i < pathCount; i++)
			{
				IPolyline path = paths[i];

				foreach (IPoint node in GetNodes(path, _pointTemplate))
				{
					bool isConnected;
					List<TooClosePath> tooClosePaths = GetTooClosePaths(node, paths, i,
						searchDistance,
						out isConnected);

					if (isConnected || tooClosePaths.Count <= 0)
					{
						continue;
					}

					foreach (TooClosePath tooClosePath in tooClosePaths)
					{
						IPoint errorGeometry = GeometryFactory.Clone(node);
						string description =
							string.Format(
								"Unconnected node is too close to path {0} on same polyline: {1}",
								tooClosePath.PathIndex,
								FormatLengthComparison(tooClosePath.Distance,
								                       Math.Abs(tooClosePath.Distance -
								                                searchDistance) <
								                       double.Epsilon
									                       ? "="
									                       : "<",
								                       searchDistance, _spatialReference));

						errorCount += ReportError(
							description, InvolvedRowUtils.GetInvolvedRows(feature), errorGeometry,
							Codes[Code.NodeTooCloseToLine_WithinFeature],
							_shapeFieldName,
							values: new object[] { tooClosePath.Distance, tooClosePath.PathIndex });
					}
				}
			}

			return errorCount;
		}

		[NotNull]
		private List<TooClosePath> GetTooClosePaths([NotNull] IPoint node,
		                                            [NotNull] IList<IPolyline> paths,
		                                            int currentPathIndex,
		                                            double searchDistance,
		                                            out bool isConnected)
		{
			int pathCount = paths.Count;
			isConnected = false;
			var result = new List<TooClosePath>();

			double epsilon = GetEpsilon(node);

			for (var j = 0; j < pathCount; j++)
			{
				if (currentPathIndex == j)
				{
					continue;
				}

				IPolyline nearPath = paths[j];

				double distance = GetDistance(node, nearPath, _nodesHaveZ);

				if (IsWithinCoincidenceTolerance(distance, epsilon))
				{
					// connected to another path, --> don't report other errors
					isConnected = true;
					break;
				}

				if (distance >= searchDistance)
				{
					// far enough -> correct
					continue;
				}

				result.Add(new TooClosePath(j, distance));
			}

			return result;
		}

		[NotNull]
		private static List<IPolyline> GetPaths(
			[NotNull] IGeometryCollection pathCollection)
		{
			int pathCount = pathCollection.GeometryCount;

			var result = new List<IPolyline>(pathCount);

			for (var i = 0; i < pathCount; i++)
			{
				result.Add(GeometryFactory.CreatePolyline(pathCollection.get_Geometry(i)));
			}

			return result;
		}

		[NotNull]
		private IEnumerable<int> GetNearTableIndexes()
		{
			const int firstNearTableIndex = 1;
			for (int nearTableIndex = firstNearTableIndex;
			     nearTableIndex < _tableCount;
			     nearTableIndex++)
			{
				yield return nearTableIndex;
			}
		}

		private int CheckNode([NotNull] IReadOnlyFeature feature,
		                      int tableIndex,
		                      [NotNull] IPoint node)
		{
			var tooCloseNeighbors = new List<TooCloseNeighbor>();

			foreach (int nearTableIndex in GetNearTableIndexes())
			{
				var nearFeatureClass = (IReadOnlyFeatureClass) InvolvedTables[nearTableIndex];

				bool isConnected;
				GetTooCloseNeighbors(tooCloseNeighbors, feature, tableIndex, node,
				                     nearFeatureClass, nearTableIndex,
				                     out isConnected);

				if (isConnected)
				{
					// if the node is connected to any neighbor, other near features don't 
					// count as errors
					return NoError;
				}
			}

			return ReportErrors(feature, node, tooCloseNeighbors);
		}

		private int CheckShortLineNodes([NotNull] IReadOnlyFeature feature,
		                                int tableIndex,
		                                [NotNull] IGeometry shape,
		                                double lineLength)
		{
			var connectedFeatures = new List<IReadOnlyFeature>();
			var unconnectedNodeCandidates = new List<UnconnectedNodeCandidate>();

			foreach (IPoint node in GetNodes(shape, _pointTemplate))
			{
				var tooCloseNeighbors = new List<TooCloseNeighbor>();
				var nodeHasAnyConnection = false;

				foreach (int nearTableIndex in GetNearTableIndexes())
				{
					var nearFeatureClass = (IReadOnlyFeatureClass) InvolvedTables[nearTableIndex];

					bool isConnectedToNeighbor;
					const bool returnAllConnectedNeighbors = true;
					GetTooCloseNeighbors(tooCloseNeighbors, feature, tableIndex, node,
					                     nearFeatureClass, nearTableIndex,
					                     out isConnectedToNeighbor,
					                     connectedFeatures, returnAllConnectedNeighbors);

					if (isConnectedToNeighbor)
					{
						// if the node is connected to any neighbor, other near features don't 
						// count as errors
						nodeHasAnyConnection = true;
					}
				}

				if (! nodeHasAnyConnection && tooCloseNeighbors.Count > 0)
				{
					unconnectedNodeCandidates.Add(
						new UnconnectedNodeCandidate(
							GeometryFactory.Clone(node), tooCloseNeighbors));
				}
			}

			var errorCount = 0;

			foreach (UnconnectedNodeCandidate candidate in GetRelevantCandidates(
				         unconnectedNodeCandidates, connectedFeatures, lineLength))
			{
				errorCount += ReportErrors(feature, candidate.Node, candidate.TooCloseNeighbors);
			}

			return errorCount;
		}

		[NotNull]
		private IEnumerable<UnconnectedNodeCandidate> GetRelevantCandidates(
			[NotNull] IEnumerable<UnconnectedNodeCandidate> unconnectedNodeCandidates,
			[NotNull] ICollection<IReadOnlyFeature> connectedFeatures,
			double lineLength)
		{
			foreach (UnconnectedNodeCandidate candidate in unconnectedNodeCandidates)
			{
				var relevantNeighbors = new List<TooCloseNeighbor>();

				foreach (TooCloseNeighbor tooCloseNeighbor in candidate.TooCloseNeighbors)
				{
					double nearTolerance = _nearTolerancesByTableIndex[tooCloseNeighbor.TableIndex];

					if (lineLength > nearTolerance ||
					    ! connectedFeatures.Contains(tooCloseNeighbor.Feature))
					{
						relevantNeighbors.Add(tooCloseNeighbor);
					}
				}

				if (relevantNeighbors.Count <= 0)
				{
					continue;
				}

				if (relevantNeighbors.Count == candidate.TooCloseNeighbors.Count)
				{
					yield return candidate;
				}
				else
				{
					yield return new UnconnectedNodeCandidate(candidate.Node, relevantNeighbors);
				}
			}
		}

		private int ReportErrors([NotNull] IReadOnlyFeature feature,
		                         [NotNull] IPoint node,
		                         [NotNull] IEnumerable<TooCloseNeighbor> tooCloseNeighbors)
		{
			// TODO consider allowing to report all neighbors in one error (no longer indicating individual distance)

			return tooCloseNeighbors.Sum(neighbor => ReportError(feature, node, neighbor));
		}

		private int ReportError([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IPoint node,
		                        [NotNull] TooCloseNeighbor neighborTooClose)
		{
			double searchDistance = _nearTolerancesByTableIndex[neighborTooClose.TableIndex];

			IPoint errorGeometry = GeometryFactory.Clone(node);
			string description =
				string.Format(
					"Unconnected node is too close to nearest (border)-line: {0}",
					FormatLengthComparison(neighborTooClose.Distance,
					                       Math.Abs(neighborTooClose.Distance - searchDistance) <
					                       double.Epsilon
						                       ? "="
						                       : "<",
					                       searchDistance, _spatialReference));

			// NOTE: currently the neighbors are different features; this may be changed later
			IssueCode issueCode = feature == neighborTooClose.Feature
				                      ? Codes[Code.NodeTooCloseToLine_WithinFeature]
				                      : Codes[Code.NodeTooCloseToLine_BetweenFeatures];
			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature, neighborTooClose.Feature),
				errorGeometry, issueCode, _shapeFieldName,
				values: new object[] { neighborTooClose.Distance });
		}

		/// <summary>
		/// Gets the nodes.
		/// </summary>
		/// <param name="shape">The shape.</param>
		/// <param name="pointTemplate">The point template.</param>
		/// <returns>The nodes for the shape. The same point instance (the template) is returned for each iteration. 
		/// Therefore the returned points must be immediately processed and must not be put in a list or otherwise kept around (unless cloned)</returns>
		[NotNull]
		private static IEnumerable<IPoint> GetNodes([NotNull] IGeometry shape,
		                                            [NotNull] IPoint pointTemplate)
		{
			switch (shape.GeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					yield return (IPoint) shape;
					break;

				case esriGeometryType.esriGeometryPolyline:
					var polyline = (IPolyline) shape;
					var parts = polyline as IGeometryCollection;

					if (parts == null || parts.GeometryCount == 1)
					{
						// single-part polyline
						polyline.QueryFromPoint(pointTemplate);
						yield return pointTemplate;

						polyline.QueryToPoint(pointTemplate);
						yield return pointTemplate;
					}
					else
					{
						// multipart polyline; get the from/to points of each individual path
						foreach (IGeometry part in GeometryUtils.GetParts(parts))
						{
							foreach (IPoint point in GetNodes(part, pointTemplate))
							{
								yield return point;
							}
						}
					}

					break;

				case esriGeometryType.esriGeometryPath:
					var path = (IPath) shape;

					path.QueryFromPoint(pointTemplate);
					yield return pointTemplate;

					path.QueryToPoint(pointTemplate);
					yield return pointTemplate;

					break;

				default:
					yield break;
			}
		}

		private void GetTooCloseNeighbors(
			[NotNull] ICollection<TooCloseNeighbor> tooCloseNeighbors,
			[NotNull] IReadOnlyFeature feature,
			int nodeTableIndex,
			[NotNull] IPoint node,
			[NotNull] IReadOnlyFeatureClass nearFeatureClass,
			int nearTableIndex,
			out bool isConnected,
			[CanBeNull] ICollection<IReadOnlyFeature> connectedNeighbors = null,
			bool returnAllConnectedNeighbors = false)
		{
			double searchDistance = _nearTolerancesByTableIndex[nearTableIndex];

			double epsilon = GetEpsilon(node);

			isConnected = false;
			foreach (IReadOnlyFeature neighborFeature in
			         Search(node, nearFeatureClass, nearTableIndex, searchDistance))
			{
				if (feature == neighborFeature)
				{
					// checking end points against segments along the same line not yet 
					// supported (would have to exclude the segment(s) leading to the node itself)
					continue;
				}

				var neighborCurve = (IPolycurve) neighborFeature.Shape;

				double distance = GetDistance(node, nearTableIndex, neighborCurve);

				if (IsWithinCoincidenceTolerance(distance, epsilon))
				{
					// connected to another curve, --> don't report other errors
					isConnected = true;

					connectedNeighbors?.Add(neighborFeature);

					if (! returnAllConnectedNeighbors)
					{
						break;
					}
				}

				if (distance > searchDistance)
				{
					// far enough -> correct
					continue;
				}

				// option to only report this if no polyline endpoint is within the distance
				// --> avoids double reporting of errors with QaMinNodeDistance
				if (neighborCurve is IPolyline)
				{
					bool neighborHasZ = _hasZ[nearTableIndex];

					if (_ignoreNearEndpoints)
					{
						if (IsNeighborEndPointWithinSearchDistance(node,
							    neighborCurve,
							    neighborHasZ,
							    searchDistance))
						{
							continue;
						}
					}
					else
					{
						if (feature.Table == nearFeatureClass)
						{
							// avoid that near end points from same feature set are reported twice
							if (feature.OID < neighborFeature.OID)
							{
								// this is a candidate for MinimumOID exclusion...

								// ... but only if the filter expressions are equal
								if (FilterExpressionsAreEqual(nearTableIndex, nodeTableIndex))
								{
									// check if an end point is within the search distance:
									if (IsNeighborEndPointWithinSearchDistance(node,
										    neighborCurve,
										    neighborHasZ,
										    searchDistance))
									{
										// exclude this to avoid double reporting
										continue;
									}
								}
							}
						}
					}
				}

				// too close and not coincident
				tooCloseNeighbors.Add(new TooCloseNeighbor(neighborFeature, nearTableIndex,
				                                           distance));
			}
		}

		private static double GetEpsilon([NotNull] IPoint point)
		{
			double x;
			double y;
			point.QueryCoords(out x, out y);

			return MathUtils.GetDoubleSignificanceEpsilon(Math.Max(Math.Abs(x), Math.Abs(y)));
		}

		private bool IsWithinCoincidenceTolerance(double distance, double epsilon)
		{
			double tolerance = CoincidenceTolerance < 0
				                   ? _xyTolerance
				                   : CoincidenceTolerance;

			if (tolerance < Math.Abs(epsilon))
			{
				tolerance = Math.Abs(epsilon);
			}

			return distance <= tolerance;
		}

		[NotNull]
		private IEnumerable<IReadOnlyFeature> Search(
			[NotNull] IPoint node,
			[NotNull] IReadOnlyFeatureClass nearFeatureClass,
			int nearTableIndex,
			double searchDistance)
		{
			var table = (IReadOnlyTable) nearFeatureClass;

			IFeatureClassFilter filter = GetSearchFilter(nearTableIndex, node, searchDistance);

			QueryFilterHelper filterHelper = _helper[nearTableIndex];
			filterHelper.MinimumOID = -1; // not symmetrical, can't set MinimumOID

			return Search(table, filter, filterHelper).Cast<IReadOnlyFeature>();
		}

		private double GetDistance([NotNull] IPoint node,
		                           int nearTableIndex,
		                           [NotNull] IPolycurve neighborCurve)
		{
			bool neighborHasZ = _hasZ[nearTableIndex];

			if (_shapeTypes[nearTableIndex] != esriGeometryType.esriGeometryPolygon)
			{
				return GetDistance(node, neighborCurve, neighborHasZ);
			}

			IGeometry boundary = GeometryUtils.GetBoundary(neighborCurve);

			try
			{
				return GetDistance(node, boundary, neighborHasZ);
			}
			finally
			{
				Marshal.ReleaseComObject(boundary);
			}
		}

		private bool FilterExpressionsAreEqual(int tableIndex1, int tableIndex2)
		{
			return string.Equals(GetConstraint(tableIndex1),
			                     GetConstraint(tableIndex2),
			                     StringComparison.OrdinalIgnoreCase);
		}

		private double GetDistance([NotNull] IPoint node,
		                           [NotNull] IGeometry neighborGeometry,
		                           bool neighborHasZ)
		{
			return _is3D && _nodesHaveZ && neighborHasZ
				       ? ((IProximityOperator3D) node).ReturnDistance3D(neighborGeometry)
				       : ((IProximityOperator) node).ReturnDistance(neighborGeometry);
		}

		[NotNull]
		private IFeatureClassFilter GetSearchFilter(int tableIndex,
		                                            [NotNull] IPoint point,
		                                            double searchDistance)
		{
			IFeatureClassFilter filter = _filter[tableIndex];

			double x;
			double y;
			point.QueryCoords(out x, out y);

			_searchEnvelopeTemplate.PutCoords(x - searchDistance, y - searchDistance,
			                                  x + searchDistance, y + searchDistance);

			filter.FilterGeometry = _searchEnvelopeTemplate;

			return filter;
		}

		private bool IsNeighborEndPointWithinSearchDistance(
			[NotNull] IPoint node,
			[NotNull] ICurve neighborCurve,
			bool neighborHasZ,
			double searchDistance)
		{
			foreach (IPoint nearEndPoint in GetNodes(neighborCurve, _neighborPointTemplate))
			{
				if (GetDistance(node, nearEndPoint, neighborHasZ) > searchDistance)
				{
					continue;
				}

				// an end point of the neighbor line is within the search distance
				// --> ignore
				return true;
			}

			return false;
		}

		private void InitFilter()
		{
			CopyFilters(out _filter, out _helper);
			foreach (var filter in _filter)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}

			foreach (QueryFilterHelper filterHelper in _helper)
			{
				filterHelper.ForNetwork = true;
			}

			_searchEnvelopeTemplate = new EnvelopeClass();
		}

		#region New region

		private class UnconnectedNodeCandidate
		{
			private readonly List<TooCloseNeighbor> _tooCloseNeighbors;

			public UnconnectedNodeCandidate([NotNull] IPoint node,
			                                [NotNull] List<TooCloseNeighbor> tooCloseNeighbors)
			{
				Node = node;
				_tooCloseNeighbors = tooCloseNeighbors;
			}

			[NotNull]
			public IPoint Node { get; }

			[NotNull]
			public ICollection<TooCloseNeighbor> TooCloseNeighbors => _tooCloseNeighbors;
		}

		private class TooClosePath
		{
			public TooClosePath(int pathIndex, double distance)
			{
				PathIndex = pathIndex;
				Distance = distance;
			}

			public int PathIndex { get; }

			public double Distance { get; }
		}

		private class TooCloseNeighbor
		{
			public TooCloseNeighbor([NotNull] IReadOnlyFeature feature, int tableIndex,
			                        double distance)
			{
				TableIndex = tableIndex;
				Feature = feature;
				Distance = distance;
			}

			[NotNull]
			public IReadOnlyFeature Feature { get; }

			public int TableIndex { get; }

			public double Distance { get; }
		}

		#endregion
	}
}
