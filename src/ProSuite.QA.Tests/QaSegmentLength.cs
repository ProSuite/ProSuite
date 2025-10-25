using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check whether the segments of a polyline/a polygon are bigger than a limit.
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaSegmentLength : ContainerTest
	{
		private readonly bool _is3D;
		private readonly double _limit;

		// TODO add parameter for LargerThan, SmallerThan: or: add two specific tests (QaMinSegmentLength, QaMaxSegmentLength)

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string SmallerThanLimit_OneSegment =
				"SmallerThanLimit.OneSegment";

			public const string SmallerThanLimit_ConsecutiveSegments =
				"SmallerThanLimit.ConsecutiveSegments";

			public Code() : base("SegmentLength") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSegmentLength_0))]
		public QaSegmentLength(
			[Doc(nameof(DocStrings.QaSegmentLength_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSegmentLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSegmentLength_is3D))]
			bool is3D)
			: base(featureClass)
		{
			_limit = limit;
			_is3D = is3D;

			NumberFormat = "N0";
		}

		[Doc(nameof(DocStrings.QaSegmentLength_0))]
		public QaSegmentLength(
			[Doc(nameof(DocStrings.QaSegmentLength_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSegmentLength_limit))]
			double limit)
			: this(
				featureClass, limit,
				featureClass.ShapeType == esriGeometryType.esriGeometryMultiPatch) { }

		[InternallyUsedTest]
		public QaSegmentLength(
			[NotNull] QaSegmentLengthDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass, definition.Limit,
			       definition.Is3D) { }

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
			using (SegmentLengthProvider
			       provider = GetSegmentLengthProvider((IReadOnlyFeature) row))
			{
				int errorCount = 0;
				var errorSegments = new List<SegmentLength>();

				double maxLength = 0;
				SegmentLength segmentLength;
				while ((segmentLength = provider.ReadSegmentLength()) != null)
				{
					double length = segmentLength.Length;

					if (length >= _limit)
					{
						continue;
					}

					if (errorSegments.Count > 0 &&
					    (errorSegments[0].PartIndex != segmentLength.PartIndex ||
					     errorSegments[errorSegments.Count - 1].SegmentIndex !=
					     segmentLength.SegmentIndex - 1))
					{
						errorCount += ReportError(provider, errorSegments, maxLength,
						                          row);
						errorSegments.Clear();
						maxLength = 0;
					}

					errorSegments.Add(segmentLength);
					maxLength = Math.Max(length, maxLength);
				}

				if (errorSegments.Count > 0)
				{
					errorCount += ReportError(provider, errorSegments, maxLength, row);
				}

				return errorCount;
			}
		}

		[NotNull]
		private SegmentLengthProvider GetSegmentLengthProvider(IReadOnlyFeature row)
		{
			SegmentLengthProvider provider;

			var segmentsFeature = row as IIndexedSegmentsFeature;
			if (segmentsFeature != null && segmentsFeature.AreIndexedSegmentsLoaded)
			{
				provider = new IndexedSegmentsLengthProvider(segmentsFeature.IndexedSegments,
				                                             _is3D);
			}
			else if (row.Shape is IMultiPatch)
			{
				IIndexedSegments indexedSegments =
					ProxyUtils.CreateIndexedMultiPatch((IMultiPatch) row.Shape);
				provider = new IndexedSegmentsLengthProvider(indexedSegments, _is3D);
			}
			else
			{
				provider = new SegmentCollectionLengthProvider((ISegmentCollection) row.Shape,
				                                               _is3D);
			}

			return provider;
		}

		private int ReportError(
			[NotNull] SegmentLengthProvider segmentLengthProvider,
			[NotNull] List<SegmentLength> errorSegments,
			double minSegmentLength, [NotNull] IReadOnlyRow row)
		{
			int part = errorSegments[0].PartIndex;
			int startSegmentIndex = errorSegments[0].SegmentIndex;
			int endSegmentIndex = errorSegments[errorSegments.Count - 1].SegmentIndex;
			IPolyline line = segmentLengthProvider.GetSubpart(part, startSegmentIndex,
			                                                  endSegmentIndex);

			return ReportError(row, line, minSegmentLength);
		}

		private int ReportError([NotNull] IReadOnlyRow row,
		                        [NotNull] IGeometry errorGeometry,
		                        double minLength)
		{
			int pointCount = ((IPointCollection) errorGeometry).PointCount;

			string description;
			IssueCode issueCode;

			if (pointCount <= 2)
			{
				description = string.Format("Segment length {0}",
				                            FormatLengthComparison(minLength, "<", _limit,
					                            errorGeometry.SpatialReference));
				issueCode = Codes[Code.SmallerThanLimit_OneSegment];
			}
			else
			{
				description = string.Format("{0} consecutive segment lengths {1}",
				                            pointCount - 1,
				                            FormatLengthComparison(minLength, "<", _limit,
					                            errorGeometry.SpatialReference,
					                            "(min {0}) < {2}"));
				issueCode = Codes[Code.SmallerThanLimit_ConsecutiveSegments];
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
				issueCode, TestUtils.GetShapeFieldName(row));
		}

		private class SegmentLength
		{
			public readonly double Length;
			public readonly int PartIndex;
			public readonly int SegmentIndex;

			public SegmentLength(double length, int partIndex, int segmentIndex)
			{
				Length = length;
				SegmentIndex = segmentIndex;
				PartIndex = partIndex;
			}
		}

		private abstract class SegmentLengthProvider : IDisposable
		{
			private readonly bool _is3D;

			protected SegmentLengthProvider(bool is3D)
			{
				_is3D = is3D;
			}

			public SegmentLength ReadSegmentLength()
			{
				int partIndex;
				int segmentIndex;
				double length;
				bool hasNext = GetNextSegment(_is3D, out length, out partIndex, out segmentIndex);

				return hasNext
					       ? new SegmentLength(length, partIndex, segmentIndex)
					       : null;
			}

			protected abstract bool GetNextSegment(bool as3D, out double length,
			                                       out int partIndex, out int segmentIndex);

			public abstract void Dispose();

			[NotNull]
			public abstract IPolyline GetSubpart(int partIndex, int startSegmentIndex,
			                                     int endSegmentIndex);
		}

		private class IndexedSegmentsLengthProvider : SegmentLengthProvider
		{
			[NotNull] private readonly IIndexedSegments _indexedPolycurve;
			[NotNull] private readonly IEnumerator<SegmentProxy> _enumSegments;

			public IndexedSegmentsLengthProvider([NotNull] IIndexedSegments indexedPolycurve,
			                                     bool is3D)
				: base(is3D)
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

			protected override bool GetNextSegment(bool as3D, out double length,
			                                       out int partIndex, out int segmentIndex)
			{
				if (! _enumSegments.MoveNext())
				{
					length = double.NaN;
					partIndex = -1;
					segmentIndex = -1;
					return false;
				}

				SegmentProxy segment = Assert.NotNull(_enumSegments.Current);

				partIndex = segment.PartIndex;
				segmentIndex = segment.SegmentIndex;

				length = segment.Length;
				if (as3D)
				{
					double z0 = segment.GetStart(true)[2];
					double z1 = segment.GetEnd(true)[2];
					double dz = z1 - z0;
					length = Math.Sqrt(length * length + dz * dz);
				}

				return true;
			}

			public override void Dispose() { }
		}

		private class SegmentCollectionLengthProvider : SegmentLengthProvider
		{
			[NotNull] private readonly ISegmentCollection _segments;
			[NotNull] private readonly IEnumSegment _enumSegments;
			private readonly bool _isRecycling;

			public SegmentCollectionLengthProvider([NotNull] ISegmentCollection segments, bool is3D)
				: base(is3D)
			{
				_segments = segments;
				_enumSegments = segments.EnumSegments;
				_isRecycling = _enumSegments.IsRecycling;
			}

			protected override bool GetNextSegment(bool as3D, out double length,
			                                       out int partIndex, out int segmentIndex)
			{
				ISegment segment;
				partIndex = -1;
				segmentIndex = -1;
				_enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

				if (segment == null)
				{
					length = double.NaN;
					return false;
				}

				length = segment.Length;
				if (as3D)
				{
					double z0;
					double z1;
					((ISegmentZ) segment).GetZs(out z0, out z1);
					double dz = z1 - z0;

					length = Math.Sqrt(length * length + dz * dz);
				}

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
