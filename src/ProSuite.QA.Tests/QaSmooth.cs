using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check that the discretized second derivative of the slope angle does not
	/// exceed a certain maximum. This means that there are no to abrupt changes in
	/// the slope angle.
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaSmooth : ContainerTest
	{
		private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;

		private readonly double _limitCstr;

		private double _limitRad;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string AbruptChangeInSlopeAngle = "AbruptChangeInSlopeAngle";

			public Code() : base("SmoothZProfile") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSmooth_0))]
		public QaSmooth(
			[Doc(nameof(DocStrings.QaSmooth_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSmooth_limit))]
			double limit)
			: base(featureClass)
		{
			_limitCstr = limit;
		}

		[InternallyUsedTest]
		public QaSmooth([NotNull] QaSmoothDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass, definition.Limit)
		{
			AngularUnit = definition.AngularUnit;
		}

		[TestParameter(_defaultAngularUnit)]
		[Doc(nameof(DocStrings.QaSmooth_AngularUnit))]
		public AngleUnit AngularUnit
		{
			get { return AngleUnit; }
			set
			{
				AngleUnit = value;
				_limitRad = -1; // gets initialized in next ExecuteCore() 
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (_limitRad <= 0)
			{
				_limitRad = FormatUtils.AngleInUnits2Radians(_limitCstr, AngularUnit);
			}

			IGeometry shape = ((IReadOnlyFeature) row).Shape;

			switch (shape.GeometryType)
			{
				case esriGeometryType.esriGeometryPolyline:
					return CheckAllSegments((ISegmentCollection) shape, row);

				case esriGeometryType.esriGeometryPolygon:
					return CheckPolygon((IPolygon) shape, row);

				default:
					return NoError;
			}
		}

		private int CheckPolygon([NotNull] IPolygon polygon, [NotNull] IReadOnlyRow row)
		{
			int errorCount = 0;

			var threeSegments = new ISegment[3];

			foreach (IRing ring in GeometryUtils.GetRings(polygon))
			{
				var segments = (ISegmentCollection) ring;
				int segmentCount = segments.SegmentCount;

				errorCount += CheckAllSegments(segments, row);

				//Rand des Rings
				threeSegments[0] = segments.Segment[segmentCount - 1];
				threeSegments[1] = segments.Segment[0];
				threeSegments[2] = segments.Segment[1];

				errorCount += CheckThreeSegments(threeSegments, row);

				threeSegments[0] = segments.Segment[segmentCount - 2];
				threeSegments[1] = segments.Segment[segmentCount - 1];
				threeSegments[2] = segments.Segment[0];

				errorCount += CheckThreeSegments(threeSegments, row);

				Marshal.ReleaseComObject(ring);
			}

			return errorCount;
		}

		private int CheckAllSegments([NotNull] ISegmentCollection segments,
		                             [NotNull] IReadOnlyRow row)
		{
			int errorCount = 0;
			var threeSegments = new ISegment[3];

			int segmentCount = segments.SegmentCount;

			for (int segmentIndex = 1; segmentIndex < segmentCount - 1; segmentIndex++)
			{
				for (int j = 0; j < 3; j++)
				{
					threeSegments[j] = segments.Segment[segmentIndex + j - 1];
				}

				errorCount += CheckThreeSegments(threeSegments, row);
			}

			return errorCount;
		}

		/// <summary>
		/// looks if the three input segments are smooth
		/// </summary>
		/// <param name="threeSegments">Input segments. Only the first three segments will be used.</param>
		/// <param name="row"></param>
		/// <returns>Are segments smooth?</returns>
		private int CheckThreeSegments([NotNull] IList<ISegment> threeSegments,
		                               [NotNull] IReadOnlyRow row)
		{
			double anglechange = GeometryMathUtils.CalculateSmoothness(threeSegments[0],
				threeSegments[1],
				threeSegments[2]);

			if (Math.Abs(anglechange) <= _limitRad)
			{
				return NoError;
			}

			string description = string.Format("Smoothness parameter {0:N4} > {1:N4}",
			                                   Math.Abs(anglechange),
			                                   _limitRad); // TODO: use AngularUnit

			IGeometry errorGeometry = GetErrorGeometry(threeSegments[1]);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
				Codes[Code.AbruptChangeInSlopeAngle], TestUtils.GetShapeFieldName(row));
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] ISegment segment)
		{
			Assert.ArgumentNotNull(segment, nameof(segment));

			object missing = Type.Missing;

			PolylineClass result = ProxyUtils.CreatePolyline(segment);

			((ISegmentCollection) result).AddSegment(segment, ref missing, ref missing);

			return result;
		}
	}
}
