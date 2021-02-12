using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class ZDifferenceStrategyBoundingBox : ZDifferenceStrategy
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string RangeOverlap = "ZRangesOverlap";
			public const string TooSmall = "ZDifferenceTooSmall";
			public const string TooLarge = "ZDifferenceTooLarge";

			public const string
				ConstraintNotFulfilled = "ConstraintNotFulfilled";

			public const string UndefinedZ = "UndefinedZ";

			public Code() : base("ZRangeDifference") { }
		}

		#endregion

		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		public ZDifferenceStrategyBoundingBox(double minimumZDifference,
		                                      [CanBeNull] string minimumZDifferenceExpression,
		                                      double maximumZDifference,
		                                      [CanBeNull] string maximumZDifferenceExpression,
		                                      [CanBeNull] string zRelationConstraint,
		                                      bool expressionCaseSensitivity,
		                                      [NotNull] IErrorReporting errorReporting,
		                                      [NotNull] Func<double, string, double, string, string>
			                                      formatComparisonFunction)
			: base(minimumZDifference, minimumZDifferenceExpression,
			       maximumZDifference, maximumZDifferenceExpression,
			       zRelationConstraint, expressionCaseSensitivity,
			       errorReporting, formatComparisonFunction) { }

		protected override int ReportErrors(IFeature feature1, int tableIndex1,
		                                    IFeature feature2, int tableIndex2)
		{
			var zRangeFeature1 =
				new ZRangeFeature(feature1, tableIndex1, _envelopeTemplate);
			var zRangeFeature2 =
				new ZRangeFeature(feature2, tableIndex2, _envelopeTemplate);

			var errorCount = 0;
			int errorCountPreZNotNull = errorCount;

			errorCount += ValidateZNotNull(zRangeFeature1);
			errorCount += ValidateZNotNull(zRangeFeature2);

			if (errorCount != errorCountPreZNotNull)
			{
				// one or both Z values are NaN
				return errorCount;
			}

			ZRangeFeature minFeature;
			ZRangeFeature maxFeature;
			if (zRangeFeature1.ZMin <= zRangeFeature2.ZMin)
			{
				minFeature = zRangeFeature1;
				maxFeature = zRangeFeature2;
			}
			else
			{
				minFeature = zRangeFeature2;
				maxFeature = zRangeFeature1;
			}

			double minimumZDifference =
				GetMinimumZDifference(minFeature.Feature, minFeature.TableIndex,
				                      maxFeature.Feature, maxFeature.TableIndex);
			double maximumZDifference =
				GetMaximumZDifference(minFeature.Feature, minFeature.TableIndex,
				                      maxFeature.Feature, maxFeature.TableIndex);

			double dz = maxFeature.ZMin - minFeature.ZMax;

			var geometryProvider =
				new IntersectionGeometryProvider(minFeature, maxFeature);

			if (minimumZDifference > 0 && dz < minimumZDifference)
			{
				string description;
				IssueCode issueCode;
				if (dz > 0)
				{
					issueCode = Codes[Code.TooSmall];
					description = string.Format(
						"The Z distance between the feature Z ranges is too small ({0})",
						FormatComparison(dz, minimumZDifference, "<"));
				}
				else
				{
					double overlapDistance = Math.Min(minFeature.ZMax, maxFeature.ZMax) -
					                         Math.Max(minFeature.ZMin, maxFeature.ZMin);

					issueCode = Codes[Code.RangeOverlap];
					description = $"The feature Z ranges overlap by {overlapDistance:N2}";
				}

				errorCount += ErrorReporting.Report(description,
				                                    geometryProvider.Geometry,
				                                    issueCode, null,
				                                    minFeature.Feature,
				                                    maxFeature.Feature);
			}

			if (maximumZDifference > 0 && dz > maximumZDifference)
			{
				// a z difference larger than maximum is always an error
				string description = string.Format(
					"The Z distance between the feature Z ranges is too large ({0})",
					FormatComparison(dz, maximumZDifference, ">"));

				errorCount += ErrorReporting.Report(
					description,
					geometryProvider.Geometry, Codes[Code.TooLarge],
					TestUtils.GetShapeFieldName(feature1),
					new object[] {dz},
					feature1, feature2);
			}

			errorCount += CheckConstraint(minFeature, maxFeature, geometryProvider, dz);

			return errorCount;
		}

		private int CheckConstraint([NotNull] ZRangeFeature minFeature,
		                            [NotNull] ZRangeFeature maxFeature,
		                            [NotNull] IntersectionGeometryProvider geometryProvider,
		                            double zDifference)
		{
			string conditionMessage;
			bool fulFilled = IsZRelationConditionFulfilled(
				maxFeature.Feature, maxFeature.TableIndex,
				minFeature.Feature, minFeature.TableIndex,
				zDifference,
				out conditionMessage);

			if (fulFilled)
			{
				return NoError;
			}

			IGeometry errorGeometry = geometryProvider.Geometry;
			return ErrorReporting.Report(conditionMessage, errorGeometry,
			                             Codes[Code.ConstraintNotFulfilled], null,
			                             maxFeature.Feature,
			                             minFeature.Feature);
		}

		private int ValidateZNotNull([NotNull] ZRangeFeature feature)
		{
			return double.IsNaN(feature.ZMin) || double.IsNaN(feature.ZMax)
				       ? ErrorReporting.Report("Z is NaN", feature.Shape,
				                               Codes[Code.UndefinedZ],
				                               TestUtils.GetShapeFieldName(
					                               feature.Feature),
				                               feature.Feature)
				       : 0;
		}

		private static void GetZRange([NotNull] IFeature feature,
		                              [NotNull] IEnvelope template,
		                              out double zMin,
		                              out double zMax)
		{
			feature.Shape.QueryEnvelope(template);

			zMin = template.ZMin;
			zMax = template.ZMax;
		}

		private class ZRangeFeature
		{
			private readonly double _zMin;
			private readonly double _zMax;

			public ZRangeFeature([NotNull] IFeature feature,
			                     int tableIndex,
			                     [NotNull] IEnvelope template)
			{
				Feature = feature;
				TableIndex = tableIndex;

				GetZRange(feature, template, out _zMin, out _zMax);
			}

			public double ZMin => _zMin;

			public double ZMax => _zMax;

			[NotNull]
			public IGeometry Shape => Feature.Shape;

			[NotNull]
			public IFeature Feature { get; }

			public int TableIndex { get; }
		}

		private class IntersectionGeometryProvider
		{
			[NotNull] private readonly ZRangeFeature _minFeature;
			[NotNull] private readonly ZRangeFeature _maxFeature;

			[CanBeNull] private IGeometry _geometry;

			public IntersectionGeometryProvider([NotNull] ZRangeFeature minFeature,
			                                    [NotNull] ZRangeFeature maxFeature)
			{
				_minFeature = minFeature;
				_maxFeature = maxFeature;
			}

			public IGeometry Geometry => _geometry ??
			                             (_geometry =
				                              GetIntersectionGeometry(_minFeature.Shape,
				                                                      _maxFeature.Shape));

			[NotNull]
			private static IGeometry GetIntersectionGeometry([NotNull] IGeometry shape1,
			                                                 [NotNull] IGeometry shape2)
			{
				IGeometry result = null;

				foreach (IGeometry intersection in
					IntersectionUtils.GetAllIntersections(shape1, shape2))
				{
					if (result == null || result.Dimension < intersection.Dimension)
					{
						if (result != null)
						{
							Marshal.ReleaseComObject(result);
						}

						result = intersection;
					}
					else
					{
						Marshal.ReleaseComObject(intersection);
					}
				}

				return Assert.NotNull(result);
			}
		}
	}
}
