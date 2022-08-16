using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class MultiFilteredBackingDataset : FilteredBackingDataset
	{
		[NotNull] private readonly IDictionary<string, FilteredFeatureClass> _inputFiltersByName;
		[CanBeNull] private readonly string _expression;

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

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
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
				// Should be possible similar to ContainerTest.IsFulfilled():
				// Make table view with columns named by the input filters
				// Add true/false values for each column
				// Implement INamedFilter here and de-couple filter functionality from ContainerTest?
				IDictionary<string, bool> passesByFiltername =
					GetPassingByFilterName(resultFeature);

				throw new NotImplementedException(
					"Multi-filter expressions are not yet implemented");
			}

			return false;
		}

		#endregion

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

		private IDictionary<string, bool> GetPassingByFilterName(IReadOnlyFeature resultFeature)
		{
			IDictionary<string, bool> result = new Dictionary<string, bool>();

			foreach (var pair in _inputFiltersByName)
			{
				string filterName = pair.Key;
				FilteredFeatureClass subFilter = pair.Value;

				bool passesFilter = subFilter.PassesFilter(resultFeature);

				result.Add(filterName, passesFilter);
			}

			return result;
		}
	}
}
