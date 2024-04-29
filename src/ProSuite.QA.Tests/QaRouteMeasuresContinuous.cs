using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
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
	public class QaRouteMeasuresContinuous : ContainerTest
	{
		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;
		private readonly IPoint _fromPoint = new PointClass();
		private readonly IPoint _toPoint = new PointClass();
		private readonly IPoint _searchFromPoint = new PointClass();
		private readonly IPoint _searchToPoint = new PointClass();

		private readonly IList<int> _routeIdFieldIndexes;
		private readonly IList<double> _mTolerances;

		private readonly int _totalClassCount;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string DifferentMeasuresAtLineConnection_MDifferenceExceedsTolerance =
				"DifferentMeasuresAtLineConnection.MDifferenceExceedsTolerance";

			public const string DifferentMeasuresAtLineConnection_OneValueIsNaN =
				"DifferentMeasuresAtLineConnection.OneValueIsNaN";

			public Code() : base("RouteMeasuresContinuous") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_0))]
		public QaRouteMeasuresContinuous(
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_polylineClass))] [NotNull]
			IReadOnlyFeatureClass
				polylineClass,
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_routeIdField))] [NotNull]
			string routeIdField)
			: this(new[] {polylineClass}, new[] {routeIdField}) { }

		[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_1))]
		public QaRouteMeasuresContinuous(
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_polylineClasses))] [NotNull]
			ICollection<IReadOnlyFeatureClass>
				polylineClasses,
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_routeIdFields))] [NotNull]
			IEnumerable<string>
				routeIdFields)
			: base(CastToTables(polylineClasses))
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentNotNull(routeIdFields, nameof(routeIdFields));

			_totalClassCount = polylineClasses.Count;

			_routeIdFieldIndexes = TestUtils.GetFieldIndexes(polylineClasses, routeIdFields);
			_mTolerances = TestUtils.GetMTolerances(polylineClasses);
		}

		[InternallyUsedTest]
		public QaRouteMeasuresContinuous([NotNull] QaRouteMeasuresContinuousDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.RouteIdFields) { }

		#region Overrides of ContainerTest

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			var polyline = feature.Shape as IPolyline;
			if (polyline == null)
			{
				return NoError;
			}

			var mAware = (IMAware) polyline;
			if (! mAware.MAware)
			{
				return NoError;
			}

			// preparing
			if (_filter == null)
			{
				InitFilter();
			}

			int errorCount = 0;

			// continuity at line connections within route

			for (int searchTableIndex = tableIndex;
			     searchTableIndex < _totalClassCount;
			     searchTableIndex++)
			{
				_helper[searchTableIndex].MinimumOID = IgnoreUndirected &&
				                                       tableIndex == searchTableIndex
					                                       ? row.OID
					                                       : -1;

				errorCount += FindErrors(row, polyline, tableIndex, searchTableIndex);
			}

			return errorCount;
		}

		[NotNull]
		private static IEnumerable<IPoint> GetEndpoints([NotNull] IPolyline polyline,
		                                                [NotNull] IPoint fromPointTemplate,
		                                                [NotNull] IPoint toPointTemplate)
		{
			polyline.QueryFromPoint(fromPointTemplate);
			polyline.QueryToPoint(toPointTemplate);

			yield return fromPointTemplate;
			yield return toPointTemplate;
		}

		private int FindErrors([NotNull] IReadOnlyRow row,
		                       [NotNull] IPolyline polyline,
		                       int tableIndex,
		                       int searchTableIndex)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			IReadOnlyTable searchTable = InvolvedTables[searchTableIndex];

			IFeatureClassFilter searchFilter = _filter[searchTableIndex];
			QueryFilterHelper searchFilterHelper = _helper[searchTableIndex];

			int errorCount = 0;

			int routeFieldIndex = _routeIdFieldIndexes[tableIndex];

			object routeId = row.get_Value(routeFieldIndex);
			if (routeId == null || routeId == DBNull.Value)
			{
				return NoError;
			}

			foreach (IPoint endPoint in GetEndpoints(polyline, _fromPoint, _toPoint))
			{
				searchFilter.FilterGeometry = endPoint;

				foreach (IReadOnlyRow searchRow in Search(searchTable, searchFilter,
				                                          searchFilterHelper))
				{
					errorCount += FindErrors(endPoint, row, routeId,
					                         tableIndex, searchRow, searchTableIndex);
				}
			}

			return errorCount;
		}

		private int FindErrors([NotNull] IPoint endPoint,
		                       [NotNull] IReadOnlyRow row,
		                       [NotNull] object routeId,
		                       int tableIndex,
		                       [NotNull] IReadOnlyRow searchRow,
		                       int searchTableIndex)
		{
			var searchFeature = searchRow as IReadOnlyFeature;
			if (searchFeature == null)
			{
				return NoError;
			}

			var searchPolyline = searchFeature.Shape as IPolyline;
			if (searchPolyline == null)
			{
				return NoError;
			}

			object searchRouteId = searchRow.get_Value(_routeIdFieldIndexes[searchTableIndex]);
			if (! Equals(routeId, searchRouteId))
			{
				return NoError;
			}

			int errorCount = 0;

			var endPointRelOp = (IRelationalOperator) endPoint;

			foreach (
				IPoint searchEndPoint in
				GetEndpoints(searchPolyline, _searchFromPoint, _searchToPoint))
			{
				if (endPointRelOp.Disjoint(searchEndPoint))
				{
					continue;
				}

				double m = endPoint.M;
				double searchM = searchEndPoint.M;

				if (double.IsNaN(m) && double.IsNaN(searchM))
				{
					// both NaN --> continuous (?)
					continue;
				}

				if (double.IsNaN(m) || double.IsNaN(searchM))
				{
					// one M value is NaN, the other isn't
					string description = string.Format(
						"M values are not continuous at line connection within route {0} (NaN / not NaN)",
						routeId);
					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row, searchRow),
						GeometryFactory.Clone(endPoint),
						Codes[Code.DifferentMeasuresAtLineConnection_OneValueIsNaN], null);
				}
				else
				{
					// both M values not NaN
					double mDifference = Math.Abs(m - searchM);
					double tolerance = Math.Max(_mTolerances[tableIndex],
					                            _mTolerances[searchTableIndex]);
					if (mDifference > tolerance)
					{
						string description = string.Format(
							"M values are not continuous at line connection within route {0}. The M difference is {1}",
							routeId, mDifference);
						errorCount += ReportError(
							description, InvolvedRowUtils.GetInvolvedRows(row, searchRow),
							GeometryFactory.Clone(endPoint),
							Codes[
								Code.DifferentMeasuresAtLineConnection_MDifferenceExceedsTolerance],
							null);
					}
				}
			}

			return errorCount;
		}

		private void InitFilter()
		{
			CopyFilters(out _filter, out _helper);
			foreach (var filter in _filter)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
			}

			foreach (QueryFilterHelper filterHelper in _helper)
			{
				filterHelper.ForNetwork = true;
			}
		}

		#endregion
	}
}
