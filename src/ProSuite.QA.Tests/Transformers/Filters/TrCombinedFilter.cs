using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrCombinedFilter : TableTransformer<FilteredFeatureClass>
	{
		[NotNull] private readonly IReadOnlyFeatureClass _featureClassToFilter;
		[NotNull] private readonly IList<IReadOnlyFeatureClass> _inputFilters;
		[CanBeNull] private readonly string _expression;

		private FilteredFeatureClass _resultingClass;

		[DocTr(nameof(DocTrStrings.TrCombinedFilter_0))]
		public TrCombinedFilter(
			[NotNull] [DocTr(nameof(DocTrStrings.TrCombinedFilter_featureClassToFilter))]
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrCombinedFilter_inputFilters))]
			IList<IReadOnlyFeatureClass> inputFilters,
			[CanBeNull] [DocTr(nameof(DocTrStrings.TrCombinedFilter_expression))]
			string expression)
			: base(inputFilters.Prepend(featureClassToFilter))
		{
			_featureClassToFilter = featureClassToFilter;
			_inputFilters = inputFilters;

			// NOTE: Even constructor parameters can be null!
			_expression = expression;

			bool valid = ValidateParameters(featureClassToFilter, inputFilters, expression,
			                                out string message);

			if (! valid)
			{
				throw new InvalidOperationException(message);
			}
		}

		[InternallyUsedTest]
		public TrCombinedFilter(
			[NotNull] TrCombinedFilterDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClassToFilter,
				   definition.InputFilters.Cast<IReadOnlyFeatureClass>().ToList(),
				   definition.Expression)
		{ }

		#region Overrides of TableTransformer<FilteredFeatureClass>

		protected override FilteredFeatureClass GetTransformedCore(string tableName)
		{
			if (_resultingClass == null)
			{
				string filteredTableName = ((ITableTransformer) this).TransformerName;

				// Un-transformed, uncached identical schema as the _featureClassToFilter
				// If the evaluation of the filter criterion is slow, re-consider caching.
				// But an efficient cache could also be implemented locally, e.g. by
				// remembering the OIDs that were filtered out previously.
				_resultingClass = new FilteredFeatureClass(
					_featureClassToFilter, filteredTableName,
					createBackingDataset: gdbTable =>
						CreateFilteredDataset((FilteredFeatureClass) gdbTable));
			}

			return _resultingClass;
		}

		private MultiFilteredBackingDataset CreateFilteredDataset(FilteredFeatureClass resultClass)
		{
			var result =
				new MultiFilteredBackingDataset(resultClass, _featureClassToFilter,
				                                _inputFilters, _expression);

			return result;
		}

		#endregion

		private static bool ValidateParameters(IReadOnlyFeatureClass tableToFilter,
		                                       IList<IReadOnlyFeatureClass> inputFilters,
		                                       string expression,
		                                       out string message)
		{
			message = null;

			foreach (IReadOnlyFeatureClass inputClass in inputFilters)
			{
				if (! (inputClass is FilteredFeatureClass inputFilteredClass))
				{
					message =
						$"The input transformer {inputClass.Name} is not a filter transformer.";
					return false;
				}

				if (! tableToFilter.Equals(inputFilteredClass.FeatureClassToFilter))
				{
					message =
						$"The input filter {inputClass.Name} is not applicable to {tableToFilter.Name}. " +
						"It filters a different feature class.";
					return false;
				}
			}

			return expression == null || ValidateExpression(inputFilters, expression, out message);
		}

		private static bool ValidateExpression(IList<IReadOnlyFeatureClass> inputFilters,
		                                       string expression, out string message)
		{
			message = null;

			if (string.IsNullOrEmpty(expression))
			{
				return true;
			}

			List<string> validFilterNames =
				inputFilters.Select(filterClass => filterClass.Name).ToList();

			foreach (string expressionToken in FilterUtils.GetFilterNames(expression))
			{
				if (! validFilterNames.Any(
					    n => expressionToken.Equals(
						    n, StringComparison.InvariantCultureIgnoreCase)))
				{
					message = $"Filter-Transformer '{expressionToken}' defined in filter " +
					          "expression is not in the list of input filters";
					return false;
				}
			}

			return true;
		}
	}
}
