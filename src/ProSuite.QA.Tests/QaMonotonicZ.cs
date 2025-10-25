using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaMonotonicZ : ContainerTest
	{
		private readonly bool _hasZ;
		[NotNull] private readonly IReadOnlyFeatureClass _lineClass;

		[CanBeNull] private RowCondition _flipCondition;

		private const MonotonicityDirection _defaultExpectedMonotonicity =
			MonotonicityDirection.Any;

		private const bool _defaultAllowConstantValues = true;
		private const string _defaultFlipExpression = null;

		private string _flipExpression;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ZNotMonotonic = "ZNotMonotonic";

			public Code() : base("MonotonicZ") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMonotonicZ_0))]
		public QaMonotonicZ(
			[Doc(nameof(DocStrings.QaMonotonicZ_lineClass))] [NotNull]
			IReadOnlyFeatureClass lineClass)
			: base(lineClass)
		{
			Assert.ArgumentNotNull(lineClass, nameof(lineClass));

			_lineClass = lineClass;

			_hasZ = DatasetUtils.GetGeometryDef(lineClass).HasZ;

			AllowConstantValues = _defaultAllowConstantValues;
			ExpectedMonotonicity = _defaultExpectedMonotonicity;
			FlipExpression = _defaultFlipExpression;
		}

		[InternallyUsedTest]
		public QaMonotonicZ(
			[NotNull] QaMonotonicZDefinition definition)
			: this((IReadOnlyFeatureClass) definition.LineClass)
		{
			AllowConstantValues = definition.AllowConstantValues;
			ExpectedMonotonicity = definition.ExpectedMonotonicity;
			FlipExpression = definition.FlipExpression;
		}

		[Doc(nameof(DocStrings.QaMonotonicZ_AllowConstantValues))]
		[TestParameter(_defaultAllowConstantValues)]
		public bool AllowConstantValues { get; set; }

		[Doc(nameof(DocStrings.QaMonotonicZ_ExpectedMonotonicity))]
		[TestParameter(_defaultExpectedMonotonicity)]
		public MonotonicityDirection ExpectedMonotonicity { get; set; }

		[Doc(nameof(DocStrings.QaMonotonicZ_FlipExpression))]
		[TestParameter(_defaultFlipExpression)]
		public string FlipExpression
		{
			get => _flipExpression;
			set
			{
				_flipExpression = value;
				AddCustomQueryFilterExpression(_flipExpression);
			}
		}

		#region Overrides of ContainerTest

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
			if (! _hasZ)
			{
				return NoError;
			}

			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			var polyline = feature.Shape as IPolyline;
			if (polyline == null)
			{
				return NoError;
			}

			if (_flipCondition == null)
			{
				const bool undefinedConstraintIsFulfilled = false;
				_flipCondition = new RowCondition(_lineClass, FlipExpression,
				                                  undefinedConstraintIsFulfilled,
				                                  GetSqlCaseSensitivity(tableIndex));
			}

			var zAware = (IZAware) polyline;
			Assert.True(zAware.ZAware, "The geometry is not Z-aware");

			IEnumerable<ZMonotonicitySequence> errorSequences = GetErrorSequences(
				polyline, ExpectedMonotonicity,
				() => _flipCondition.IsFulfilled(feature),
				AllowConstantValues);

			return errorSequences.Sum(errorSequence => ReportError(
				                          GetErrorMessage(errorSequence),
				                          InvolvedRowUtils.GetInvolvedRows(row),
				                          errorSequence.CreatePolyline(),
				                          Codes[Code.ZNotMonotonic],
				                          TestUtils.GetShapeFieldName(feature)));
		}

		[NotNull]
		private string GetErrorMessage([NotNull] ZMonotonicitySequence sequence)
		{
			var sb = new StringBuilder();

			string segmentInfo = sequence.SegmentCount == 1
				                     ? "one segment"
				                     : string.Format("{0} segments",
				                                     sequence.SegmentCount);

			sb.AppendFormat("Z values are {0} for {1}",
			                GetMonotonicityTypeString(sequence.MonotonicityType,
			                                          sequence.FeatureIsFlipped),
			                segmentInfo);

			if (sequence.MonotonicityType == esriMonotinicityEnum.esriValueDecreases ||
			    sequence.MonotonicityType == esriMonotinicityEnum.esriValueIncreases)
			{
				sb.AppendFormat(". {0}",
				                GetExpectedMonotonicityString(sequence,
				                                              ExpectedMonotonicity));

				if (sequence.FeatureIsFlipped != null &&
				    sequence.FeatureIsFlipped.Value)
				{
					sb.Append(" (against the feature orientation)");
				}
			}

			return sb.ToString();
		}

		[NotNull]
		private static string GetExpectedMonotonicityString(
			[NotNull] ZMonotonicitySequence sequence,
			MonotonicityDirection expectedMonotonicity)
		{
			switch (expectedMonotonicity)
			{
				case MonotonicityDirection.Decreasing:
					return "The Z values should be decreasing";

				case MonotonicityDirection.Increasing:
					return "The Z values should be increasing";

				case MonotonicityDirection.Any:
					return string.Format("The Z value trend for the line is {0}",
					                     GetMonotonicityTypeString(
						                     sequence.FeatureMonotonicityTrend,
						                     sequence.FeatureIsFlipped));

				default:
					throw new ArgumentException(
						string.Format("Unexpected monotonicity direction: {0}",
						              expectedMonotonicity));
			}
		}

		[NotNull]
		private static string GetMonotonicityTypeString(
			esriMonotinicityEnum? monotonicityType,
			bool? featureIsFlipped)
		{
			if (monotonicityType == null)
			{
				return "undefined";
			}

			bool flipped = featureIsFlipped.HasValue && featureIsFlipped.Value;

			switch (monotonicityType.Value)
			{
				case esriMonotinicityEnum.esriValueDecreases:
					return flipped
						       ? "increasing"
						       : "decreasing";

				case esriMonotinicityEnum.esriValueLevel:
					return "constant";

				case esriMonotinicityEnum.esriValueIncreases:
					return flipped
						       ? "decreasing"
						       : "increasing";

				default:
					return "undefined";
			}
		}

		[NotNull]
		private static IEnumerable<ZMonotonicitySequence> GetErrorSequences(
			[NotNull] IPolyline polyline,
			MonotonicityDirection expectedMonotonicity,
			[NotNull] Func<bool> isFeatureFlipped,
			bool allowConstantValues)
		{
			var geometryCollection = (IGeometryCollection) polyline;

			int partCount = geometryCollection.GeometryCount;

			if (partCount <= 1)
			{
				return GetErrorSequencesFromSinglePart(polyline,
				                                       expectedMonotonicity,
				                                       isFeatureFlipped,
				                                       allowConstantValues);
			}

			var result = new List<ZMonotonicitySequence>();

			foreach (IPath path in GeometryUtils.GetPaths(polyline))
			{
				var pathPolyline = (IPolyline) GeometryUtils.GetHighLevelGeometry(path);

				result.AddRange(GetErrorSequencesFromSinglePart(pathPolyline,
				                                                expectedMonotonicity,
				                                                isFeatureFlipped,
				                                                allowConstantValues));
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<ZMonotonicitySequence> GetErrorSequencesFromSinglePart(
			[NotNull] IPolyline singlePartPolyline,
			MonotonicityDirection expectedMonotonicity,
			[NotNull] Func<bool> isFeatureFlipped,
			bool allowConstantValues)
		{
			bool? featureFlipped = null;

			var points = (IPointCollection4) singlePartPolyline;
			int segmentCount = points.PointCount - 1;
			WKSPointZ[] wksPointZs = new WKSPointZ[points.PointCount];

			GeometryUtils.QueryWKSPointZs(points, wksPointZs);

			ZMonotonicitySequence currentSequence = null;

			double trend = wksPointZs[segmentCount].Z - wksPointZs[0].Z;
			esriMonotinicityEnum monotonicityTrend =
				MeasureUtils.GetMonotonicityType(trend);

			var checkedMonotonicity = GetCheckedMonotonicityDirection(
				expectedMonotonicity,
				monotonicityTrend);

			esriMonotinicityEnum? preMonotonicity = null;
			IEnumSegment enumSegments = null;
			bool? recycling = null;
			for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
			{
				double dz = wksPointZs[segmentIndex + 1].Z - wksPointZs[segmentIndex].Z;

				esriMonotinicityEnum monotonicity = MeasureUtils.GetMonotonicityType(dz);

				if (monotonicity == esriMonotinicityEnum.esriValueDecreases ||
				    monotonicity == esriMonotinicityEnum.esriValueIncreases)
				{
					if (! featureFlipped.HasValue)
					{
						featureFlipped = isFeatureFlipped();
					}

					if (featureFlipped.Value)
					{
						monotonicity =
							monotonicity == esriMonotinicityEnum.esriValueDecreases
								? esriMonotinicityEnum.esriValueIncreases
								: esriMonotinicityEnum.esriValueDecreases;
					}
				}

				if (monotonicity != preMonotonicity)
				{
					if (currentSequence != null)
					{
						yield return currentSequence;
					}

					preMonotonicity = monotonicity;
					currentSequence = null;
				}

				if (currentSequence == null)
				{
					if (monotonicity == esriMonotinicityEnum.esriValueLevel &&
					    allowConstantValues)
					{
						// ok
					}
					else if (monotonicity == esriMonotinicityEnum.esriValueIncreases &&
					         checkedMonotonicity == MonotonicityDirection.Increasing)
					{
						// ok
					}
					else if (monotonicity == esriMonotinicityEnum.esriValueDecreases &&
					         checkedMonotonicity == MonotonicityDirection.Decreasing)
					{
						// ok
					}
					else if (checkedMonotonicity == MonotonicityDirection.Any)
					{
						if (monotonicity == esriMonotinicityEnum.esriValueIncreases)
						{
							checkedMonotonicity = MonotonicityDirection.Increasing;
						}
						else if (monotonicity == esriMonotinicityEnum.esriValueDecreases)
						{
							checkedMonotonicity = MonotonicityDirection.Decreasing;
						}
					}
					else
					{
						currentSequence =
							new ZMonotonicitySequence(monotonicity,
							                          singlePartPolyline.SpatialReference)
							{
								FeatureMonotonicityTrend = monotonicityTrend,
								FeatureIsFlipped = featureFlipped
							};
					}
				}

				if (currentSequence != null)
				{
					if (enumSegments == null)
					{
						enumSegments = ((ISegmentCollection) singlePartPolyline)
							.EnumSegments;
					}

					var segment = GetSegment(enumSegments, segmentIndex);
					if (! recycling.HasValue)
					{
						recycling = enumSegments.IsRecycling;
					}

					currentSequence.Add(recycling.Value
						                    ? GeometryFactory.Clone(segment)
						                    : segment);

					if (recycling.Value)
					{
						Marshal.ReleaseComObject(segment);
					}
				}
			}

			if (currentSequence != null)
			{
				yield return currentSequence;
			}

			if (enumSegments != null)
			{
				enumSegments.Reset();
			}
		}

		[NotNull]
		private static ISegment GetSegment([NotNull] IEnumSegment enumSegments,
		                                   int segmentIndex)
		{
			enumSegments.SetAt(0, segmentIndex);

			ISegment segment;
			int partIndex = 0;
			int segIndex = 0;

			enumSegments.Next(out segment, ref partIndex, ref segIndex);
			return Assert.NotNull(segment, "segment not found for index {0}",
			                      segmentIndex);
		}

		private static MonotonicityDirection GetCheckedMonotonicityDirection(
			MonotonicityDirection expectedMonotonicity,
			esriMonotinicityEnum monotonicityTrend)
		{
			if (expectedMonotonicity != MonotonicityDirection.Any)
			{
				return expectedMonotonicity;
			}

			switch (monotonicityTrend)
			{
				case esriMonotinicityEnum.esriValueDecreases:
					return MonotonicityDirection.Decreasing;

				case esriMonotinicityEnum.esriValueIncreases:
					return MonotonicityDirection.Increasing;

				case esriMonotinicityEnum.esriValueLevel:
				case esriMonotinicityEnum.esriValuesEmpty:
					return MonotonicityDirection.Any;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(monotonicityTrend), monotonicityTrend,
						null);
			}
		}

		#endregion
	}
}
