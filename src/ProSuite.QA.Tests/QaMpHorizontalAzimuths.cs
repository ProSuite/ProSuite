using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
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
	/// Reports horizontal with almost near azimuth
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpHorizontalAzimuths : ContainerTest
	{
		private readonly double _nearAngleRad;
		private readonly double _azimuthToleranceRad;
		private readonly double _horizontalToleranceRad;
		private readonly bool _perRing;

		private readonly double _xyResolution;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string AzimuthsSimilarButNotEqual_SegmentPair =
				"AzimuthsSimilarButNotEqual.SegmentPair";

			public const string AzimuthsSimilarButNotEqual_SegmentGroup =
				"AzimuthsSimilarButNotEqual.SegmentGroup";

			public Code() : base("MpHorizontalAzimuths") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_0))]
		public QaMpHorizontalAzimuths(
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_multiPatchClass))] [NotNull]
			IReadOnlyFeatureClass multiPatchClass,
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_nearAngle))]
			double nearAngle,
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_azimuthTolerance))]
			double azimuthTolerance,
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_horizontalTolerance))]
			double horizontalTolerance,
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_perRing))]
			bool perRing)
			: base(multiPatchClass)
		{
			_nearAngleRad = MathUtils.ToRadians(nearAngle);
			_azimuthToleranceRad = MathUtils.ToRadians(azimuthTolerance);
			_horizontalToleranceRad = MathUtils.ToRadians(horizontalTolerance);
			_perRing = perRing;

			_xyResolution = SpatialReferenceUtils.GetXyResolution(multiPatchClass.SpatialReference);

			AngleUnit = AngleUnit.Degree;
		}

		[InternallyUsedTest]
		public QaMpHorizontalAzimuths(
			[NotNull] QaMpHorizontalAzimuthsDefinition definition)
			: this((IReadOnlyFeatureClass)definition.MultiPatchClass,
			       definition.NearAngle,
			       definition.AzimuthTolerance,
			       definition.HorizontalTolerance,
			       definition.PerRing)
		{ }

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
			var errorCount = 0;
			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return errorCount;
			}

			var multiPatch = feature.Shape as IMultiPatch;
			if (multiPatch == null)
			{
				return errorCount;
			}

			var errorSegments = new List<ParallelSegmentPair>();

			ParallelSegmentPairProvider parallelSegmentPairProvider =
				GetParallelSegmentPairProvider(feature);

			foreach (ParallelSegmentPair parallelSegmentPair in
			         parallelSegmentPairProvider.ReadParallelSegmentPair())
			{
				double nonParallelAngle = parallelSegmentPair.AzimuthDifference;
				if (nonParallelAngle <= _azimuthToleranceRad)
				{
					continue;
				}

				AzimuthSegment baseSegment = parallelSegmentPair.BaseSegment;
				AzimuthSegment parallelSegment = parallelSegmentPair.ParallelSegment;

				double baseResolution = baseSegment.GetAzimuthResolution();
				double parallelResolution = parallelSegment.GetAzimuthResolution();
				double angleResolution = Math.Max(baseResolution, parallelResolution);
				if (angleResolution > _azimuthToleranceRad && nonParallelAngle < angleResolution)
				{
					continue;
				}

				errorSegments.Add(parallelSegmentPair);
			}

			errorCount += ReportErrors(errorSegments, row);

			return errorCount;
		}

		[NotNull]
		private ParallelSegmentPairProvider GetParallelSegmentPairProvider(
			[NotNull] IReadOnlyFeature feature)
		{
			var indexedFeature = feature as IIndexedMultiPatchFeature;
			IIndexedMultiPatch multiPatch = indexedFeature?.IndexedMultiPatch ??
			                                ProxyUtils.CreateIndexedMultiPatch(
				                                (IMultiPatch) feature.Shape);

			if (_perRing)
			{
				return new RingParallelSegmentPairProvider(_nearAngleRad,
				                                           _horizontalToleranceRad,
				                                           _xyResolution, multiPatch);
			}

			return new MultipatchParallelSegmentPairProvider(_nearAngleRad,
			                                                 _horizontalToleranceRad,
			                                                 _xyResolution, multiPatch);
		}

		private int ReportErrors([NotNull] IEnumerable<ParallelSegmentPair> errorSegments,
		                         [NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;

			var unhandledPairs = new List<ParallelSegmentPair>(errorSegments);

			while (unhandledPairs.Count > 0)
			{
				var relatedPairs = new List<ParallelSegmentPair> {unhandledPairs[0]};
				unhandledPairs.RemoveAt(0);

				SegmentPairUtils.AddRelatedPairsRecursive(relatedPairs, unhandledPairs);

				errorCount += ReportError(relatedPairs, row);
			}

			return errorCount;
		}

		private int ReportError([NotNull] IList<ParallelSegmentPair> relatedPairs,
		                        [NotNull] IReadOnlyRow row)
		{
			if (relatedPairs.Count == 0)
			{
				return NoError;
			}

			double maxAngleOffset = -1;
			foreach (ParallelSegmentPair parallelSegmentPair in relatedPairs)
			{
				double angleOffset = Math.Abs(parallelSegmentPair.AzimuthDifference);
				if (angleOffset > maxAngleOffset)
				{
					maxAngleOffset = angleOffset;
				}
			}

			IssueCode issueCode;
			string description = GetDescription(relatedPairs, maxAngleOffset, out issueCode);

			IGeometry errorGeometry = SegmentPairUtils.CreateGeometry(relatedPairs);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
				issueCode, TestUtils.GetShapeFieldName(row));
		}

		[NotNull]
		private string GetDescription(
			[NotNull] ICollection<ParallelSegmentPair> relatedPairs,
			double maxAngleOffset,
			out IssueCode issueCode)
		{
			double azimuthDiffDeg = MathUtils.ToDegrees(maxAngleOffset);
			double azimuthToleranceDeg = MathUtils.ToDegrees(_azimuthToleranceRad);
			string angleFormat = FormatUtils.CompareFormat(
				azimuthDiffDeg, ">", azimuthToleranceDeg, "N1");

			string offsetDescription = string.Format("{0} > {1}",
			                                         FormatAngle(maxAngleOffset, angleFormat),
			                                         FormatAngle(_azimuthToleranceRad,
			                                                     angleFormat));
			if (relatedPairs.Count == 1)
			{
				issueCode = Codes[Code.AzimuthsSimilarButNotEqual_SegmentPair];
				return string.Format(
					"The two segments have similar, but not equal azimuths (azimuth difference: {0})",
					offsetDescription);
			}

			issueCode = Codes[Code.AzimuthsSimilarButNotEqual_SegmentGroup];
			return string.Format(
				"The segments of this group have similar, but not equal azimuths (maximum azimuth difference: {0})",
				offsetDescription);
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class ParallelSegmentPair : ISegmentPair
		{
			public ParallelSegmentPair([NotNull] AzimuthSegment baseSegment,
			                           [NotNull] AzimuthSegment parallelSegment)
			{
				BaseSegment = baseSegment;
				ParallelSegment = parallelSegment;
				double azimuthDifference = Math.Abs(parallelSegment.Azimuth - baseSegment.Azimuth);
				if (azimuthDifference > Math.PI / 2.0)
				{
					azimuthDifference = Math.PI - azimuthDifference;
				}

				AzimuthDifference = azimuthDifference;
			}

			public double AzimuthDifference { get; }

			[NotNull]
			public AzimuthSegment BaseSegment { get; }

			[NotNull]
			public AzimuthSegment ParallelSegment { get; }

			SegmentProxy ISegmentPair.BaseSegment => BaseSegment.Segment;

			SegmentProxy ISegmentPair.RelatedSegment => ParallelSegment.Segment;
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private abstract class ParallelSegmentPairProvider
		{
			private readonly double _horizontalToleranceRad;
			private readonly double _nearAngleRad;

			protected ParallelSegmentPairProvider(double nearAngleRad,
			                                      double horizontalToleranceRad,
			                                      double xyResolution)
			{
				_nearAngleRad = nearAngleRad;
				_horizontalToleranceRad = horizontalToleranceRad;
				XyResolution = xyResolution;
			}

			private double XyResolution { get; }

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

			public IEnumerable<ParallelSegmentPair> ReadParallelSegmentPair()
			{
				return GetParallelSegmentPairs();
			}

			protected abstract IEnumerable<ParallelSegmentPair> GetParallelSegmentPairs();

			protected IEnumerable<ParallelSegmentPair> GetParallelSegmentPairs(
				IEnumerable<SegmentProxy> segments)
			{
				List<AzimuthSegment> horizontalSegments = ReadHorizontalSegments(segments);
				horizontalSegments.Sort(AzimuthSegment.CompareAzimuth);
				List<AzimuthSegment> sortedHorizontalSegments = horizontalSegments;

				for (var baseIndex = 0; baseIndex < sortedHorizontalSegments.Count; baseIndex++)
				{
					AzimuthSegment baseSegment = sortedHorizontalSegments[baseIndex];
					double baseAzimuth = baseSegment.Azimuth;

					double minSearchAzimuth = baseAzimuth - _nearAngleRad;
					double maxSearchAzimuth = baseAzimuth + _nearAngleRad;
					for (int parallelIndex = baseIndex + 1;
					     parallelIndex < sortedHorizontalSegments.Count;
					     parallelIndex++)
					{
						AzimuthSegment candidate = sortedHorizontalSegments[parallelIndex];
						if (candidate.Azimuth < maxSearchAzimuth ||
						    candidate.Azimuth - Math.PI > minSearchAzimuth)
						{
							yield return new ParallelSegmentPair(baseSegment, candidate);
						}
					}
				}
			}

			private List<AzimuthSegment> ReadHorizontalSegments(
				IEnumerable<SegmentProxy> segments)
			{
				var result = new List<AzimuthSegment>();

				foreach (SegmentProxy segment in segments)
				{
					if (IsHorizontal(segment))
					{
						result.Add(new AzimuthSegment(segment, XyResolution));
					}
				}

				return result;
			}
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class MultipatchParallelSegmentPairProvider : ParallelSegmentPairProvider
		{
			[NotNull] private readonly IIndexedMultiPatch _multiPatch;

			public MultipatchParallelSegmentPairProvider(
				double nearAngleRad,
				double horizontalToleranceRad,
				double xyResolution,
				[NotNull] IIndexedMultiPatch multiPatch)
				: base(nearAngleRad, horizontalToleranceRad, xyResolution)
			{
				_multiPatch = multiPatch;
			}

			protected override IEnumerable<ParallelSegmentPair> GetParallelSegmentPairs()
			{
				return GetParallelSegmentPairs(_multiPatch.GetSegments());
			}
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class RingParallelSegmentPairProvider : ParallelSegmentPairProvider
		{
			[NotNull] private readonly IIndexedMultiPatch _multiPatch;

			public RingParallelSegmentPairProvider(double nearAngleRad,
			                                       double horizontalToleranceRad,
			                                       double xyResolution,
			                                       [NotNull] IIndexedMultiPatch multiPatch)
				: base(nearAngleRad, horizontalToleranceRad, xyResolution)
			{
				_multiPatch = multiPatch;
			}

			protected override IEnumerable<ParallelSegmentPair> GetParallelSegmentPairs()
			{
				int lastPartIndex = -1;
				var ringSegments = new List<SegmentProxy>();
				foreach (SegmentProxy segment in _multiPatch.GetSegments())
				{
					if (segment.PartIndex != lastPartIndex)
					{
						foreach (ParallelSegmentPair pair in GetParallelSegmentPairs(ringSegments))
						{
							yield return pair;
						}

						ringSegments.Clear();
						lastPartIndex = segment.PartIndex;
					}

					ringSegments.Add(segment);
				}

				foreach (ParallelSegmentPair pair in GetParallelSegmentPairs(ringSegments))
				{
					yield return pair;
				}
			}
		}
	}
}
