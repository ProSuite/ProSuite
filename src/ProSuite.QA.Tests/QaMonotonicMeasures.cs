using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	[MValuesTest]
	public class QaMonotonicMeasures : ContainerTest
	{
		private readonly bool _hasM;
		private readonly IFeatureClass _lineClass;
		private readonly MonotonicityDirection _expectedMonotonicity;
		private readonly string _flipExpression;

		private RowCondition _flipCondition;

		private readonly bool _allowConstantValues;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string MeasuresNotMonotonic = "MeasuresNotMonotonic";

			public Code() : base("MonotonicMeasures") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMonotonicMeasures_0))]
		public QaMonotonicMeasures(
				[Doc(nameof(DocStrings.QaMonotonicMeasures_lineClass))] [NotNull]
				IFeatureClass lineClass,
				[Doc(nameof(DocStrings.QaMonotonicMeasures_allowConstantValues))]
				bool allowConstantValues)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(lineClass, allowConstantValues, MonotonicityDirection.Any, null) { }

		[Doc(nameof(DocStrings.QaMonotonicMeasures_1))]
		public QaMonotonicMeasures(
			[Doc(nameof(DocStrings.QaMonotonicMeasures_lineClass))] [NotNull]
			IFeatureClass lineClass,
			[Doc(nameof(DocStrings.QaMonotonicMeasures_allowConstantValues))]
			bool allowConstantValues,
			[Doc(nameof(DocStrings.QaMonotonicMeasures_expectedMonotonicity))]
			MonotonicityDirection
				expectedMonotonicity,
			[Doc(nameof(DocStrings.QaMonotonicMeasures_flipExpression))] [CanBeNull]
			string flipExpression)
			: base((ITable) lineClass)
		{
			Assert.ArgumentNotNull(lineClass, nameof(lineClass));

			_lineClass = lineClass;
			_expectedMonotonicity = expectedMonotonicity;
			_flipExpression = StringUtils.IsNotEmpty(flipExpression)
				                  ? flipExpression
				                  : null;

			_hasM = DatasetUtils.HasM(lineClass);

			_allowConstantValues = allowConstantValues;
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

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (! _hasM)
			{
				return NoError;
			}

			var feature = row as IFeature;
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
				_flipCondition = new RowCondition((ITable) _lineClass, _flipExpression,
				                                  undefinedConstraintIsFulfilled,
				                                  GetSqlCaseSensitivity(tableIndex));
			}

			var mAware = (IMAware) polyline;
			Assert.True(mAware.MAware, "The geometry is not M-aware");

			IEnumerable<MMonotonicitySequence> errorSequences = MeasureUtils.GetErrorSequences(
				polyline, _expectedMonotonicity,
				() => _flipCondition.IsFulfilled(feature),
				_allowConstantValues);

			return errorSequences.Sum(errorSequence => ReportError(
				                          GetErrorMessage(errorSequence),
				                          errorSequence.CreatePolyline(),
				                          Codes[Code.MeasuresNotMonotonic],
				                          TestUtils.GetShapeFieldName(feature),
				                          row));
		}

		[NotNull]
		private string GetErrorMessage([NotNull] MMonotonicitySequence sequence)
		{
			var sb = new StringBuilder();

			string segmentInfo = sequence.SegmentCount == 1
				                     ? "one segment"
				                     : string.Format("{0} segments", sequence.SegmentCount);

			sb.AppendFormat("M values are {0} for {1}",
			                GetMonotonicityTypeString(sequence.MonotonicityType,
			                                          sequence.FeatureIsFlipped),
			                segmentInfo);

			if (sequence.MonotonicityType == esriMonotinicityEnum.esriValueDecreases ||
			    sequence.MonotonicityType == esriMonotinicityEnum.esriValueIncreases)
			{
				sb.AppendFormat(". {0}",
				                GetExpectedMonotonicityString(sequence,
				                                              _expectedMonotonicity));

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
			[NotNull] MMonotonicitySequence sequence,
			MonotonicityDirection expectedMonotonicity)
		{
			switch (expectedMonotonicity)
			{
				case MonotonicityDirection.Decreasing:
					return "The M values should be decreasing";

				case MonotonicityDirection.Increasing:
					return "The M values should be increasing";

				case MonotonicityDirection.Any:
					return string.Format("The M value trend for the line is {0}",
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

		#endregion
	}
}
