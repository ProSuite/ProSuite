using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	[IntersectionParameterTest]
	public class QaMinIntersect : QaSpatialRelationSelfBase
	{
		private readonly double _limit;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string SmallIntersectionArea = "SmallIntersectionArea";

			public Code() : base("MinIntersect") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMinIntersect_0))]
		public QaMinIntersect(
			[Doc(nameof(DocStrings.QaMinIntersect_polygonClasses))]
			IList<IReadOnlyFeatureClass> polygonClasses,
			[Doc(nameof(DocStrings.QaMinIntersect_limit))]
			double limit)
			: base(polygonClasses, esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			_limit = limit;
		}

		[Doc(nameof(DocStrings.QaMinIntersect_1))]
		public QaMinIntersect(
			[Doc(nameof(DocStrings.QaMinIntersect_polygonClass))]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaMinIntersect_limit))]
			double limit)
			: this(new[] {polygonClass}, limit) { }

		[InternallyUsedTest]
		public QaMinIntersect(QaMinIntersectDefinition definition)
			: this(definition.PolygonClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.Limit)
		{ }

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			if (row1 == row2)
			{
				return NoError;
			}

			IGeometry shape1 = ((IReadOnlyFeature) row1).Shape;
			IGeometry shape2 = ((IReadOnlyFeature) row2).Shape;

			var intersection = ((ITopologicalOperator) shape1).Intersect(
				                   shape2,
				                   esriGeometryDimension.esriGeometry2Dimension) as IPolygon;

			if (intersection == null || intersection.IsEmpty)
			{
				return NoError;
			}

			if (GeometryUtils.GetExteriorRingCount(intersection, allowSimplify: false) == 1)
			{
				return CheckIntersectionArea(intersection, row1, row2, shape2);
			}

			// more than one exterior ring:
			var errorCount = 0;

			foreach (IGeometry intersectionPart in GeometryUtils.Explode(intersection))
			{
				errorCount += CheckIntersectionArea(intersectionPart, row1, row2, shape2);
			}

			return errorCount;
		}

		private int CheckIntersectionArea([NotNull] IGeometry intersection,
		                                  [NotNull] IReadOnlyRow row1,
		                                  [NotNull] IReadOnlyRow row2,
		                                  [NotNull] IGeometry shape2)
		{
			if (intersection.IsEmpty)
			{
				return NoError;
			}

			var intersectionArea = intersection as IArea;
			if (intersectionArea == null)
			{
				return NoError;
			}

			double area = intersectionArea.Area;

			if (area >= _limit)
			{
				return NoError;
			}

			string description = string.Format("Intersect area {0}",
			                                   FormatAreaComparison(area, "<", _limit,
				                                   shape2.SpatialReference));

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row1, row2),
				intersection, Codes[Code.SmallIntersectionArea],
				TestUtils.GetShapeFieldName(row1));
		}
	}
}
