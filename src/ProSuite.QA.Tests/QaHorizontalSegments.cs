using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports non-linear polycurve segments as errors
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaHorizontalSegments : ContainerTest
	{
		private readonly double _limitRad;
		private readonly double _toleranceRad;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NotSufficientlyHorizontal_Segment =
				"NotSufficientlyHorizontal.Segment";

			public const string NotSufficientlyHorizontal_ConsecutiveSegments =
				"NotSufficientlyHorizontal.ConsecutiveSegments";

			public Code() : base("HorizontalSegments") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaHorizontalSegments_0))]
		public QaHorizontalSegments(
			[Doc(nameof(DocStrings.QaHorizontalSegments_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaHorizontalSegments_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaHorizontalSegments_tolerance))]
			double tolerance)
			: base(featureClass)
		{
			_limitRad = MathUtils.ToRadians(limit);
			_toleranceRad = MathUtils.ToRadians(tolerance);

			AngleUnit = AngleUnit.Degree;
		}

		[InternallyUsedTest]
		public QaHorizontalSegments(
			[NotNull] QaHorizontalSegmentsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       definition.Limit, definition.Tolerance) { }

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
			using (
				SegmentSlopeAngleProvider segmentSlopeAngleProvider =
				GetSegmentSlopeAngleProvider((IReadOnlyFeature) row))
			{
				var errorCount = 0;
				var errorSegments = new List<SegmentSlopeAngle>();

				double maxAngle = 0;
				double maxZDifference = 0;

				SegmentSlopeAngle segmentSlopeAngle;
				while ((segmentSlopeAngle = segmentSlopeAngleProvider.ReadSegmentSlopeAngle()) !=
				       null)
				{
					double slopeAngle = segmentSlopeAngle.SlopeAngle;

					if (slopeAngle > _limitRad)
					{
						continue;
					}

					if (slopeAngle <= _toleranceRad)
					{
						continue;
					}

					if (errorSegments.Count > 0 &&
					    (errorSegments[0].PartIndex != segmentSlopeAngle.PartIndex ||
					     errorSegments[errorSegments.Count - 1].SegmentIndex !=
					     segmentSlopeAngle.SegmentIndex - 1))
					{
						errorCount += ReportError(segmentSlopeAngleProvider, errorSegments,
						                          maxAngle, maxZDifference, row);
						errorSegments.Clear();
						maxAngle = 0;
						maxZDifference = 0;
					}

					errorSegments.Add(segmentSlopeAngle);
					maxAngle = Math.Max(slopeAngle, maxAngle);
					maxZDifference = Math.Max(segmentSlopeAngle.ZDifference, maxZDifference);
				}

				if (errorSegments.Count > 0)
				{
					errorCount += ReportError(segmentSlopeAngleProvider, errorSegments,
					                          maxAngle, maxZDifference, row);
				}

				return errorCount;
			}
		}

		[NotNull]
		private static SegmentSlopeAngleProvider GetSegmentSlopeAngleProvider(
			[NotNull] IReadOnlyFeature row)
		{
			var segmentsFeature = row as IIndexedSegmentsFeature;
			if (segmentsFeature != null && segmentsFeature.AreIndexedSegmentsLoaded)
			{
				return new IndexedSegmentsSlopeAngleProvider(segmentsFeature.IndexedSegments);
			}

			var multiPatch = row.Shape as IMultiPatch;
			if (multiPatch != null)
			{
				return new IndexedSegmentsSlopeAngleProvider(
					ProxyUtils.CreateIndexedMultiPatch(multiPatch));
			}

			return new SegmentCollectionSlopeAngleProvider((ISegmentCollection) row.Shape);
		}

		private int ReportError(
			[NotNull] SegmentSlopeAngleProvider segmentSlopeAngleProvider,
			[NotNull] List<SegmentSlopeAngle> errorSegments,
			double maxAngleRad, double maxZDifference,
			[NotNull] IReadOnlyRow row)
		{
			int part = errorSegments[0].PartIndex;
			int startSegmentIndex = errorSegments[0].SegmentIndex;
			int endSegmentIndex = errorSegments[errorSegments.Count - 1].SegmentIndex;

			IPolyline line = segmentSlopeAngleProvider.GetSubpart(part, startSegmentIndex,
				endSegmentIndex);

			return ReportError(row, line, maxAngleRad, maxZDifference);
		}

		private int ReportError([NotNull] IReadOnlyRow row,
		                        [NotNull] IGeometry errorGeometry,
		                        double angleRad,
		                        double zDifference)
		{
			int pointCount = ((IPointCollection) errorGeometry).PointCount;

			double angleDeg = MathUtils.ToDegrees(angleRad);
			double toleranceDeg = MathUtils.ToDegrees(_toleranceRad);
			string format = FormatUtils.CompareFormat(angleDeg, ">", toleranceDeg, "N1");

			string description;
			IssueCode issueCode;

			if (pointCount <= 2)
			{
				description =
					string.Format(
						"The segment is almost, but not sufficently horizontal " +
						"(difference angle to horizontal: {0} > {1}, z-Difference = {2:N2})",
						FormatAngle(angleRad, format),
						FormatAngle(_toleranceRad, format),
						zDifference);
				issueCode = Codes[Code.NotSufficientlyHorizontal_Segment];
			}
			else
			{
				description =
					string.Format(
						"{0} consecutive segments are almost, but not sufficently horizontal " +
						"(max. difference angle to horizontal: {1} > {2}, max. z-Difference = {3:N2})",
						pointCount - 1,
						FormatAngle(angleRad, format), FormatAngle(_toleranceRad, format),
						zDifference);
				issueCode = Codes[Code.NotSufficientlyHorizontal_ConsecutiveSegments];
			}

			return ReportError(description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
			                   issueCode, TestUtils.GetShapeFieldName(row));
		}

		private class SegmentSlopeAngle
		{
			public readonly double SlopeAngle;
			public readonly double ZDifference;
			public readonly int PartIndex;
			public readonly int SegmentIndex;

			public SegmentSlopeAngle(double slopeAngle,
			                         double zDifference,
			                         int partIndex,
			                         int segmentIndex)
			{
				SlopeAngle = slopeAngle;
				ZDifference = zDifference;
				SegmentIndex = segmentIndex;
				PartIndex = partIndex;
			}
		}

		private abstract class SegmentSlopeAngleProvider : IDisposable
		{
			[CanBeNull]
			public SegmentSlopeAngle ReadSegmentSlopeAngle()
			{
				int partIndex;
				int segmentIndex;
				double length;
				double dz;
				bool hasNext = GetNextSegment(out length, out dz, out partIndex, out segmentIndex);

				if (! hasNext)
				{
					return null;
				}

				double slopeAngle = Math.Abs(length) < double.Epsilon
					                    ? Math.PI / 2
					                    : Math.Atan2(Math.Abs(dz), length);

				return new SegmentSlopeAngle(slopeAngle, Math.Abs(dz), partIndex, segmentIndex);
			}

			protected abstract bool GetNextSegment(out double length, out double dz,
			                                       out int partIndex, out int segmentIndex);

			public abstract void Dispose();

			[NotNull]
			public abstract IPolyline GetSubpart(int partIndex, int startSegmentIndex,
			                                     int endSegmentIndex);
		}

		private class IndexedSegmentsSlopeAngleProvider : SegmentSlopeAngleProvider
		{
			[NotNull] private readonly IIndexedSegments _indexedPolycurve;
			private readonly IEnumerator<SegmentProxy> _enumSegments;

			public IndexedSegmentsSlopeAngleProvider(
				[NotNull] IIndexedSegments indexedPolycurve)
			{
				_indexedPolycurve = indexedPolycurve;
				_enumSegments = _indexedPolycurve.GetSegments().GetEnumerator();
			}

			public override IPolyline GetSubpart(int partIndex, int startSegmentIndex,
			                                     int endSegmentIndex)
			{
				return _indexedPolycurve.GetSubpart(partIndex, startSegmentIndex, 0,
				                                    endSegmentIndex, 1);
			}

			protected override bool GetNextSegment(out double length, out double dz,
			                                       out int partIndex, out int segmentIndex)
			{
				if (! _enumSegments.MoveNext())
				{
					length = double.NaN;
					dz = double.NaN;
					partIndex = -1;
					segmentIndex = -1;
					return false;
				}

				SegmentProxy segment = Assert.NotNull(_enumSegments.Current);
				partIndex = segment.PartIndex;
				segmentIndex = segment.SegmentIndex;

				length = segment.Length;
				double z0 = segment.GetStart(true)[2];
				double z1 = segment.GetEnd(true)[2];
				dz = z1 - z0;

				return true;
			}

			public override void Dispose() { }
		}

		private class SegmentCollectionSlopeAngleProvider : SegmentSlopeAngleProvider
		{
			private readonly ISegmentCollection _segments;
			private readonly IEnumSegment _enumSegments;
			private readonly bool _isRecycling;

			public SegmentCollectionSlopeAngleProvider([NotNull] ISegmentCollection segments)
			{
				_segments = segments;
				_enumSegments = segments.EnumSegments;
				_isRecycling = _enumSegments.IsRecycling;
			}

			protected override bool GetNextSegment(out double length, out double dz,
			                                       out int partIndex, out int segmentIndex)
			{
				ISegment segment;
				partIndex = -1;
				segmentIndex = -1;
				_enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

				if (segment == null)
				{
					length = double.NaN;
					dz = double.NaN;
					return false;
				}

				length = segment.Length;

				double z0;
				double z1;
				((ISegmentZ) segment).GetZs(out z0, out z1);
				dz = z1 - z0;

				if (_isRecycling)
				{
					Marshal.ReleaseComObject(segment);
				}

				return true;
			}

			public override IPolyline GetSubpart(int partIndex, int startSegmentIndex,
			                                     int endSegmentIndex)
			{
				return ProxyUtils.GetSubpart(_segments, partIndex, startSegmentIndex,
				                             endSegmentIndex);
			}

			public override void Dispose()
			{
				Marshal.ReleaseComObject(_enumSegments);
			}
		}
	}
}
