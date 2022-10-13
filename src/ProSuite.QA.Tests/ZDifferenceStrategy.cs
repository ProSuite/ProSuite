using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Tests
{
	public abstract class ZDifferenceStrategy
	{
		private readonly Func<double, string, double, string, string>
			_formatComparisonFunction;

		[NotNull] private readonly ZRelationCondition _zRelationCondition;
		private readonly double _minimumZDifference;
		private readonly double _maximumZDifference;

		[CanBeNull] private readonly ZDifferenceBoundExpression _minimumZDifferenceExpression;

		[CanBeNull] private readonly ZDifferenceBoundExpression _maximumZDifferenceExpression;

		private const string _zDifferenceColumn = "_ZDifference";
		[CanBeNull] private readonly Dictionary<string, object> _zDifferenceColumnValue;

		protected const int NoError = 0;

		protected ZDifferenceStrategy(
			double minimumZDifference, [CanBeNull] string minimumZDifferenceExpression,
			double maximumZDifference, [CanBeNull] string maximumZDifferenceExpression,
			[CanBeNull] string zRelationConstraint, bool expressionCaseSensitivity,
			[NotNull] IErrorReporting errorReporting,
			[NotNull] Func<double, string, double, string, string> formatComparisonFunction)
		{
			Assert.ArgumentNotNaN(minimumZDifference, nameof(minimumZDifference));
			Assert.ArgumentNotNaN(maximumZDifference, nameof(maximumZDifference));
			Assert.ArgumentNotNull(errorReporting, nameof(errorReporting));
			Assert.ArgumentNotNull(formatComparisonFunction,
			                       nameof(formatComparisonFunction));

			_minimumZDifference = minimumZDifference;
			_maximumZDifference = maximumZDifference;

			if (StringUtils.IsNotEmpty(minimumZDifferenceExpression))
			{
				_minimumZDifferenceExpression = new ZDifferenceBoundExpression(
					minimumZDifferenceExpression, expressionCaseSensitivity);
			}

			if (StringUtils.IsNotEmpty(maximumZDifferenceExpression))
			{
				_maximumZDifferenceExpression = new ZDifferenceBoundExpression(
					maximumZDifferenceExpression, expressionCaseSensitivity);
			}

			ErrorReporting = errorReporting;
			_formatComparisonFunction = formatComparisonFunction;

			if (zRelationConstraint != null &&
			    zRelationConstraint.IndexOf(_zDifferenceColumn,
			                                StringComparison
				                                .InvariantCultureIgnoreCase) >= 0)
			{
				_zDifferenceColumnValue = new Dictionary<string, object>();
			}

			_zRelationCondition = new ZRelationCondition(
				zRelationConstraint,
				expressionCaseSensitivity);
		}

		public int ReportErrors([NotNull] IReadOnlyRow row1, int tableIndex1,
		                        [NotNull] IReadOnlyRow row2, int tableIndex2)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (row1 == row2)
			{
				return NoError;
			}

			var feature1 = (IReadOnlyFeature) row1;
			var feature2 = (IReadOnlyFeature) row2;

			return ReportErrors(feature1, tableIndex1, feature2, tableIndex2);
		}

		protected abstract int ReportErrors([NotNull] IReadOnlyFeature feature1, int tableIndex1,
		                                    [NotNull] IReadOnlyFeature feature2, int tableIndex2);

		protected bool IsZRelationConditionFulfilled(IReadOnlyRow row1, int tableIndex1,
		                                             IReadOnlyRow row2, int tableIndex2,
		                                             double zDifference,
		                                             out string conditionMessage)
		{
			if (_zDifferenceColumnValue != null)
			{
				_zDifferenceColumnValue[_zDifferenceColumn] = zDifference;
			}

			return _zRelationCondition.IsFulfilled(row1, tableIndex1,
			                                       row2, tableIndex2,
			                                       out conditionMessage,
			                                       _zDifferenceColumnValue);
		}

		protected string FormatComparison(double zDifference,
		                                  double limit,
		                                  string comparison)
		{
			return _formatComparisonFunction(zDifference, comparison, limit, "N2");
		}

		[NotNull]
		protected IErrorReporting ErrorReporting { get; }

		protected double GetMinimumZDifference([NotNull] IReadOnlyFeature upper,
		                                       int tableIndexUpper,
		                                       [NotNull] IReadOnlyFeature lower,
		                                       int tableIndexLower)
		{
			return _minimumZDifferenceExpression == null
				       ? _minimumZDifference
				       : _minimumZDifferenceExpression.GetDouble(
					         upper, tableIndexUpper,
					         lower, tableIndexLower) ?? 0;
		}

		protected double GetMaximumZDifference([NotNull] IReadOnlyFeature upper,
		                                       int tableIndexUpper,
		                                       [NotNull] IReadOnlyFeature lower,
		                                       int tableIndexLower)
		{
			return _maximumZDifferenceExpression == null
				       ? _maximumZDifference
				       : _maximumZDifferenceExpression.GetDouble(
					         upper, tableIndexUpper,
					         lower, tableIndexLower) ?? 0;
		}

		private class ZRelationCondition : RowPairCondition
		{
			public ZRelationCondition([CanBeNull] string zRelationConstraint,
			                          bool caseSensitive)
				: base(zRelationConstraint, true, true, "U", "L", caseSensitive) { }

			protected override void AddUnboundColumns(Action<string, Type> addColumn,
			                                          IList<IReadOnlyTable> tables)
			{
				addColumn(_zDifferenceColumn, typeof(double));
			}
		}

		private class ZDifferenceBoundExpression : MultiTableDoubleFieldExpression
		{
			public ZDifferenceBoundExpression([NotNull] string expression,
			                                  bool caseSensitive)
				: base(expression, "U", "L", caseSensitive) { }
		}
	}
}
