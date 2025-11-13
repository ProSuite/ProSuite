using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaPointOnLine : ContainerTest
	{
		private IEnvelope _box;
		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;

		private readonly IPoint _nearPoint;
		private readonly int _tableCount;
		private readonly ISpatialReference _spatialReference;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PointNotNearLine = "PointNotNearLine";

			public Code() : base("PointOnLine") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaPointOnLine_0))]
		public QaPointOnLine(
			[NotNull] [Doc(nameof(DocStrings.QaPointOnLine_pointClass))]
			IReadOnlyFeatureClass pointClass,
			[NotNull] [Doc(nameof(DocStrings.QaPointOnLine_nearClasses))]
			IList<IReadOnlyFeatureClass> nearClasses,
			[Doc(nameof(DocStrings.QaPointOnLine_near))]
			double near)
			: base(CastToTables(new[] { pointClass }, nearClasses))
		{
			_spatialReference = pointClass.SpatialReference;
			SearchDistance = near;
			_filter = null;

			_tableCount = InvolvedTables.Count;
			_nearPoint = new PointClass();
		}

		[InternallyUsedTest]
		public QaPointOnLine([NotNull] QaPointOnLineDefinition definition)
			: this((IReadOnlyFeatureClass) definition.PointClass,
			       definition.NearClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.Near) { }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			// preparing
			if (_filter == null)
			{
				InitFilter();
			}

			if (tableIndex > 0)
			{
				return NoError;
			}

			var feature = row as IReadOnlyFeature;

			if (feature == null)
			{
				return NoError;
			}

			IEnumerable<IPoint> points = GetPoints(feature);

			bool near = false;

			foreach (IPoint point in points)
			{
				for (int involvedTableIndex = 1;
				     involvedTableIndex < _tableCount;
				     involvedTableIndex++)
				{
					var featureClass = (IReadOnlyFeatureClass) InvolvedTables[involvedTableIndex];
					_helper[involvedTableIndex].MinimumOID = -1;

					if (! CheckTable(point, featureClass, involvedTableIndex))
					{
						continue;
					}

					near = true;
					break;
				}
			}

			if (near)
			{
				return NoError;
			}

			var error = (IPoint) feature.ShapeCopy;

			string description =
				string.Format(
					"Point does not lie closer than {0} to any (border)-line",
					FormatLength(SearchDistance, _spatialReference));

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), error,
				Codes[Code.PointNotNearLine], TestUtils.GetShapeFieldName(feature));
		}

		private static IEnumerable<IPoint> GetPoints(IReadOnlyFeature feature)
		{
			IGeometry geometry = feature.Shape;

			if (geometry.GeometryType == esriGeometryType.esriGeometryPoint)
			{
				yield return (IPoint) geometry;
			}
			else if (geometry.GeometryType == esriGeometryType.esriGeometryMultipoint)
			{
				var multiPoint = (IPointCollection) geometry;
				for (int i = 0; i < multiPoint.PointCount; i++)
				{
					yield return multiPoint.get_Point(i);
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException(
					$"Invalid input geometry type for {feature.FeatureClass.Name}: {geometry.GeometryType}");
			}
		}

		private bool CheckTable([NotNull] IPoint point,
		                        [NotNull] IReadOnlyFeatureClass neighbor,
		                        int tableIndex)
		{
			IFeatureClassFilter filter = _filter[tableIndex];

			point.QueryEnvelope(_box);
			_box.Expand(SearchDistance, SearchDistance, false);

			filter.FilterGeometry = _box;

			foreach (IReadOnlyRow row in
			         Search(neighbor, _filter[tableIndex], _helper[tableIndex]))
			{
				var neighborFeature = (IReadOnlyFeature) row;
				double distance = GetDistance(point, neighborFeature);

				if (distance <= SearchDistance)
				{
					return true;
				}
			}

			return false;
		}

		private double GetDistance([NotNull] IPoint point,
		                           [NotNull] IReadOnlyFeature neighborFeature)
		{
			double along = 0;
			double distance = 0;
			bool rightSide = false;
			const bool asRatio = false;

			var neighborCurve = (ICurve) neighborFeature.Shape;

			neighborCurve.QueryPointAndDistance(esriSegmentExtension.esriNoExtension,
			                                    point, asRatio, _nearPoint,
			                                    ref along, ref distance, ref rightSide);

			return distance;
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

			_box = new EnvelopeClass();
		}
	}
}
