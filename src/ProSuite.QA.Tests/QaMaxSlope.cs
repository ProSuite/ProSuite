using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Determine that no slope exceeds a certain limit
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaMaxSlope : ContainerTest
	{
		private readonly double _limit;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string SlopeTooSteep = "SlopeTooSteep";

			public Code() : base("MaxSlope") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMaxSlope_0))]
		public QaMaxSlope(
			[Doc(nameof(DocStrings.QaMaxSlope_featureClass))] IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMaxSlope_limit))] double limit)
			: base(featureClass)
		{
			_limit = limit;
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			IGeometry shape = ((IReadOnlyFeature) row).Shape;

			if (! (shape is IPolycurve))
			{
				return 0;
			}

			var segments = (ISegmentCollection) shape;

			return CheckSegments(segments, row);
		}

		private int CheckSegments([NotNull] ISegmentCollection segments,
		                          [NotNull] IReadOnlyRow row)
		{
			IEnumSegment enumSegments = segments.EnumSegments;
			enumSegments.Reset();

			ISegment segment;
			int partIndex = -1;
			int segmentIndex = -1;
			enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
			bool recycling = enumSegments.IsRecycling;

			int errorCount = 0;
			while (segment != null)
			{
				double slopeRadians = GeometryMathUtils.CalculateSlope(segment);

				if (slopeRadians > _limit)
				{
					string description = string.Format(
						"Slope angle {0} > {1}", FormatAngle(slopeRadians, "N2"),
						FormatAngle(_limit, "N2"));

					IPolyline errorGeometry = GeometryFactory.CreatePolyline(segment);

					errorCount += ReportError(description, errorGeometry,
					                          Codes[Code.SlopeTooSteep],
					                          TestUtils.GetShapeFieldName(row),
					                          new object[] {MathUtils.ToDegrees(slopeRadians)},
					                          row);
				}

				if (recycling)
				{
					// release the segment, otherwise "pure virtual function call" occurs 
					// when there are certain circular arcs (IsLine == true ?)
					Marshal.ReleaseComObject(segment);
				}

				enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
			}

			return errorCount;
		}
	}
}
