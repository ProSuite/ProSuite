using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace EsriDE.ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[LinearNetworkTest]
	[TopologyTest]
	[UsedImplicitly]
	public class QaDangleCount : QaNetworkBase
	{
		[NotNull] private readonly IList<IFeatureClass> _polylineClasses;

		[NotNull] private readonly IDictionary<int, IDictionary<int, FeatureDangleCount>>
			_dangleCounts = new Dictionary<int, IDictionary<int, FeatureDangleCount>>();

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

		[Doc("QaDangleCount_0")]
		public QaDangleCount(
			[Doc("QaDangleCount_polylineClass")] [NotNull]
			IFeatureClass polylineClass,
			[Doc("QaDangleCount_dangleCountExpression")] [NotNull]
			string dangleCountExpression,
			[Doc("QaDangleCount_tolerance")] double tolerance)
			: this(new[] {polylineClass}, new[] {dangleCountExpression}, tolerance) { }

		[Doc("QaDangleCount_1")]
		public QaDangleCount(
			[Doc("QaDangleCount_polylineClasses")] [NotNull]
			IList<IFeatureClass>
				polylineClasses,
			[Doc("QaDangleCount_dangleCountExpressions")] [NotNull]
			IList<string>
				dangleCountExpressions,
			[Doc("QaDangleCount_tolerance")] double tolerance)
			: base(
				CastToTables((IEnumerable<IFeatureClass>) polylineClasses), tolerance,
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

		[NotNull]
		private List<TableView> GetDangleCountExpressions(
			[NotNull] IEnumerable<IFeatureClass> polylineClasses,
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
				foreach (IFeatureClass polylineClass in polylineClasses)
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
			[NotNull] IFeatureClass polylineClass,
			[NotNull] string dangleCountExpression)
		{
			TableView tableView = TableViewFactory.Create(
				(ITable) polylineClass,
				dangleCountExpression,
				false,
				GetSqlCaseSensitivity((ITable) polylineClass));

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
				KeyValuePair<int, IDictionary<int, FeatureDangleCount>> tablePair in
				_dangleCounts)
			{
				int tableIndex = tablePair.Key;

				var oidsToRemove = new List<int>();
				IDictionary<int, FeatureDangleCount> dangleCountPerOid = tablePair.Value;

				foreach (KeyValuePair<int, FeatureDangleCount> featurePair in
					dangleCountPerOid)
				{
					int oid = featurePair.Key;
					FeatureDangleCount dangleCount = featurePair.Value;

					// TODO revise for tolerance
					if ((dangleCount.XMax > tileXMax || dangleCount.YMax > tileYMax) &&
					    (dangleCount.XMax <= testRunXMax &&
					     dangleCount.YMax <= testRunYMax))
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

			return ReportError(description,
			                   GeometryFactory.CreateMultipoint(
				                   featureDangleCount.DanglingPoints),
			                   Codes[Code.InvalidNumberOfDangles],
			                   TestUtils.GetShapeFieldName(featureDangleCount.Feature),
			                   featureDangleCount.Feature);
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
					new[] {_dangleCountPlaceHolder},
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

			IDictionary<int, FeatureDangleCount> dangleCountPerFeature;
			if (! _dangleCounts.TryGetValue(tableIndex, out dangleCountPerFeature))
			{
				dangleCountPerFeature = new Dictionary<int, FeatureDangleCount>();

				_dangleCounts.Add(tableIndex, dangleCountPerFeature);
			}

			var feature = (IFeature) tableIndexRow.Row;
			int oid = feature.OID;

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

			public FeatureDangleCount([NotNull] IFeature feature,
			                          double xMax,
			                          double yMax)
			{
				Feature = feature;
				XMax = xMax;
				YMax = yMax;
			}

			[NotNull]
			public IFeature Feature { get; }

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
