using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
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
	[GeometryTest]
	public class QaInteriorRings : ContainerTest
	{
		private readonly int _maximumInteriorRingCount;
		private readonly string _shapeFieldName;
		private readonly ISpatialReference _spatialReference;

		private const double _defaultIgnoreInnerRingsLargerThan = -1;
		private const bool _defaultReportIndividualRings = false;
		private const bool _defaultReportOnlySmallestRingsExceedingMaximumCount = true;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string UnallowedInteriorRings = "UnallowedInteriorRings";

			public Code() : base("InteriorRings") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaInteriorRings_0))]
		public QaInteriorRings(
			[Doc(nameof(DocStrings.QaInteriorRings_polygonClass))] [NotNull]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaInteriorRings_maximumInteriorRingCount))]
			int maximumInteriorRingCount)
			: base(polygonClass)
		{
			Assert.ArgumentNotNull(polygonClass, nameof(polygonClass));
			Assert.ArgumentCondition(
				polygonClass.ShapeType == esriGeometryType.esriGeometryPolygon,
				"polygon feature class expected");

			_maximumInteriorRingCount = maximumInteriorRingCount;
			_shapeFieldName = polygonClass.ShapeFieldName;
			_spatialReference = polygonClass.SpatialReference;

			IgnoreInnerRingsLargerThan = _defaultIgnoreInnerRingsLargerThan;
			ReportIndividualRings = _defaultReportIndividualRings;
			ReportOnlySmallestRingsExceedingMaximumCount =
				_defaultReportOnlySmallestRingsExceedingMaximumCount;
		}

		[InternallyUsedTest]
		public QaInteriorRings(
			[NotNull] QaInteriorRingsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.PolygonClass,
			       definition.MaximumInteriorRingCount)
		{
			IgnoreInnerRingsLargerThan = definition.IgnoreInnerRingsLargerThan;
			ReportIndividualRings = definition.ReportIndividualRings;
			ReportOnlySmallestRingsExceedingMaximumCount =
				definition.ReportOnlySmallestRingsExceedingMaximumCount;
		}

		[TestParameter(_defaultIgnoreInnerRingsLargerThan)]
		[Doc(nameof(DocStrings.QaInteriorRings_IgnoreInnerRingsLargerThan))]
		[UsedImplicitly]
		public double IgnoreInnerRingsLargerThan { get; set; }

		[TestParameter(_defaultReportIndividualRings)]
		[Doc(nameof(DocStrings.QaInteriorRings_ReportIndividualRings))]
		public bool ReportIndividualRings { get; set; }

		[TestParameter(_defaultReportOnlySmallestRingsExceedingMaximumCount)]
		[Doc(nameof(DocStrings.QaInteriorRings_ReportOnlySmallestRingsExceedingMaximumCount))]
		public bool ReportOnlySmallestRingsExceedingMaximumCount { get; set; }

		public override bool IsQueriedTable(int tableIndex)
		{
			AssertValidInvolvedTableIndex(tableIndex);
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			var polygon = feature.Shape as IPolygon;
			if (polygon == null || polygon.IsEmpty)
			{
				return NoError;
			}

			var geometryCollection = polygon as IGeometryCollection;
			if (geometryCollection == null || geometryCollection.GeometryCount == 0)
			{
				return NoError;
			}

			List<IRing> allInteriorRings = GeometryUtils.GetRings(polygon)
			                                            .Where(ring => ! ring.IsExterior)
			                                            .ToList();

			int totalInteriorRingCount = allInteriorRings.Count;

			List<IRing> relevantInteriorRings =
				IgnoreInnerRingsLargerThan >= 0
					? allInteriorRings.Where(ring => ! IsIgnored(ring))
					                  .ToList()
					: allInteriorRings;

			return relevantInteriorRings.Count > _maximumInteriorRingCount
				       ? ReportErrors(row, relevantInteriorRings, polygon, totalInteriorRingCount)
				       : NoError;
		}

		private int ReportErrors([NotNull] IReadOnlyRow row,
		                         [NotNull] ICollection<IRing> relevantInteriorRings,
		                         [NotNull] IPolygon polygon,
		                         int totalInteriorRingCount)
		{
			if (ReportIndividualRings)
			{
				int errorCount = 0;
				foreach (IRing ring in GetErrorRings(relevantInteriorRings))
				{
					errorCount += ReportError(
						GetIndividualErrorDescription(ring, relevantInteriorRings,
						                              totalInteriorRingCount),
						InvolvedRowUtils.GetInvolvedRows(row),
						GetIndividualErrorGeometry(polygon, ring),
						Codes[Code.UnallowedInteriorRings], _shapeFieldName);
				}

				return errorCount;
			}

			return ReportError(
				GetCombinedErrorDescription(relevantInteriorRings, totalInteriorRingCount),
				InvolvedRowUtils.GetInvolvedRows(row),
				GetCombinedErrorGeometry(polygon, relevantInteriorRings),
				Codes[Code.UnallowedInteriorRings], _shapeFieldName);
		}

		private string GetCombinedErrorDescription(
			[NotNull] ICollection<IRing> relevantInteriorRings,
			int totalInteriorRingCount)
		{
			if (IgnoreInnerRingsLargerThan <= 0)
			{
				// settings used in previous releases -- don't change description for allowed error compatibility
				return string.Format(
					"Polygon has {0} interior ring(s), the maximum allowed number of interior rings is {1}",
					relevantInteriorRings.Count,
					_maximumInteriorRingCount);
			}

			// there is an area limit

			if (_maximumInteriorRingCount <= 0)
			{
				return string.Format(
					relevantInteriorRings.Count == 1
						? "Polygon has {0} interior ring(s), of which {1} is smaller than the minimum area ({2})"
						: "Polygon has {0} interior ring(s), of which {1} are smaller than the minimum area ({2})",
					totalInteriorRingCount,
					relevantInteriorRings.Count,
					IgnoreInnerRingsLargerThan);
			}

			return string.Format(
				relevantInteriorRings.Count == 1
					? "Polygon has {0} interior ring(s), of which {1} is smaller than the minimum area ({2}); " +
					  "the maximum allowed number of interior rings is {3}"
					: "Polygon has {0} interior ring(s), of which {1} are smaller than the minimum area ({2}); " +
					  "the maximum allowed number of interior rings is {3}",
				totalInteriorRingCount,
				relevantInteriorRings.Count,
				IgnoreInnerRingsLargerThan,
				_maximumInteriorRingCount);
		}

		[NotNull]
		private string GetIndividualErrorDescription(
			[NotNull] IRing ring,
			[NotNull] ICollection<IRing> relevantInteriorRings,
			int totalInteriorRingCount)
		{
			if (IgnoreInnerRingsLargerThan <= 0)
			{
				if (_maximumInteriorRingCount > 0)
				{
					return string.Format(
						"Polygon has {0} interior ring(s). The maximum allowed number of interior rings is {1}",
						relevantInteriorRings.Count,
						_maximumInteriorRingCount);
				}

				return string.Format("Polygon has {0} interior ring(s)",
				                     relevantInteriorRings.Count);
			}

			// there is an area limit

			if (_maximumInteriorRingCount <= 0)
			{
				return string.Format(
					relevantInteriorRings.Count == 1
						? "Polygon has {0} interior ring(s), of which {1} is smaller than the minimum area. Area of this ring is {2}"
						: "Polygon has {0} interior ring(s), of which {1} are smaller than the minimum area. Area of this ring is {2}",
					totalInteriorRingCount,
					relevantInteriorRings.Count,
					FormatAreaComparison(GetArea(ring), "<", IgnoreInnerRingsLargerThan,
					                     _spatialReference));
			}

			return string.Format(
				relevantInteriorRings.Count == 1
					? "Polygon has {0} interior ring(s), of which {1} is smaller than the minimum area. Area of this ring is {2}. " +
					  "The maximum allowed number of smaller interior rings is {3}"
					: "Polygon has {0} interior ring(s), of which {1} are smaller than the minimum area. Area of this ring is {2}. " +
					  "The maximum allowed number of smaller interior rings is {3}",
				totalInteriorRingCount,
				relevantInteriorRings.Count,
				FormatAreaComparison(GetArea(ring), "<", IgnoreInnerRingsLargerThan,
				                     _spatialReference), _maximumInteriorRingCount);
		}

		[NotNull]
		private static IGeometry GetIndividualErrorGeometry([NotNull] IPolygon polygon,
		                                                    [NotNull] IRing ring)
		{
			return CreatePolygonFromRings(new[] { ring }, polygon);
		}

		[NotNull]
		private IGeometry GetCombinedErrorGeometry([NotNull] IPolygon polygon,
		                                           [NotNull] ICollection<IRing> rings)
		{
			IGeometry result = CreatePolygonFromRings(GetErrorRings(rings), polygon);

			return result.IsEmpty
				       ? polygon
				       : result;
		}

		[NotNull]
		private IEnumerable<IRing> GetErrorRings([NotNull] ICollection<IRing> rings)
		{
			if (! ReportOnlySmallestRingsExceedingMaximumCount ||
			    _maximumInteriorRingCount == 0)
			{
				return rings;
			}

			int reportedRingsCount = rings.Count - _maximumInteriorRingCount;

			return TestUtils.GetSmallestRings(rings, reportedRingsCount);
		}

		[NotNull]
		private static IGeometry CreatePolygonFromRings([NotNull] IEnumerable<IRing> rings,
		                                                [NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));
			Assert.ArgumentNotNull(rings, nameof(rings));

			IPolygon result = GeometryFactory.CreatePolygon(polygon.SpatialReference,
			                                                GeometryUtils.IsZAware(polygon),
			                                                GeometryUtils.IsMAware(polygon));

			var geometryCollection = (IGeometryCollection) result;

			object missing = Type.Missing;
			foreach (IRing ring in rings)
			{
				geometryCollection.AddGeometry(GeometryFactory.Clone(ring),
				                               ref missing,
				                               ref missing);
			}

			const bool allowReorder = true;
			GeometryUtils.Simplify(result, allowReorder);

			return result;
		}

		private bool IsIgnored([NotNull] IRing ring)
		{
			return IgnoreInnerRingsLargerThan > 0 && GetArea(ring) > IgnoreInnerRingsLargerThan;
		}

		private static double GetArea([NotNull] IRing ring)
		{
			return ring.IsEmpty
				       ? 0
				       : Math.Abs(((IArea) ring).Area);
		}
	}
}
