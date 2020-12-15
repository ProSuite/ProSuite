using System;
using System.Collections.Generic;
using System.Globalization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.NamedValuesExpressions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	public static class DatasetFilterFactory
	{
		private const string _criterionNameNames = "names";
		private const string _criterionNameFeatureDatasets = "featuredatasets";

		[CanBeNull]
		public static DatasetFilter TryCreate(
			[NotNull] IEnumerable<NamedValuesExpression> inclusionExpressions,
			[NotNull] IEnumerable<NamedValuesExpression> exclusionExpressions,
			[NotNull] out NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(inclusionExpressions, nameof(inclusionExpressions));
			Assert.ArgumentNotNull(exclusionExpressions, nameof(exclusionExpressions));

			notifications = new NotificationCollection();

			IEnumerable<IDatasetMatchCriterion> inclusionMatchCriteria;
			IEnumerable<IDatasetMatchCriterion> exclusionMatchCriteria;
			bool success = TryGetCriteria(inclusionExpressions, exclusionExpressions,
			                              notifications,
			                              out inclusionMatchCriteria,
			                              out exclusionMatchCriteria);

			return success
				       ? new DatasetFilter(inclusionMatchCriteria, exclusionMatchCriteria)
				       : null;
		}

		private static bool TryGetCriteria(
			IEnumerable<NamedValuesExpression> inclusionExpressions,
			IEnumerable<NamedValuesExpression> exclusionExpressions,
			NotificationCollection notifications,
			out IEnumerable<IDatasetMatchCriterion> inclusionMatchCriteria,
			out IEnumerable<IDatasetMatchCriterion> exclusionMatchCriteria)
		{
			bool inclusionOk = TryGetDatasetMatchCriteria(inclusionExpressions, notifications,
			                                              out inclusionMatchCriteria);
			bool exclusionOk = TryGetDatasetMatchCriteria(exclusionExpressions, notifications,
			                                              out exclusionMatchCriteria);

			return inclusionOk && exclusionOk;
		}

		private static bool TryGetDatasetMatchCriteria(
			[NotNull] IEnumerable<NamedValuesExpression> expressions,
			[NotNull] NotificationCollection notifications,
			[NotNull] out IEnumerable<IDatasetMatchCriterion> criteria)
		{
			var list = new List<IDatasetMatchCriterion>();

			var anyFailure = false;
			foreach (NamedValuesExpression expression in expressions)
			{
				IDatasetMatchCriterion criterion = TryCreate(expression, notifications);
				if (criterion == null)
				{
					anyFailure = true;
				}
				else
				{
					list.Add(criterion);
				}
			}

			criteria = list;
			return ! anyFailure;
		}

		[CanBeNull]
		private static IDatasetMatchCriterion TryCreate(
			[NotNull] NamedValuesExpression expression,
			[NotNull] NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));
			Assert.ArgumentNotNull(notifications, nameof(notifications));

			var simpleExpression = expression as SimpleNamedValuesExpression;
			if (simpleExpression != null)
			{
				return TryCreate(simpleExpression.NamedValues, notifications);
			}

			var conjunction = expression as NamedValuesConjunctionExpression;
			if (conjunction != null)
			{
				return TryCreate(conjunction, notifications);
			}

			throw new ArgumentException(
				string.Format("Unsupported expression type: {0}", expression),
				nameof(expression));
		}

		[CanBeNull]
		private static IDatasetMatchCriterion TryCreate(
			[NotNull] NamedValuesConjunctionExpression conjunction,
			[NotNull] NotificationCollection notifications)
		{
			var result = new DatasetMatchCriterionConjunction();

			var anyFailure = false;

			foreach (NamedValues namedValues in conjunction.NamedValuesCollection)
			{
				IDatasetMatchCriterion criterion = TryCreate(namedValues, notifications);
				if (criterion != null)
				{
					result.Add(criterion);
				}
				else
				{
					anyFailure = true;
				}
			}

			return anyFailure
				       ? null
				       : result;
		}

		[CanBeNull]
		private static IDatasetMatchCriterion TryCreate(
			[NotNull] NamedValues namedValues,
			[NotNull] NotificationCollection notifications)
		{
			switch (namedValues.Name.ToLower(CultureInfo.InvariantCulture))
			{
				case _criterionNameNames:
					return new DatasetNameMatchCriterion(namedValues.Values);

				case _criterionNameFeatureDatasets:
					return new DatasetFeatureDatasetMatchCriterion(namedValues.Values);

				default:
					notifications.Add("Unknown criterion name: {0}; supported criterion names: {1}",
					                  namedValues.Name,
					                  StringUtils.Concatenate(
						                  new[]
						                  {
							                  _criterionNameNames,
							                  _criterionNameFeatureDatasets
						                  },
						                  ","));
					return null;
			}
		}
	}
}
