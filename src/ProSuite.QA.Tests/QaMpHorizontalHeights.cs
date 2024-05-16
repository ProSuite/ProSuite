using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports horizontal with almost near azimuth
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpHorizontalHeights : ContainerTest
	{
		private readonly double _nearHeight;
		private readonly double _heightTolerance;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string HeightsSimilarButNotEqual_TwoSegments =
				"HeightsSimilarButNotEqual.TwoSegments";

			public const string HeightsSimilarButNotEqual_SegmentGroup =
				"HeightsSimilarButNotEqual.SegmentGroup";

			public Code() : base("MpHorizontalHeights") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMpHorizontalHeights_0))]
		public QaMpHorizontalHeights(
			[Doc(nameof(DocStrings.QaMpHorizontalHeights_multiPatchClass))] [NotNull]
			IReadOnlyFeatureClass
				multiPatchClass,
			[Doc(nameof(DocStrings.QaMpHorizontalHeights_nearHeight))]
			double nearHeight,
			[Doc(nameof(DocStrings.QaMpHorizontalHeights_heightTolerance))]
			double heightTolerance)
			: base(multiPatchClass)
		{
			_nearHeight = nearHeight;
			_heightTolerance = heightTolerance;
		}

		[InternallyUsedTest]
		public QaMpHorizontalHeights(
			[NotNull] QaMpHorizontalHeightsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.MultiPatchClass,
			       definition.NearHeight,
			       definition.HeightTolerance) { }

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
			var feature = row as IReadOnlyFeature;

			var multiPatch = feature?.Shape as IMultiPatch;
			if (multiPatch == null)
			{
				return NoError;
			}

			var errorSegments = new List<HeightSegmentPair>();
			HeightSegmentPairProvider heightSegmentPairProvider =
				GetHeightSegmentPairProvider(feature);

			foreach (HeightSegmentPair heightSegmentPair in
			         heightSegmentPairProvider.ReadHeightSegmentPair())
			{
				double heightDifference = heightSegmentPair.HeightDifference;
				if (heightDifference <= _heightTolerance)
				{
					continue;
				}

				errorSegments.Add(heightSegmentPair);
			}

			return ReportErrors(errorSegments, row);
		}

		[NotNull]
		private HeightSegmentPairProvider GetHeightSegmentPairProvider(
			[NotNull] IReadOnlyFeature feature)
		{
			var indexedFeature = feature as IIndexedMultiPatchFeature;
			IIndexedMultiPatch multiPatch = indexedFeature?.IndexedMultiPatch ??
			                                ProxyUtils.CreateIndexedMultiPatch(
				                                (IMultiPatch) feature.Shape);

			return new MultipatchHeightSegmentPairProvider(_nearHeight, multiPatch);
		}

		private int ReportErrors([NotNull] IEnumerable<HeightSegmentPair> errorSegments,
		                         [NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;
			var unhandledPairs = new List<HeightSegmentPair>(errorSegments);

			while (unhandledPairs.Count > 0)
			{
				var relatedPairs = new List<HeightSegmentPair> { unhandledPairs[0] };
				unhandledPairs.RemoveAt(0);

				SegmentPairUtils.AddRelatedPairsRecursive(relatedPairs, unhandledPairs);

				errorCount += ReportError(relatedPairs, row);
			}

			return errorCount;
		}

		private int ReportError([NotNull] IList<HeightSegmentPair> relatedPairs,
		                        [NotNull] IReadOnlyRow row)
		{
			if (relatedPairs.Count == 0)
			{
				return NoError;
			}

			double maxHeightOffset = -1;
			foreach (HeightSegmentPair heightSegmentPair in relatedPairs)
			{
				double heightOffset = Math.Abs(heightSegmentPair.HeightDifference);
				if (heightOffset > maxHeightOffset)
				{
					maxHeightOffset = heightOffset;
				}
			}

			ISpatialReference sr = ((IReadOnlyFeature) row).Shape.SpatialReference;
			string description;
			string offsetDescription = FormatLengthComparison(maxHeightOffset, ">",
			                                                  _heightTolerance, sr);
			IssueCode issueCode;

			if (relatedPairs.Count == 1)
			{
				description =
					string.Format(
						"The two segments have a similar, but not equal height (height difference: {0})",
						offsetDescription);
				issueCode = Codes[Code.HeightsSimilarButNotEqual_TwoSegments];
			}
			else
			{
				description =
					string.Format(
						"Segments in this group have similar, but not equal heights (maximum height difference: {0})",
						offsetDescription);
				issueCode = Codes[Code.HeightsSimilarButNotEqual_SegmentGroup];
			}

			IGeometry errorGeometry = SegmentPairUtils.CreateGeometry(relatedPairs);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
				issueCode, TestUtils.GetShapeFieldName(row));
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class HeightSegmentPair : ISegmentPair
		{
			[NotNull] private readonly HeightSegment _baseSegment;
			[NotNull] private readonly HeightSegment _nearSegment;

			public HeightSegmentPair([NotNull] HeightSegment baseSegment,
			                         [NotNull] HeightSegment nearSegment)
			{
				_baseSegment = baseSegment;
				_nearSegment = nearSegment;

				HeightDifference = Math.Abs(baseSegment.Height - nearSegment.Height);
			}

			public double HeightDifference { get; }

			SegmentProxy ISegmentPair.BaseSegment => _baseSegment.Segment;

			SegmentProxy ISegmentPair.RelatedSegment => _nearSegment.Segment;
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private abstract class HeightSegmentPairProvider
		{
			private readonly double _horizontalToleranceRad;
			private readonly double _nearHeight;

			protected HeightSegmentPairProvider(double nearHeight)
			{
				_nearHeight = nearHeight;

				_horizontalToleranceRad = 0;
			}

			private bool IsHorizontal([NotNull] SegmentProxy segment)
			{
				double length = segment.Length;
				if (Math.Abs(length) < double.Epsilon)
				{
					return false;
				}

				double z0 = segment.GetStart(true)[2];
				double z1 = segment.GetEnd(true)[2];
				double slopeAngle = Math.Atan2(Math.Abs(z1 - z0), length);

				bool isHorizontal = slopeAngle <= _horizontalToleranceRad;
				return isHorizontal;
			}

			[NotNull]
			public IEnumerable<HeightSegmentPair> ReadHeightSegmentPair()
			{
				return GetHeightSegmentPairs();
			}

			[NotNull]
			protected abstract IEnumerable<HeightSegmentPair> GetHeightSegmentPairs();

			protected IEnumerable<HeightSegmentPair> GetHeightSegmentPairs(
				IEnumerable<SegmentProxy> segments)
			{
				List<HeightSegment> horizontalSegments = ReadHorizontalSegments(segments);
				horizontalSegments.Sort(HeightSegment.CompareHeight);

				List<HeightSegment> sortedHorizontalSegments = horizontalSegments;

				for (var baseIndex = 0; baseIndex < sortedHorizontalSegments.Count; baseIndex++)
				{
					HeightSegment baseSegment = sortedHorizontalSegments[baseIndex];
					double baseHeight = baseSegment.Height;

					double maxSearchHeight = baseHeight + _nearHeight;
					for (int nearIndex = baseIndex + 1;
					     nearIndex < sortedHorizontalSegments.Count;
					     nearIndex++)
					{
						HeightSegment candidate = sortedHorizontalSegments[nearIndex];
						if (candidate.Height < maxSearchHeight)
						{
							yield return new HeightSegmentPair(baseSegment, candidate);
						}
						else
						{
							break;
						}
					}
				}
			}

			[NotNull]
			private List<HeightSegment> ReadHorizontalSegments(
				IEnumerable<SegmentProxy> segments)
			{
				var result = new List<HeightSegment>();

				foreach (SegmentProxy segment in segments)
				{
					if (IsHorizontal(segment))
					{
						result.Add(new HeightSegment(segment));
					}
				}

				return result;
			}
		}

		private class HeightSegment
		{
			private readonly SegmentProxy _segment;
			private readonly double _height;

			public HeightSegment(SegmentProxy segment)
			{
				_segment = segment;
				double z0 = segment.GetStart(true)[2];
				double z1 = segment.GetStart(true)[2];
				_height = (z0 + z1) / 2.0;
			}

			public SegmentProxy Segment => _segment;

			public double Height => _height;

			public static int CompareHeight(HeightSegment x, HeightSegment y)
			{
				return x._height.CompareTo(y._height);
			}
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class MultipatchHeightSegmentPairProvider : HeightSegmentPairProvider
		{
			private readonly IIndexedMultiPatch _multiPatch;

			public MultipatchHeightSegmentPairProvider(double nearHeight,
			                                           [NotNull] IIndexedMultiPatch
				                                           multiPatch)
				: base(nearHeight)
			{
				_multiPatch = multiPatch;
			}

			protected override IEnumerable<HeightSegmentPair> GetHeightSegmentPairs()
			{
				return GetHeightSegmentPairs(_multiPatch.GetSegments());
			}
		}
	}
}
