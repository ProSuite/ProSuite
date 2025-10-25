using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests
{
	[LinearNetworkTest]
	[TopologyTest]
	[UsedImplicitly]
	public class QaDangleCount : QaNetworkBase
	{
		[NotNull] private readonly IList<IReadOnlyFeatureClass> _polylineClasses;

		[NotNull] private readonly IDictionary<int, IDictionary<long, FeatureDangleCount>>
			_dangleCounts = new Dictionary<int, IDictionary<long, FeatureDangleCount>>();

		[NotNull] private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();
		[NotNull] private readonly List<string> _dangleCountExpressionsSql;
		[CanBeNull] private List<TableView> _dangleCountExpressions;

		private const string _dangleCountPlaceHolder = "_DangleCount";

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InvalidNumberOfDangles = "InvalidNumberOfDangles";

			public Code() : base("DangleCount") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaDangleCount_0))]
		public QaDangleCount(
			[Doc(nameof(DocStrings.QaDangleCount_polylineClass))] [NotNull]
			IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaDangleCount_dangleCountExpression))] [NotNull]
			string dangleCountExpression,
			[Doc(nameof(DocStrings.QaDangleCount_tolerance))]
			double tolerance)
			: this(new[] { polylineClass }, new[] { dangleCountExpression }, tolerance) { }

		[Doc(nameof(DocStrings.QaDangleCount_1))]
		public QaDangleCount(
			[Doc(nameof(DocStrings.QaDangleCount_polylineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				polylineClasses,
			[Doc(nameof(DocStrings.QaDangleCount_dangleCountExpressions))] [NotNull]
			IList<string>
				dangleCountExpressions,
			[Doc(nameof(DocStrings.QaDangleCount_tolerance))]
			double tolerance)
			: base(
				CastToTables((IEnumerable<IReadOnlyFeatureClass>) polylineClasses), tolerance,
				false, null
			)
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentNotNull(dangleCountExpressions,
			                       nameof(dangleCountExpressions));
			Assert.ArgumentCondition(
				dangleCountExpressions.Count == 1 ||
				dangleCountExpressions.Count == polylineClasses.Count,
				"The number of dangle count expressions must be either 1 or equal to the number of polyline classes");
			Assert.ArgumentCondition(tolerance >= 0, "Invalid tolerance: {0}", tolerance);

			_polylineClasses = polylineClasses.ToList();
			_dangleCountExpressionsSql = dangleCountExpressions.ToList();
		}

		[InternallyUsedTest]
		public QaDangleCount(QaDangleCountDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.DangleCountExpressions,
			       definition.Tolerance
			) { }

		[NotNull]
		private List<TableView> GetDangleCountExpressions(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> polylineClasses,
			[NotNull] IList<string> dangleCountExpressions)
		{
			var result = new List<TableView>();
			if (dangleCountExpressions.Count == 1)
			{
				string expression = dangleCountExpressions[0];
				result.AddRange(
					polylineClasses.Select(
						c => CreateDangleCountExpression(c, expression)));
			}
			else
			{
				int tableIndex = 0;
				foreach (IReadOnlyFeatureClass polylineClass in polylineClasses)
				{
					result.Add(CreateDangleCountExpression(
						           polylineClass,
						           dangleCountExpressions[tableIndex]));
					tableIndex++;
				}
			}

			return result;
		}

		[NotNull]
		private TableView CreateDangleCountExpression(
			[NotNull] IReadOnlyFeatureClass polylineClass,
			[NotNull] string dangleCountExpression)
		{
			TableView tableView = TableViewFactory.Create(
				polylineClass,
				dangleCountExpression,
				false,
				GetSqlCaseSensitivity(polylineClass));

			tableView.AddColumn(_dangleCountPlaceHolder, typeof(int));

			tableView.Constraint = dangleCountExpression;

			return tableView;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			int errorCount = base.CompleteTileCore(args);

			if (ConnectedElementsList == null)
			{
				return errorCount;
			}

			IEnvelope allBox = args.AllBox;
			if (allBox == null)
			{
				return errorCount;
			}

			IEnvelope tileEnvelope = args.CurrentEnvelope;

			if (tileEnvelope == null)
			{
				return errorCount;
			}

			double tileXMax = tileEnvelope.XMax;
			double tileYMax = tileEnvelope.YMax;
			double testRunXMax = allBox.XMax;
			double testRunYMax = allBox.YMax;

			foreach (List<NetElement> connectedRows in ConnectedElementsList)
			{
				UpdateDangleCount(connectedRows);
			}

			foreach (
				KeyValuePair<int, IDictionary<long, FeatureDangleCount>> tablePair in
				_dangleCounts)
			{
				int tableIndex = tablePair.Key;

				var oidsToRemove = new List<long>();
				IDictionary<long, FeatureDangleCount> dangleCountPerOid = tablePair.Value;

				foreach (KeyValuePair<long, FeatureDangleCount> featurePair in
				         dangleCountPerOid)
				{
					long oid = featurePair.Key;
					FeatureDangleCount dangleCount = featurePair.Value;

					// TODO revise for tolerance
					if ((dangleCount.XMax > tileXMax || dangleCount.YMax > tileYMax) &&
					    dangleCount.XMax <= testRunXMax && dangleCount.YMax <= testRunYMax)
					{
						continue;
					}

					errorCount += CheckDangles(tableIndex, dangleCount);

					oidsToRemove.Add(oid);
				}

				foreach (int oid in oidsToRemove)
				{
					dangleCountPerOid.Remove(oid);
				}
			}

			return errorCount;
		}

		private int CheckDangles(int tableIndex,
		                         [NotNull] FeatureDangleCount featureDangleCount)
		{
			string constraintValues;
			return IsAllowedDangleCount(tableIndex, featureDangleCount,
			                            out constraintValues)
				       ? 0
				       : ReportError(featureDangleCount, constraintValues);
		}

		private int ReportError([NotNull] FeatureDangleCount featureDangleCount,
		                        [CanBeNull] string constraintValues)
		{
			string description = string.IsNullOrEmpty(constraintValues)
				                     ? string.Format("Invalid number of dangles: {0}",
				                                     featureDangleCount
					                                     .DanglingPointCount)
				                     : string.Format(
					                     "Invalid number of dangles: {0} ({1})",
					                     featureDangleCount.DanglingPointCount,
					                     constraintValues);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(featureDangleCount.Feature),
				GeometryFactory.CreateMultipoint(
					featureDangleCount.DanglingPoints),
				Codes[Code.InvalidNumberOfDangles],
				TestUtils.GetShapeFieldName(featureDangleCount.Feature));
		}

		private bool IsAllowedDangleCount(int tableIndex,
		                                  [NotNull] FeatureDangleCount featureDangleCount,
		                                  out string constraintValues)
		{
			if (_dangleCountExpressions == null)
			{
				_dangleCountExpressions = GetDangleCountExpressions(_polylineClasses,
					_dangleCountExpressionsSql);
			}

			TableView tableView = _dangleCountExpressions[tableIndex];

			tableView.ClearRows();
			DataRow dataRow = Assert.NotNull(tableView.Add(featureDangleCount.Feature));

			dataRow[_dangleCountPlaceHolder] = featureDangleCount.DanglingPointCount;

			bool matchesConstraint = tableView.FilteredRowCount == 1;
			const bool constraintOnly = true;
			if (matchesConstraint)
			{
				constraintValues = string.Empty;
			}
			else
			{
				var addedColumnNames = new SimpleSet<string>(
					new[] { _dangleCountPlaceHolder },
					StringComparer.InvariantCultureIgnoreCase);

				constraintValues = tableView.ToString(featureDangleCount.Feature,
				                                      constraintOnly,
				                                      addedColumnNames);
			}

			return matchesConstraint;
		}

		private void UpdateDangleCount([NotNull] IList<NetElement> connectedRows)
		{
			if (connectedRows.Count != 1)
			{
				return;
			}

			var directedRow = connectedRows[0] as DirectedRow;
			if (directedRow == null)
			{
				return;
			}

			// we found an edge end point not connected to any other feature --> a dangle

			TableIndexRow feature = directedRow.Row;

			AddDanglingEndpoint(feature, directedRow.NetPoint);
		}

		private void AddDanglingEndpoint([NotNull] TableIndexRow tableIndexRow,
		                                 [NotNull] IPoint endPoint)
		{
			int tableIndex = tableIndexRow.TableIndex;

			IDictionary<long, FeatureDangleCount> dangleCountPerFeature;
			if (! _dangleCounts.TryGetValue(tableIndex, out dangleCountPerFeature))
			{
				dangleCountPerFeature = new Dictionary<long, FeatureDangleCount>();

				_dangleCounts.Add(tableIndex, dangleCountPerFeature);
			}

			var feature = (IReadOnlyFeature) tableIndexRow.Row;
			long oid = feature.OID;

			FeatureDangleCount dangleCount;
			if (! dangleCountPerFeature.TryGetValue(oid, out dangleCount))
			{
				feature.Shape.QueryEnvelope(_envelopeTemplate);

				dangleCount = new FeatureDangleCount(feature,
				                                     _envelopeTemplate.XMax,
				                                     _envelopeTemplate.YMax);

				dangleCountPerFeature[oid] = dangleCount;
			}

			dangleCount.AddDanglingPoint(endPoint);
		}

		private class FeatureDangleCount
		{
			[NotNull] private readonly List<IPoint> _danglingPoints = new List<IPoint>();

			public FeatureDangleCount([NotNull] IReadOnlyFeature feature,
			                          double xMax,
			                          double yMax)
			{
				Feature = feature;
				XMax = xMax;
				YMax = yMax;
			}

			[NotNull]
			public IReadOnlyFeature Feature { get; }

			[NotNull]
			public IEnumerable<IPoint> DanglingPoints => _danglingPoints;

			public void AddDanglingPoint([NotNull] IPoint endPoint)
			{
				_danglingPoints.Add(endPoint);
			}

			public int DanglingPointCount => _danglingPoints.Count;

			public double XMax { get; }

			public double YMax { get; }
		}
	}
}
