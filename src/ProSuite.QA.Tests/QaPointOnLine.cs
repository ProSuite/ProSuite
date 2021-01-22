using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[ProximityTest]
	public class QaPointOnLine : ContainerTest
	{
		private IEnvelope _box;
		private IList<ISpatialFilter> _filter;
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

		[Doc("QaPointOnLine_0")]
		public QaPointOnLine(
			[NotNull] [Doc("QaPointOnLine_pointClass")]
			IFeatureClass pointClass,
			[NotNull] [Doc("QaPointOnLine_nearClasses")]
			IList<IFeatureClass> nearClasses,
			[Doc("QaPointOnLine_near")] double near)
			: base(CastToTables(new[] {pointClass}, nearClasses))
		{
			_spatialReference = ((IGeoDataset) pointClass).SpatialReference;
			SearchDistance = near;
			_filter = null;

			_tableCount = InvolvedTables.Count;
			_nearPoint = new PointClass();
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
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

			var feature = row as IFeature;

			if (feature == null)
			{
				return NoError;
			}

			var point = (IPoint) feature.Shape;

			bool near = false;
			for (int involvedTableIndex = 1;
			     involvedTableIndex < _tableCount;
			     involvedTableIndex++)
			{
				var featureClass = (IFeatureClass) InvolvedTables[involvedTableIndex];
				_helper[involvedTableIndex].MinimumOID = -1;

				if (! CheckTable(point, featureClass, involvedTableIndex))
				{
					continue;
				}

				near = true;
				break;
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

			return ReportError(description, error,
			                   Codes[Code.PointNotNearLine],
			                   TestUtils.GetShapeFieldName(feature),
			                   feature);
		}

		private bool CheckTable([NotNull] IPoint point,
		                        [NotNull] IFeatureClass neighbor,
		                        int tableIndex)
		{
			ISpatialFilter filter = _filter[tableIndex];

			point.QueryEnvelope(_box);
			_box.Expand(SearchDistance, SearchDistance, false);

			filter.Geometry = _box;

			foreach (IRow row in
				Search((ITable) neighbor, _filter[tableIndex], _helper[tableIndex], point))
			{
				var neighborFeature = (IFeature) row;
				double distance = GetDistance(point, neighborFeature);

				if (distance <= SearchDistance)
				{
					return true;
				}
			}

			return false;
		}

		private double GetDistance([NotNull] IPoint point,
		                           [NotNull] IFeature neighborFeature)
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
			foreach (ISpatialFilter filter in _filter)
			{
				filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}

			foreach (QueryFilterHelper filterHelper in _helper)
			{
				filterHelper.ForNetwork = true;
			}

			_box = new EnvelopeClass();
		}
	}
}
