using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	public class TrMultiFilter : TableTransformer<FilteredFeatureClass>
	{
		[NotNull] private readonly IReadOnlyFeatureClass _featureClassToFilter;
		[NotNull] private readonly IList<IReadOnlyFeatureClass> _inputFilters;
		[CanBeNull] private readonly string _expression;

		private FilteredFeatureClass _resultingClass;

		[DocTr(nameof(DocTrStrings.TrMultiFilter_0))]
		public TrMultiFilter([NotNull] IReadOnlyFeatureClass featureClassToFilter,
		                     [DocTr(nameof(DocTrStrings.TrMultiFilter_inputFilters))] [NotNull]
		                     IList<IReadOnlyFeatureClass> inputFilters,
		                     [DocTr(nameof(DocTrStrings.TrMultiFilter_expression))] [CanBeNull]
		                     string expression)
			: base(inputFilters.Prepend(featureClassToFilter))
		{
			_featureClassToFilter = featureClassToFilter;
			_inputFilters = inputFilters;

			// TODO: Is can be null allowed ? Make optional parameter? Extra overload?
			_expression = expression;

			bool valid = ValidateParameters(featureClassToFilter, inputFilters, expression,
			                                out string message);

			if (! valid)
			{
				throw new InvalidOperationException(message);
			}
		}

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
				FilteredFeatureClass inputFilteredClass = inputClass as FilteredFeatureClass;

				if (inputFilteredClass == null)
				{
					message =
						$"The input transformer {inputClass.Name} is not a filter transformer.";
					return false;
				}

				if (! tableToFilter.Equals(inputFilteredClass.FeatureClassToFilter))
				{
					message =
						$"The input filter {inputClass.Name} is not appliccable to {tableToFilter.Name}. " +
						"It filters a different feature class.";
					return false;
				}
			}

			if (expression != null)
			{
				return ValidateExpression(inputFilters, expression, out message);
			}

			return true;
		}

		private static bool ValidateExpression(IList<IReadOnlyFeatureClass> inputFilters,
		                                       string expression, out string message)
		{
			// TODO: Implement expression-based filtering

			if (! string.IsNullOrEmpty(expression))
			{
				message =
					"Expressions are not yet supported. Currently all input filter conditions " +
					"are AND-combined, i.e. a row must pass all filters";
				return false;
			}

			message = null;
			return true;
		}
	}
}
