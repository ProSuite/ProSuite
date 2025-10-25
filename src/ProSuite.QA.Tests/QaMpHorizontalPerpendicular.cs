using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
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
	public class QaMpHorizontalPerpendicular : ContainerTest
	{
		private readonly bool _connectedOnly;
		private readonly double _connectedTolerance;
		private readonly double _nearAngleRad;
		private readonly double _azimuthToleranceRad;
		private readonly double _horizontalToleranceRad;

		private readonly double _xyResolution;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NotSufficientlyPerpendicular_TwoSegments =
				"NotSufficientlyPerpendicular.TwoSegments";

			public const string NotSufficientlyPerpendicular_GroupOfSegments =
				"NotSufficientlyPerpendicular.GroupOfSegments";

			public Code() : base("MpHorizontalPerpendicular") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_0))]
		public QaMpHorizontalPerpendicular(
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_multiPatchClass))] [NotNull]
			IReadOnlyFeatureClass
				multiPatchClass,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_nearAngle))]
			double nearAngle,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_azimuthTolerance))]
			double azimuthTolerance,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_horizontalTolerance))]
			double horizontalTolerance,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_connectedOnly))]
			bool connectedOnly,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_connectedTolerance))]
			double connectedTolerance)
			: base(multiPatchClass)
		{
			_connectedOnly = connectedOnly;
			_connectedTolerance = connectedTolerance;
			_nearAngleRad = MathUtils.ToRadians(nearAngle);
			_azimuthToleranceRad = MathUtils.ToRadians(azimuthTolerance);
			_horizontalToleranceRad = MathUtils.ToRadians(horizontalTolerance);

			_xyResolution = SpatialReferenceUtils.GetXyResolution(multiPatchClass.SpatialReference);

			AngleUnit = AngleUnit.Degree;
		}

		[InternallyUsedTest]
		public QaMpHorizontalPerpendicular(
			[NotNull] QaMpHorizontalPerpendicularDefinition definition)
			: this((IReadOnlyFeatureClass) definition.MultiPatchClass,
			       definition.NearAngle,
			       definition.AzimuthTolerance,
			       definition.HorizontalTolerance,
			       definition.ConnectedOnly,
			       definition.ConnectedTolerance) { }

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

			var errorSegments = new List<PerpendicularSegmentPair>();
			PerpendicularSegmentsProvider perpendicularSegmentsProvider =
				GetPerpendicularSegmentsProvider(feature);
			foreach (PerpendicularSegmentPair perpendicularSegmentPair in
			         perpendicularSegmentsProvider.ReadPerpendicularSegments())
			{
				double nonPerpendicularAngle =
					Math.Abs(Math.PI / 2 - perpendicularSegmentPair.AzimuthDifference);
				if (nonPerpendicularAngle <= _azimuthToleranceRad)
				{
					continue;
				}

				AzimuthSegment baseSegment = perpendicularSegmentPair.BaseSegment;
				AzimuthSegment perpendicularSegment =
					perpendicularSegmentPair.PerpendicularSegment;

				double baseResolution = baseSegment.GetAzimuthResolution();
				double perpendicularResolution = perpendicularSegment.GetAzimuthResolution();
				double angleResolution = Math.Max(baseResolution, perpendicularResolution);
				if (angleResolution > _azimuthToleranceRad &&
				    nonPerpendicularAngle < angleResolution)
				{
					continue;
				}

				errorSegments.Add(perpendicularSegmentPair);
			}

			errorCount += ReportErrors(errorSegments, row);

			return errorCount;
		}

		private int ReportErrors(
			[NotNull] IEnumerable<PerpendicularSegmentPair> errorSegments,
			[NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;
			var unhandledPairs = new List<PerpendicularSegmentPair>(errorSegments);
			while (unhandledPairs.Count > 0)
			{
				var relatedPairs = new List<PerpendicularSegmentPair>();
				relatedPairs.Add(unhandledPairs[0]);
				unhandledPairs.RemoveAt(0);

				SegmentPairUtils.AddRelatedPairsRecursive(relatedPairs, unhandledPairs);

				errorCount += ReportError(relatedPairs, row);
			}

			return errorCount;
		}

		private int ReportError([NotNull] IList<PerpendicularSegmentPair> relatedPairs,
		                        [NotNull] IReadOnlyRow row)
		{
			if (relatedPairs.Count == 0)
			{
				return NoError;
			}

			double maxAngleOffset = -1;
			foreach (PerpendicularSegmentPair perpendicularSegmentPair in relatedPairs)
			{
				double angleOffset =
					Math.Abs(perpendicularSegmentPair.AzimuthDifference - Math.PI / 2);
				if (angleOffset > maxAngleOffset)
				{
					maxAngleOffset = angleOffset;
				}
			}

			double azimuthDiffDeg = MathUtils.ToDegrees(maxAngleOffset);
			double azimuthToleranceDeg = MathUtils.ToDegrees(_azimuthToleranceRad);
			string angleFormat = FormatUtils.CompareFormat(
				azimuthDiffDeg, ">", azimuthToleranceDeg, "N1");

			string description;
			IssueCode issueCode;
			string offsetDescription = string.Format("{0} > {1}",
			                                         FormatAngle(maxAngleOffset, angleFormat),
			                                         FormatAngle(_azimuthToleranceRad,
			                                                     angleFormat));

			if (relatedPairs.Count == 1)
			{
				description =
					string.Format(
						"The two segments are almost, but not sufficently perpendicular " +
						"(angle from perpendicular: {0})",
						offsetDescription);
				issueCode = Codes[Code.NotSufficientlyPerpendicular_TwoSegments];
			}
			else
			{
				description =
					string.Format(
						"The segments of this group are almost, but not sufficently perpendicular " +
						"(maximum angle from perpendicular {0})",
						offsetDescription);
				issueCode = Codes[Code.NotSufficientlyPerpendicular_GroupOfSegments];
			}

			IGeometry errorGeometry = SegmentPairUtils.CreateGeometry(relatedPairs);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
				issueCode, TestUtils.GetShapeFieldName(row));
		}

		[NotNull]
		private PerpendicularSegmentsProvider GetPerpendicularSegmentsProvider(
			IReadOnlyFeature feature)
		{
			var indexedFeature = feature as IIndexedMultiPatchFeature;
			IIndexedMultiPatch multiPatch = indexedFeature?.IndexedMultiPatch ??
			                                ProxyUtils.CreateIndexedMultiPatch(
				                                (IMultiPatch) feature.Shape);

			if (! _connectedOnly)
			{
				return new AllPerpendicularSegmentsProvider(_nearAngleRad,
				                                            _horizontalToleranceRad,
				                                            _xyResolution, multiPatch);
			}

			return new ConnectedPerpendicularSegmentsProvider(_nearAngleRad,
			                                                  _horizontalToleranceRad,
			                                                  _connectedTolerance,
			                                                  _xyResolution, multiPatch);
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class PerpendicularSegmentPair : ISegmentPair
		{
			public PerpendicularSegmentPair([NotNull] AzimuthSegment baseSegment,
			                                [NotNull] AzimuthSegment perpendicularSegment)
			{
				BaseSegment = baseSegment;
				PerpendicularSegment = perpendicularSegment;
				AzimuthDifference = Math.Abs(perpendicularSegment.Azimuth - baseSegment.Azimuth);
			}

			public double AzimuthDifference { get; }

			[NotNull]
			public AzimuthSegment BaseSegment { get; }

			[NotNull]
			public AzimuthSegment PerpendicularSegment { get; }

			SegmentProxy ISegmentPair.BaseSegment => BaseSegment.Segment;

			SegmentProxy ISegmentPair.RelatedSegment => PerpendicularSegment.Segment;
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private abstract class PerpendicularSegmentsProvider
		{
			private readonly double _horizontalToleranceRad;

			protected PerpendicularSegmentsProvider(double nearAngleRad,
			                                        double horizontalToleranceRad,
			                                        double xyResolution)
			{
				NearAngleRad = nearAngleRad;
				_horizontalToleranceRad = horizontalToleranceRad;
				XyResolution = xyResolution;
			}

			private double XyResolution { get; }

			protected double NearAngleRad { get; }

			[NotNull]
			public IEnumerable<PerpendicularSegmentPair> ReadPerpendicularSegments()
			{
				return GetPerpendicularSegmentPairs();
			}

			[NotNull]
			protected abstract IEnumerable<PerpendicularSegmentPair>
				GetPerpendicularSegmentPairs();

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
			protected List<AzimuthSegment> ReadAllHorizontalSegments(
				IIndexedSegments multiPatch)
			{
				var result = new List<AzimuthSegment>();

				foreach (SegmentProxy segment in multiPatch.GetSegments())
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
		private class AllPerpendicularSegmentsProvider : PerpendicularSegmentsProvider
		{
			[NotNull] private readonly IIndexedMultiPatch _multiPatch;

			public AllPerpendicularSegmentsProvider(double nearAngelRad,
			                                        double horizontalToleranceRad,
			                                        double xyResolution,
			                                        [NotNull] IIndexedMultiPatch multiPatch)
				: base(nearAngelRad, horizontalToleranceRad, xyResolution)
			{
				_multiPatch = multiPatch;
			}

			protected override IEnumerable<PerpendicularSegmentPair>
				GetPerpendicularSegmentPairs()
			{
				List<AzimuthSegment> horizontalSegments = ReadAllHorizontalSegments(_multiPatch);
				horizontalSegments.Sort(AzimuthSegment.CompareAzimuth);

				List<AzimuthSegment> sortedHorizontalSegments = horizontalSegments;

				for (var baseIndex = 0; baseIndex < sortedHorizontalSegments.Count; baseIndex++)
				{
					AzimuthSegment baseSegment = sortedHorizontalSegments[baseIndex];
					double baseAzimuth = baseSegment.Azimuth;

					double exactPerpendicularAzimuth = baseAzimuth + Math.PI / 2;
					double minSearchAzimuth = exactPerpendicularAzimuth - NearAngleRad;
					double maxSearchAzimuth = exactPerpendicularAzimuth + NearAngleRad;

					for (int perpendicularIndex = baseIndex + 1;
					     perpendicularIndex < sortedHorizontalSegments.Count;
					     perpendicularIndex++)
					{
						AzimuthSegment candidate = sortedHorizontalSegments[perpendicularIndex];
						if (candidate.Azimuth <= minSearchAzimuth)
						{
							continue;
						}

						if (candidate.Azimuth >= maxSearchAzimuth)
						{
							break;
						}

						yield return new PerpendicularSegmentPair(baseSegment, candidate);
					}
				}
			}
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class ConnectedPerpendicularSegmentsProvider : PerpendicularSegmentsProvider
		{
			private readonly double _connectedTolerance;
			[NotNull] private readonly List<AzimuthSegment> _sortedHorizontalSegments;

			public ConnectedPerpendicularSegmentsProvider(double nearAngelRad,
			                                              double horizontalToleranceRad,
			                                              double connectedTolerance,
			                                              double xyResolution,
			                                              [NotNull] IIndexedMultiPatch
				                                              multiPatch)
				: base(nearAngelRad, horizontalToleranceRad, xyResolution)
			{
				_connectedTolerance = connectedTolerance;

				List<AzimuthSegment> horizontalSegments = ReadAllHorizontalSegments(multiPatch);
				horizontalSegments.Sort(AzimuthSegment.CompareAzimuth);

				_sortedHorizontalSegments = horizontalSegments;
			}

			protected override IEnumerable<PerpendicularSegmentPair>
				GetPerpendicularSegmentPairs()
			{
				for (var baseIndex = 0; baseIndex < _sortedHorizontalSegments.Count; baseIndex++)
				{
					AzimuthSegment baseSegment = _sortedHorizontalSegments[baseIndex];

					double baseAzimuth = baseSegment.Azimuth;

					WKSPointZ baseStart =
						ProxyUtils.GetWksPoint(baseSegment.Segment.GetStart(true));

					WKSPointZ baseEnd =
						ProxyUtils.GetWksPoint(baseSegment.Segment.GetEnd(true));

					double exactPerpendicularAzimuth = baseAzimuth + Math.PI / 2;
					double minSearchAzimuth = exactPerpendicularAzimuth - NearAngleRad;
					double maxSearchAzimuth = exactPerpendicularAzimuth + NearAngleRad;

					for (int perpendicularIndex = baseIndex + 1;
					     perpendicularIndex < _sortedHorizontalSegments.Count;
					     perpendicularIndex++)
					{
						AzimuthSegment candidate = _sortedHorizontalSegments[perpendicularIndex];

						if (candidate.Azimuth <= minSearchAzimuth)
						{
							continue;
						}

						if (candidate.Azimuth >= maxSearchAzimuth)
						{
							break;
						}

						WKSPointZ candidateStart =
							ProxyUtils.GetWksPoint(candidate.Segment.GetStart(true));

						WKSPointZ candidateEnd =
							ProxyUtils.GetWksPoint(candidate.Segment.GetEnd(true));

						if (IsConnected(baseStart, candidateStart) ||
						    IsConnected(baseStart, candidateEnd) ||
						    IsConnected(baseEnd, candidateStart) ||
						    IsConnected(baseEnd, candidateEnd))
						{
							yield return new PerpendicularSegmentPair(baseSegment, candidate);
						}
					}
				}
			}

			private bool IsConnected(WKSPointZ x, WKSPointZ y)
			{
				double dx = x.X - y.X;
				double dy = x.Y - y.Y;
				double dz = x.Z - y.Z;
				double distanceSquare = dx * dx + dy * dy + dz * dz;

				return distanceSquare <= _connectedTolerance * _connectedTolerance;
			}
		}
	}
}
