using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaContainedPointsCount : ContainerTest
	{
		private readonly int _minimumPointCount;
		private readonly int _maximumPointCount;
		private readonly bool _countPointOnPolygonBorder;
		[CanBeNull] private readonly string _relevantPointConditionSql;
		private readonly int _polygonClassesCount;
		private readonly int _totalClassesCount;

		private const PolylineUsage _defaultPolylineUsage = PolylineUsage.AsIs;
		private PolylineUsage _polylineUsage;

		private RelevantPointCondition _relevantPointCondition;
		private QueryFilterHelper[] _helper;
		private IFeatureClassFilter[] _queryFilter;

		[NotNull] private readonly IDictionary<int, Dictionary<long, PolygonPoints>>
			_polygonPointsByTableIndex =
				new Dictionary<int, Dictionary<long, PolygonPoints>>();

		// TODO store xmax/ymax also, to be able to discard polygons that are guaranteed to
		// not be reported anymore? Or filter out polygons from earlier tiles in CompleteTile()?
		[NotNull] private readonly Dictionary<int, HashSet<long>> _fullyCheckedPolygonsByTableIndex
			= new Dictionary<int, HashSet<long>>();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PointCount_SmallerThanExpected =
				"PointCount.SmallerThanExpected";

			public const string PointCount_LargerThanExpected = "PointCount.LargerThanExpected";
			public const string PointCount_SmallerThanMinimum = "PointCount.SmallerThanMinimum";
			public const string PointCount_LargerThanMaximum = "PointCount.LargerThanMaximum";

			public const string NotClosed_Line = "NotClosedLine";

			public Code() : base("ContainedPoints") { }
		}

		#endregion

		#region Constructors

		// TODO update doc strings (polygons AND polylines)

		[Doc(nameof(DocStrings.QaContainedPointsCount_0))]
		public QaContainedPointsCount(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClass))] [NotNull]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_expectedPointCount))]
			int expectedPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition)
			: this(new[] { polygonClass },
			       new[] { pointClass },
			       expectedPointCount,
			       expectedPointCount,
			       relevantPointCondition,
			       false) { }

		[Doc(nameof(DocStrings.QaContainedPointsCount_1))]
		public QaContainedPointsCount(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClass))] [NotNull]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_minimumPointCount))]
			int minimumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_maximumPointCount))]
			int maximumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition)
			: this(new[] { polygonClass },
			       new[] { pointClass },
			       minimumPointCount,
			       maximumPointCount,
			       relevantPointCondition,
			       false) { }

		[Doc(nameof(DocStrings.QaContainedPointsCount_2))]
		public QaContainedPointsCount(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClass))] [NotNull]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_minimumPointCount))]
			int minimumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_maximumPointCount))]
			int maximumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition,
			[Doc(nameof(DocStrings.QaContainedPointsCount_countPointOnPolygonBorder))]
			bool
				countPointOnPolygonBorder)
			: this(new[] { polygonClass },
			       new[] { pointClass },
			       minimumPointCount,
			       maximumPointCount,
			       relevantPointCondition,
			       countPointOnPolygonBorder) { }

		[Doc(nameof(DocStrings.QaContainedPointsCount_3))]
		public QaContainedPointsCount(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				polygonClasses,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				pointClasses,
			[Doc(nameof(DocStrings.QaContainedPointsCount_expectedPointCount))]
			int expectedPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition)
			: this(polygonClasses, pointClasses,
			       expectedPointCount, expectedPointCount,
			       relevantPointCondition, false) { }

		[Doc(nameof(DocStrings.QaContainedPointsCount_4))]
		public QaContainedPointsCount(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				polygonClasses,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				pointClasses,
			[Doc(nameof(DocStrings.QaContainedPointsCount_minimumPointCount))]
			int minimumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_maximumPointCount))]
			int maximumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition,
			[Doc(nameof(DocStrings.QaContainedPointsCount_countPointOnPolygonBorder))]
			bool
				countPointOnPolygonBorder) :
			base(CastToTables(
				     // ReSharper disable once PossiblyMistakenUseOfParamsMethod
				     Union(polygonClasses, pointClasses)))
		{
			Assert.ArgumentNotNull(polygonClasses, nameof(polygonClasses));
			Assert.ArgumentNotNull(pointClasses, nameof(pointClasses));
			Assert.ArgumentCondition(polygonClasses.Count > 0, "No polygon class specified");
			Assert.ArgumentCondition(pointClasses.Count > 0, "No point class specified");

			_minimumPointCount = minimumPointCount;
			_maximumPointCount = maximumPointCount;
			_countPointOnPolygonBorder = countPointOnPolygonBorder;
			_relevantPointConditionSql = relevantPointCondition;

			_polygonClassesCount = polygonClasses.Count;
			_totalClassesCount = polygonClasses.Count + pointClasses.Count;

			_polylineUsage = _defaultPolylineUsage;
		}

		#endregion

		[InternallyUsedTest]
		public QaContainedPointsCount(QaContainedPointsCountDefinition definition)
			: this(definition.PolygonClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.PointClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.MinimumPointCount,
			       definition.MaximumPointCount,
			       definition.RelevantPointCondition,
			       definition.CountPointOnPolygonBorder)
		{
			PolylineUsage = definition.PolylineUsage;
		}

		[TestParameter(_defaultPolylineUsage)]
		[Doc(nameof(DocStrings.QaContainedPointsCount_PolylineUsage))]
		public PolylineUsage PolylineUsage
		{
			get { return _polylineUsage; }
			set { _polylineUsage = value; }
		}

		#region Overrides of ContainerTest

		public override bool IsQueriedTable(int tableIndex)
		{
			// only point classes are queried
			return tableIndex >= _polygonClassesCount;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			IEnvelope tileEnvelope = args.CurrentEnvelope;

			if (tileEnvelope == null)
			{
				return NoError;
			}

			IEnvelope testRunEnvelope = args.AllBox;
			bool isLastTile = args.State == TileState.Final;

			try
			{
				var polygonPointsToCheck = new List<PolygonPoints>();

				foreach (KeyValuePair<int, Dictionary<long, PolygonPoints>> pair
				         in _polygonPointsByTableIndex)
				{
					Dictionary<long, PolygonPoints> polygonPointsByOid = pair.Value;

					var oidsToRemove = new List<long>();
					foreach (PolygonPoints polygonPoints in polygonPointsByOid.Values)
					{
						if (polygonPoints.IsFullyChecked(tileEnvelope, testRunEnvelope))
						{
							polygonPoints.PointCountComplete = true;
							polygonPointsToCheck.Add(polygonPoints);

							SetPolygonFullyChecked(polygonPoints.OID, polygonPoints.TableIndex);

							oidsToRemove.Add(polygonPoints.OID);
						}
						else
						{
							if (isLastTile)
							{
								polygonPoints.PointCountComplete = false;
								polygonPointsToCheck.Add(polygonPoints);
								oidsToRemove.Add(polygonPoints.OID);
							}
						}
					}

					foreach (int oid in oidsToRemove)
					{
						polygonPointsByOid.Remove(oid);
					}
				}

				return CheckPolygonPoints(polygonPointsToCheck);
			}
			finally
			{
				if (isLastTile)
				{
					_polygonPointsByTableIndex.Clear();
				}
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (tableIndex >= _polygonClassesCount)
			{
				return NoError;
			}

			if (_queryFilter == null)
			{
				InitFilter();
				Assert.NotNull(_queryFilter, "_queryFilter");
			}

			var feature = (IReadOnlyFeature) row;
			IGeometry containingShape = feature.Shape;

			if (containingShape == null || containingShape.IsEmpty)
			{
				return NoError;
			}

			IPolyline line = containingShape as IPolyline;
			if (line != null && PolylineUsage != PolylineUsage.AsIs)
			{
				if (! line.IsClosed)
				{
					if (PolylineUsage == PolylineUsage.AsPolygonIfClosedElseReportIssue)
					{
						return ReportError(
							"Unclosed line", InvolvedRowUtils.GetInvolvedRows(feature),
							line, Codes[Code.NotClosed_Line], null);
					}

					if (PolylineUsage == PolylineUsage.AsPolygonIfClosedElseIgnore)
					{
						return NoError;
					}
				}
				else
				{
					containingShape = GeometryFactory.CreatePolygon(line);
				}
			}

			if (IsPolygonFullyChecked(row.OID, tableIndex))
			{
				return NoError;
			}

			PolygonPoints polygonPoints = GetPolygonPoints(feature, tableIndex);

			if (_relevantPointCondition == null)
			{
				_relevantPointCondition = new RelevantPointCondition(
					_relevantPointConditionSql, GetSqlCaseSensitivity());
			}

			for (int pointClassIndex = _polygonClassesCount;
			     pointClassIndex < _totalClassesCount;
			     pointClassIndex++)
			{
				_queryFilter[pointClassIndex].FilterGeometry = containingShape;

				foreach (IReadOnlyRow pointRow in Search(InvolvedTables[pointClassIndex],
				                                         _queryFilter[pointClassIndex],
				                                         _helper[pointClassIndex]))
				{
					var pointFeature = (IReadOnlyFeature) pointRow;
					// point feature is POINT, polygon feature is POLYGON
					if (! _relevantPointCondition.IsFulfilled(
						    pointFeature, pointClassIndex,
						    row, tableIndex))
					{
						continue;
					}

					polygonPoints.AddPointFeature(pointFeature, pointClassIndex);
				}
			}

			return NoError;
		}

		#endregion

		#region Non-public

		[NotNull]
		private PolygonPoints GetPolygonPoints([NotNull] IReadOnlyFeature feature, int tableIndex)
		{
			Dictionary<long, PolygonPoints> polygonPointsByOid;
			if (! _polygonPointsByTableIndex.TryGetValue(tableIndex, out polygonPointsByOid))
			{
				polygonPointsByOid = new Dictionary<long, PolygonPoints>();
				_polygonPointsByTableIndex.Add(tableIndex, polygonPointsByOid);
			}

			long oid = feature.OID;

			PolygonPoints polygonPoints;
			if (! polygonPointsByOid.TryGetValue(oid, out polygonPoints))
			{
				polygonPoints = new PolygonPoints(feature, tableIndex);
				polygonPointsByOid.Add(oid, polygonPoints);
			}

			return polygonPoints;
		}

		private bool IsPolygonFullyChecked(long oid, int tableIndex)
		{
			HashSet<long> oids;
			return _fullyCheckedPolygonsByTableIndex.TryGetValue(tableIndex, out oids) &&
			       oids.Contains(oid);
		}

		private void SetPolygonFullyChecked(long oid, int tableIndex)
		{
			HashSet<long> oids;
			if (! _fullyCheckedPolygonsByTableIndex.TryGetValue(tableIndex, out oids))
			{
				oids = new HashSet<long>();
				_fullyCheckedPolygonsByTableIndex.Add(tableIndex, oids);
			}

			oids.Add(oid);
		}

		private bool IsError([NotNull] PolygonPoints polygonPoints,
		                     [NotNull] out string errorDescription,
		                     [CanBeNull] out IssueCode issueCode)
		{
			errorDescription = string.Empty;
			issueCode = null;

			int pointCount = polygonPoints.PointCount;

			bool singleExpectedCount = _minimumPointCount == _maximumPointCount;

			if (pointCount < _minimumPointCount)
			{
				string expectedDescription = singleExpectedCount
					                             ? "expected"
					                             : "minimum";

				if (polygonPoints.PointCountComplete)
				{
					errorDescription = string.Format("Incorrect point count: {0} ({1}: {2})",
					                                 pointCount, expectedDescription,
					                                 _minimumPointCount);
					issueCode = singleExpectedCount
						            ? Codes[Code.PointCount_SmallerThanExpected]
						            : Codes[Code.PointCount_SmallerThanMinimum];
					return true;
				}

				// there may be additional points outside the test perimeter, can't report as error
				return false;
			}

			if (pointCount > _maximumPointCount)
			{
				string expectedDescription = singleExpectedCount
					                             ? "expected"
					                             : "maximum";

				if (polygonPoints.PointCountComplete)
				{
					errorDescription = string.Format("Incorrect point count: {0} ({1}: {2})",
					                                 pointCount, expectedDescription,
					                                 _maximumPointCount);
					issueCode = singleExpectedCount
						            ? Codes[Code.PointCount_LargerThanExpected]
						            : Codes[Code.PointCount_LargerThanMaximum];
				}
				else
				{
					// TODO replace 'polygon' with 'polyline' if geometry is polyline
					errorDescription = string.Format(
						"Incorrect point count: {0} ({1}: {2}); " +
						"Note: the polygon exceeds the test perimeter, " +
						"there may be additional points outside the perimeter",
						pointCount, expectedDescription, _maximumPointCount);
				}

				return true;
			}

			return false;
		}

		private int CheckPolygonPoints(
			[NotNull] IEnumerable<PolygonPoints> polygonPointsToCheck)
		{
			Assert.ArgumentNotNull(polygonPointsToCheck, nameof(polygonPointsToCheck));

			Dictionary<int, List<PolygonPointsError>> errorsByTable =
				GetErrorsByTable(polygonPointsToCheck);

			int errorCount = 0;
			foreach (KeyValuePair<int, List<PolygonPointsError>> pair in errorsByTable)
			{
				int tableIndex = pair.Key;
				List<PolygonPointsError> errors = pair.Value;

				var featureClass = (IReadOnlyFeatureClass) InvolvedTables[tableIndex];
				Dictionary<long, PolygonPointsError> errorsByOid = GetErrorsByOid(errors);

				const bool recycle = true;
				foreach (
					IReadOnlyRow polygonRow in
					TableFilterUtils.GetRows(featureClass, errorsByOid.Keys, recycle))
				{
					IReadOnlyFeature polygonFeature = (IReadOnlyFeature) polygonRow;
					IGeometry errorGeometry = polygonFeature.ShapeCopy;

					PolygonPointsError error = errorsByOid[polygonFeature.OID];

					errorCount += ReportError(
						error.ErrorDescription, GetInvolvedRows(polygonFeature, error),
						errorGeometry, error.IssueCode, null);
				}
			}

			return errorCount;
		}

		[NotNull]
		private InvolvedRows GetInvolvedRows(
			[NotNull] IReadOnlyFeature polygonFeature,
			[NotNull] PolygonPointsError error)
		{
			InvolvedRows result = InvolvedRowUtils.GetInvolvedRows(polygonFeature);

			foreach (PointFeature pointFeature in error.PointFeatures)
			{
				IReadOnlyTable pointFeatureClass = InvolvedTables[pointFeature.TableIndex];

				result.Add(new InvolvedRow(pointFeatureClass.Name, pointFeature.OID));
			}

			return result;
		}

		[NotNull]
		private static Dictionary<long, PolygonPointsError> GetErrorsByOid(
			[NotNull] IEnumerable<PolygonPointsError> errors)
		{
			var result = new Dictionary<long, PolygonPointsError>();

			foreach (PolygonPointsError error in errors)
			{
				result.Add(error.OID, error);
			}

			return result;
		}

		[NotNull]
		private Dictionary<int, List<PolygonPointsError>> GetErrorsByTable(
			[NotNull] IEnumerable<PolygonPoints> polygonPointsToCheck)
		{
			var result = new Dictionary<int, List<PolygonPointsError>>();

			foreach (PolygonPoints polygonPoints in polygonPointsToCheck)
			{
				string description;
				IssueCode issueCode;
				if (! IsError(polygonPoints, out description, out issueCode))
				{
					continue;
				}

				List<PolygonPointsError> errors;
				if (! result.TryGetValue(polygonPoints.TableIndex, out errors))
				{
					errors = new List<PolygonPointsError>();
					result.Add(polygonPoints.TableIndex, errors);
				}

				errors.Add(new PolygonPointsError(polygonPoints, description, issueCode));
			}

			return result;
		}

		/// <summary>
		/// create a filter that gets the lines crossing the current row,
		/// with the same attribute constraints as the table
		/// </summary>
		private void InitFilter()
		{
			IList<IFeatureClassFilter> filters;
			IList<QueryFilterHelper> filterHelpers;

			_queryFilter = new IFeatureClassFilter[_totalClassesCount];
			_helper = new QueryFilterHelper[_totalClassesCount];

			// Create copy of this filter and use it for quering features
			CopyFilters(out filters, out filterHelpers);
			for (int i = 0; i < _totalClassesCount; i++)
			{
				_queryFilter[i] = filters[i];
				_helper[i] = filterHelpers[i];

				// use Contains when searching points
				if (i >= _polygonClassesCount)
				{
					_queryFilter[i].SpatialRelationship =
						_countPointOnPolygonBorder
							? esriSpatialRelEnum.esriSpatialRelIntersects
							: esriSpatialRelEnum.esriSpatialRelContains;
				}
			}
		}

		#endregion

		#region Nested types

		private class RelevantPointCondition : RowPairCondition
		{
			private const bool _isDirected = true;
			private const bool _undefinedConstraintIsFulfilled = true;
			private const string _row1Alias = "POINT";
			private const string _row2Alias = "POLYGON";

			public RelevantPointCondition([CanBeNull] string constraint,
			                              bool caseSensitive)
				: base(constraint, _isDirected, _undefinedConstraintIsFulfilled,
				       _row1Alias, _row2Alias, caseSensitive) { }
		}

		/// <summary>
		/// Represents a feature that has a remaining geometry that is not covered by the covering feature classes
		/// (in tiles processed so far)
		/// </summary>
		private class PolygonPoints
		{
			private readonly long _oid;
			private readonly int _tableIndex;
			private readonly IEnvelope _extent;

			private readonly SimpleSet<PointFeature> _pointFeatures =
				new SimpleSet<PointFeature>();

			private bool _pointCountComplete;

			/// <summary>
			/// Initializes a new instance of the <see cref="PolygonPoints"/> class.
			/// </summary>
			/// <param name="feature">The feature.</param>
			/// <param name="tableIndex">The table index for the feature</param>
			public PolygonPoints([NotNull] IReadOnlyFeature feature, int tableIndex)
			{
				Assert.ArgumentNotNull(feature, nameof(feature));

				_extent = feature.Extent;
				_tableIndex = tableIndex;
				_oid = feature.OID;
			}

			public int TableIndex => _tableIndex;

			public long OID => _oid;

			[NotNull]
			public IEnumerable<PointFeature> PointFeatures => _pointFeatures;

			public int PointCount => _pointFeatures.Count;

			public bool PointCountComplete
			{
				get { return _pointCountComplete; }
				set { _pointCountComplete = value; }
			}

			public bool IsFullyChecked([NotNull] IEnvelope tileEnvelope,
			                           [CanBeNull] IEnvelope testRunEnvelope)
			{
				return TestUtils.IsFeatureFullyChecked(_extent, tileEnvelope, testRunEnvelope);
			}

			public void AddPointFeature(IReadOnlyFeature feature, int tableIndex)
			{
				_pointFeatures.TryAdd(new PointFeature(feature, tableIndex));
			}
		}

		private class PolygonPointsError
		{
			private readonly PolygonPoints _polygonPoints;
			private readonly string _errorDescription;
			private readonly IssueCode _issueCode;

			public PolygonPointsError([NotNull] PolygonPoints polygonPoints,
			                          [NotNull] string errorDescription, IssueCode issueCode)
			{
				_polygonPoints = polygonPoints;
				_errorDescription = errorDescription;
				_issueCode = issueCode;
			}

			[NotNull]
			public IEnumerable<PointFeature> PointFeatures => _polygonPoints.PointFeatures;

			public long OID => _polygonPoints.OID;

			[NotNull]
			public string ErrorDescription => _errorDescription;

			public IssueCode IssueCode => _issueCode;
		}

		private class PointFeature : IEquatable<PointFeature>
		{
			private readonly long _oid;
			private readonly int _tableIndex;

			public PointFeature([NotNull] IReadOnlyFeature feature, int tableIndex)
			{
				_oid = feature.OID;
				_tableIndex = tableIndex;
			}

			public long OID => _oid;

			public int TableIndex => _tableIndex;

			public override string ToString()
			{
				return string.Format("OID: {0}, TableIndex: {1}", _oid, _tableIndex);
			}

			public bool Equals(PointFeature other)
			{
				if (ReferenceEquals(null, other))
				{
					return false;
				}

				if (ReferenceEquals(this, other))
				{
					return true;
				}

				return other._oid == _oid && other._tableIndex == _tableIndex;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != typeof(PointFeature))
				{
					return false;
				}

				return Equals((PointFeature) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (_oid.GetHashCode() * 397) ^ _tableIndex;
				}
			}
		}

		#endregion
	}
}
