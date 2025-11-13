using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[LinearNetworkTest]
	[MValuesTest]
	public class QaRouteMeasuresUnique : NonContainerTest
	{
		private readonly IList<int> _routeIdFieldIndexes;
		private readonly IList<double> _mTolerances;
		private readonly IList<double> _xyTolerances;
		private readonly List<IReadOnlyFeatureClass> _polylineClasses;

		private readonly int _totalClassCount;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string MeasuresNotUnique_WithinFeature =
				"MeasuresNotUnique.WithinFeature";

			public const string MeasuresNotUnique_WithinRoute = "MeasuresNotUnique.WithinRoute";

			public Code() : base("RouteMeasuresUnique") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaRouteMeasuresUnique_0))]
		public QaRouteMeasuresUnique(
			[Doc(nameof(DocStrings.QaRouteMeasuresUnique_polylineClass))] [NotNull]
			IReadOnlyFeatureClass
				polylineClass,
			[Doc(nameof(DocStrings.QaRouteMeasuresUnique_routeIdField))] [NotNull]
			string
				routeIdField)
			: this(new[] {polylineClass}, new[] {routeIdField}) { }

		[Doc(nameof(DocStrings.QaRouteMeasuresUnique_1))]
		public QaRouteMeasuresUnique(
			[Doc(nameof(DocStrings.QaRouteMeasuresUnique_polylineClasses))] [NotNull]
			ICollection<IReadOnlyFeatureClass>
				polylineClasses,
			[Doc(nameof(DocStrings.QaRouteMeasuresUnique_routeIdFields))] [NotNull]
			IEnumerable<string>
				routeIdFields)
			: base(CastToTables(polylineClasses))
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentNotNull(routeIdFields, nameof(routeIdFields));

			_polylineClasses = new List<IReadOnlyFeatureClass>(polylineClasses);

			_totalClassCount = polylineClasses.Count;

			_routeIdFieldIndexes = TestUtils.GetFieldIndexes(polylineClasses, routeIdFields);
			_mTolerances = TestUtils.GetMTolerances(polylineClasses);
			_xyTolerances = TestUtils.GetXyTolerances(polylineClasses);
		}

		[InternallyUsedTest]
		public QaRouteMeasuresUnique([NotNull] QaRouteMeasuresUniqueDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.RouteIdFields) { }

		public override int Execute()
		{
			return CheckFeatures(GetFeatures((IGeometry) null));
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return CheckFeatures(GetFeatures(boundingBox));
		}

		public override int Execute(IPolygon area)
		{
			return CheckFeatures(GetFeatures(area));
		}

		public override int Execute(IEnumerable<IReadOnlyRow> selectedRows)
		{
			return CheckFeatures(GetFeatures(selectedRows.Cast<IReadOnlyFeature>()));
		}

		public override int Execute(IReadOnlyRow row)
		{
			return CheckFeatures(GetFeatures(new[] {(IReadOnlyFeature) row}));
		}

		protected override ISpatialReference GetSpatialReference()
		{
			return DatasetUtils.GetUniqueSpatialReference(_polylineClasses);
		}

		private int GetTableIndex([NotNull] IReadOnlyRow row)
		{
			int tableCount = InvolvedTables.Count;

			for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
			{
				IReadOnlyTable table = InvolvedTables[tableIndex];

				if (table == row.Table)
				{
					return tableIndex;
				}
			}

			return -1;
		}

		[NotNull]
		private IEnumerable<KeyValuePair<int, IReadOnlyFeature>> GetFeatures(
			[NotNull] IEnumerable<IReadOnlyFeature> features)
		{
			Assert.ArgumentNotNull(features, nameof(features));

			foreach (IReadOnlyFeature feature in features)
			{
				int tableIndex = GetTableIndex(feature);

				if (tableIndex >= 0)
				{
					yield return new KeyValuePair<int, IReadOnlyFeature>(tableIndex, feature);
				}
			}
		}

		[NotNull]
		private IEnumerable<KeyValuePair<int, IReadOnlyFeature>> GetFeatures(
			[CanBeNull] IGeometry geometry)
		{
			for (var tableIndex = 0; tableIndex < _totalClassCount; tableIndex++)
			{
				IReadOnlyFeatureClass featureClass = _polylineClasses[tableIndex];

				ITableFilter queryFilter = GetQueryFilter(featureClass, tableIndex, geometry);

				const bool recycle = true;
				foreach (IReadOnlyRow feature in
				         featureClass.EnumRows(queryFilter, recycle))
				{
					yield return new KeyValuePair<int, IReadOnlyFeature>(
						tableIndex, (IReadOnlyFeature) feature);
				}
			}
		}

		private int CheckFeatures(
			[NotNull] IEnumerable<KeyValuePair<int, IReadOnlyFeature>> features)
		{
			Assert.ArgumentNotNull(features, nameof(features));

			var routeMeasures = new RouteMeasures(_mTolerances, _xyTolerances);

			var errorCount = 0;

			foreach (KeyValuePair<int, IReadOnlyFeature> pair in features)
			{
				int tableIndex = pair.Key;
				IReadOnlyFeature feature = pair.Value;

				if (CancelTestingRow(feature))
				{
					continue;
				}

				object routeId = feature.get_Value(_routeIdFieldIndexes[tableIndex]);

				if (routeId == null || routeId is DBNull)
				{
					continue;
				}

				foreach (CurveMeasureRange range in
				         MeasureUtils.GetMeasureRanges(feature, tableIndex))
				{
					routeMeasures.Add(routeId, range);
				}

				errorCount += ReportNonUniqueMeasuresWithinFeature(feature);
			}

			errorCount += routeMeasures.GetOverlaps().Sum(overlap => ReportOverlap(overlap));

			return errorCount;
		}

		[NotNull]
		private static IEnumerable<esriMonotinicityEnum> GetInvalidMonotonicityTypes(
			[NotNull] IPolyline polyline)
		{
			esriMonotinicityEnum trend = MeasureUtils.GetMonotonicityTrend(polyline);

			switch (trend)
			{
				case esriMonotinicityEnum.esriValueIncreases:
					yield return esriMonotinicityEnum.esriValueDecreases;
					break;

				case esriMonotinicityEnum.esriValueDecreases:
					yield return esriMonotinicityEnum.esriValueIncreases;
					break;

				case esriMonotinicityEnum.esriValueLevel:
					yield return esriMonotinicityEnum.esriValueIncreases;
					yield return esriMonotinicityEnum.esriValueDecreases;
					break;

				case esriMonotinicityEnum.esriValuesEmpty:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private int ReportNonUniqueMeasuresWithinFeature([NotNull] IReadOnlyFeature feature)
		{
			IGeometry geometry = feature.Shape;
			if (geometry == null || geometry.IsEmpty)
			{
				return NoError;
			}

			var polyline = geometry as IPolyline;

			if (polyline == null)
			{
				return NoError;
			}

			var errorCount = 0;

			if (MeasureUtils.ContainsAllMonotonicityTypes(
				    polyline,
				    esriMonotinicityEnum.esriValueDecreases,
				    esriMonotinicityEnum.esriValueIncreases))
			{
				IEnumerable<esriMonotinicityEnum> invalidTypes =
					GetInvalidMonotonicityTypes(polyline);

				const string description =
					"Measures are not unique due to non-monotonicity within feature";

				errorCount += MeasureUtils.GetMonotonicitySequences(polyline, invalidTypes)
				                          .Sum(sequence => ReportError(
					                               description,
					                               InvolvedRowUtils.GetInvolvedRows(feature),
					                               sequence.CreatePolyline(),
					                               Codes[Code.MeasuresNotUnique_WithinFeature],
					                               TestUtils.GetShapeFieldName(feature)));
			}

			return errorCount;
		}

		private int ReportOverlap([NotNull] OverlappingMeasures overlap)
		{
			Assert.ArgumentNotNull(overlap, nameof(overlap));

			IPolyline lineErrorGeometry;
			IMultipoint pointErrorGeometry;
			InvolvedRows involvedRows = GetInvolvedRows(overlap,
			                                            out lineErrorGeometry,
			                                            out pointErrorGeometry);

			string description =
				string.Format(
					"Measures are not unique for route with id = '{0}'; from M: {1} to M: {2}",
					overlap.RouteId, overlap.MMin, overlap.MMax);

			var errorCount = 0;

			if (lineErrorGeometry != null)
			{
				errorCount += ReportError(
					description, involvedRows, lineErrorGeometry,
					Codes[Code.MeasuresNotUnique_WithinRoute], null);
			}

			if (pointErrorGeometry != null)
			{
				errorCount += ReportError(
					description, involvedRows, pointErrorGeometry,
					Codes[Code.MeasuresNotUnique_WithinRoute], null);
			}

			if (lineErrorGeometry == null && pointErrorGeometry == null)
			{
				// unable to determine error geometry, but there are involved features -> report them anyway
				errorCount += ReportError(description, null,
				                          Codes[Code.MeasuresNotUnique_WithinRoute], null,
				                          involvedRows);
			}

			return errorCount;
		}

		[NotNull]
		private InvolvedRows GetInvolvedRows(
			[NotNull] OverlappingMeasures overlap,
			[CanBeNull] out IPolyline lineErrorGeometry,
			[CanBeNull] out IMultipoint pointErrorGeometry)
		{
			Assert.ArgumentNotNull(overlap, nameof(overlap));

			var involvedRows = new InvolvedRows();
			var polylines = new List<IPolyline>();
			var allPoints = new List<IPoint>();

			foreach (TestRowReference testRowReference in overlap.Features)
			{
				IReadOnlyFeature feature = GetFeature(testRowReference);

				involvedRows.AddRange(InvolvedRowUtils.GetInvolvedRows(feature));

				var polyline = (IPolyline) feature.Shape;

				IList<IPoint> points;
				IPolyline subcurves = MeasureUtils.GetSubcurves(polyline,
				                                                overlap.MMin,
				                                                overlap.MMax,
				                                                out points);

				allPoints.AddRange(points);
				if (subcurves != null)
				{
					polylines.Add(subcurves);
				}
			}

			lineErrorGeometry = GetErrorGeometry(polylines);
			pointErrorGeometry = allPoints.Count > 0
				                     ? GeometryFactory.CreateMultipoint(allPoints)
				                     : null;

			return involvedRows;
		}

		[CanBeNull]
		private static IPolyline GetErrorGeometry([NotNull] IList<IPolyline> polylines)
		{
			switch (polylines.Count)
			{
				case 0:
					return null;

				case 1:
					return polylines[0];

				default:
					return (IPolyline) GeometryUtils.Union(polylines);
			}
		}

		[NotNull]
		private IReadOnlyFeature GetFeature([NotNull] TestRowReference testRowReference)
		{
			Assert.ArgumentNotNull(testRowReference, nameof(testRowReference));

			IReadOnlyTable table = InvolvedTables[testRowReference.TableIndex];

			IReadOnlyRow row = table.GetRow(testRowReference.ObjectId);
			Assert.NotNull(row, "Row {0} not found in {1}",
			               testRowReference.ObjectId,
			               table.Name);

			return (IReadOnlyFeature) row;
		}

		[NotNull]
		private ITableFilter GetQueryFilter([NotNull] IReadOnlyFeatureClass featureClass,
		                                    int tableIndex,
		                                    [CanBeNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			string routeIdFieldName = GetRouteIdFieldName(featureClass, tableIndex);

			ITableFilter result;
			if (geometry == null)
			{
				result = new AoTableFilter();
			}
			else
			{
				result = new AoFeatureClassFilter
				         {
					         SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects,
					         FilterGeometry = geometry
				         };
			}

			result.WhereClause = GetWhereClause(tableIndex, routeIdFieldName);

			TableFilterUtils.SetSubFields(result,
			                              featureClass.OIDFieldName,
			                              featureClass.ShapeFieldName,
			                              routeIdFieldName);

			return result;
		}

		[NotNull]
		private string GetWhereClause(int tableIndex,
		                              [NotNull] string routeIdFieldName)
		{
			string filterExpression = GetConstraint(tableIndex);
			string routeIdConstraint = string.Format("{0} IS NOT NULL", routeIdFieldName);

			return StringUtils.IsNotEmpty(filterExpression)
				       ? string.Format("({0}) AND {1}", filterExpression, routeIdConstraint)
				       : routeIdConstraint;
		}

		[NotNull]
		private string GetRouteIdFieldName([NotNull] IReadOnlyFeatureClass featureClass,
		                                   int tableIndex)
		{
			return featureClass.Fields.Field[_routeIdFieldIndexes[tableIndex]].Name;
		}
	}
}
