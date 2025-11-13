using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	internal static class ZDifferenceStrategyFactory
	{
		[NotNull]
		public static ZDifferenceStrategy CreateStrategy(
			ZComparisonMethod zComparisonMethod,
			double minimumZDifference,
			string minimumZDifferenceExpression,
			double maximumZDifference,
			string maximumZDifferenceExpression,
			[CanBeNull] string zRelationConstraint,
			bool caseSensitivity,
			[NotNull] Func<double, string, double, string, string> formatComparisonFunction,
			[NotNull] IErrorReporting errorReporting,
			Func<int, bool> useDistanceFromPlane = null,
			double coplanarityTolerance = 0,
			bool ignoreNonCoplanarReferenceRings = false)
		{
			switch (zComparisonMethod)
			{
				case ZComparisonMethod.BoundingBox:
					return new ZDifferenceStrategyBoundingBox(
						minimumZDifference, minimumZDifferenceExpression,
						maximumZDifference, maximumZDifferenceExpression,
						zRelationConstraint, caseSensitivity,
						errorReporting, formatComparisonFunction);

				case ZComparisonMethod.IntersectionPoints:
					return new ZDifferenceStrategyIntersectionPoints(
						minimumZDifference, minimumZDifferenceExpression,
						maximumZDifference, maximumZDifferenceExpression,
						zRelationConstraint, caseSensitivity,
						errorReporting, formatComparisonFunction,
						useDistanceFromPlane, coplanarityTolerance,
						ignoreNonCoplanarReferenceRings
					);

				default:
					throw new ArgumentOutOfRangeException(
						nameof(zComparisonMethod), zComparisonMethod,
						$@"Unsupported ZComparisonMethod: {zComparisonMethod}");
			}
		}
	}
}
