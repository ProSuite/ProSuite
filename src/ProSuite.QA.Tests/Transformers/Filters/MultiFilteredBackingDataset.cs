using System.Collections.Generic;
using System.Data;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class MultiFilteredBackingDataset : FilteredBackingDataset
	{
		[NotNull] private readonly IDictionary<string, FilteredFeatureClass> _inputFiltersByName;
		[CanBeNull] private readonly string _expression;

		private DataView _filtersView;
		private IList<INamedFilter> _filters;

		public MultiFilteredBackingDataset(
			[NotNull] FilteredFeatureClass resultFeatureClass,
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] IList<IReadOnlyFeatureClass> inputFilters,
			[CanBeNull] string expression)
			: base(resultFeatureClass, featureClassToFilter,
			       inputFilters.Prepend(featureClassToFilter).Cast<IReadOnlyTable>().ToList())
		{
			// Alternatively just use a 'IFeatureFilter' interface on the FilteredFeatureClass (Name, PassesFilter)
			// This could also be used by IssueFilters to access commen functionality.
			_inputFiltersByName =
				inputFilters.ToDictionary(i => i.Name,
				                          i => ((FilteredFeatureClass) i));

			_expression = expression;
		}

		#region Overrides of BackingDataset

		public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
		{
			QueryFilterHelper resultFilter = QueryHelpers[0];
			Assert.NotNull(filter);

			// If the features are not in the container, a different approach would be more suitable:
			// Get all intersecting, search by unioned envelopes, etc.
			foreach (IReadOnlyRow resultRow in DataSearchContainer.Search(
				         FeatureClassToFilter, filter, resultFilter))
			{
				IReadOnlyFeature resultFeature = (IReadOnlyFeature) resultRow;

				if (PassesFilter(resultFeature))
				{
					yield return CreateFeature(resultFeature);
				}
			}
		}

		#endregion

		private IList<INamedFilter> Filters
		{
			get
			{
				if (_filters == null)
				{
					_filters = _inputFiltersByName.Values
					                              .Select(fc => fc.BackingData)
					                              .Cast<INamedFilter>()
					                              .ToList();
				}

				return _filters;
			}
		}

		#region Overrides of FilteredBackingDataset

		public override bool PassesFilter(IReadOnlyFeature resultFeature)
		{
			if (string.IsNullOrEmpty(_expression))
			{
				if (PassesAllFilters(resultFeature))
				{
					return true;
				}
			}
			else
			{
				_filtersView = _filtersView ??
				               FilterUtils.GetFiltersView(_expression, Filters);

				if (FilterUtils.IsFulfilled(_filtersView, Filters,
				                            filter => PassesFilter(filter, resultFeature)))
				{
					// NOTE:
					// Inversion of filter-logic in row filters: if the condition is fulfilled the row is NOT
					// filtered, but passed on!
					// Example 1: true/true
					// filter1 fulfilled (i.e. row passes filter): true1, filter2 fulfilled (i.e. row passes filtered): true2
					// filter1 AND filter2  (true1 AND true2): true -> row passes combined filter
					// filter1 OR filter2 (true1 OR true2): true -> row passes combined filter

					// Example : true/false
					// filter1 fulfilled (i.e. row passes filter): true1, filter2 is not fulfilled (i.e. row passes filtered): false2
					// filter1 AND filter2 (true1 AND false2): false -> row does not pass, filtered out
					// filter1 OR filter2 (true1 OR false2): true -> row passes combined filter

					return true;
				}
			}

			return false;
		}

		#endregion

		private static bool PassesFilter(INamedFilter filter, IReadOnlyFeature feature)
		{
			FilteredBackingDataset filterDataset = (FilteredBackingDataset) filter;

			return filterDataset.PassesFilter(feature);
		}

		private bool PassesAllFilters(IReadOnlyFeature resultFeature)
		{
			foreach (FilteredFeatureClass subFilter in _inputFiltersByName.Values)
			{
				if (! subFilter.PassesFilter(resultFeature))
				{
					return false;
				}
			}

			return true;
		}
	}
}
