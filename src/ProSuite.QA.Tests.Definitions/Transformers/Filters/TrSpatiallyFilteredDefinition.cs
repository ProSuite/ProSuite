using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public abstract class TrSpatiallyFilteredDefinition : AlgorithmDefinition
	{
		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		[NotNull] protected readonly IFeatureClassSchemaDef _featureClassToFilter;
		[NotNull] protected readonly IFeatureClassSchemaDef _filtering;

		//private FilteredFeatureClass _resultingClass;

		protected TrSpatiallyFilteredDefinition(
			[NotNull] IFeatureClassSchemaDef featureClassToFilter,
			[NotNull] IFeatureClassSchemaDef filtering)
			: base(new[] { featureClassToFilter, filtering })
		{
			_featureClassToFilter = featureClassToFilter;
			_filtering = filtering;
			FilteringSearchOption = _defaultSearchOption;
		}

		[TestParameter(_defaultSearchOption)]
		[DocTr(nameof(DocTrStrings.TrSpatiallyFiltered_FilteringSearchOption))]
		public SearchOption FilteringSearchOption { get; set; }
	}
}
