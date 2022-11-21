using System;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class FilteredFeatureClass : TransformedFeatureClassBase<FilteredBackingDataset>
	{
		public FilteredFeatureClass(
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] string name,
			[NotNull] Func<GdbTable, FilteredBackingDataset> createBackingDataset)
			: base(null, name, featureClassToFilter.ShapeType, createBackingDataset,
			       featureClassToFilter.Workspace)
		{
			FeatureClassToFilter = featureClassToFilter;

			TransformedTableFields attributes = new TransformedTableFields(featureClassToFilter);
			attributes.AddAllFields(this);
		}

		public IReadOnlyFeatureClass FeatureClassToFilter { get; }

		public bool PassesFilter(IReadOnlyFeature feature)
		{
			return BackingData.PassesFilter(feature);
		}

		#region Overrides of TransformedFeatureClassBase<FilteredBackingDataset>

		public override bool NoCaching
		{
			// TODO: Caching results in wrong results in the test
			get => false;
			internal set => throw new NotImplementedException();
		}

		#endregion
	}
}
