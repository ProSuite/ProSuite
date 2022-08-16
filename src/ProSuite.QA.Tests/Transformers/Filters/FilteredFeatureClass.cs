using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class FilteredFeatureClass : TransformedFeatureClassBase<FilteredBackingDataset>
	{
		public FilteredFeatureClass(
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] string name,
			[NotNull] Func<GdbTable, FilteredBackingDataset> createBackingDataset)
			: base(-1, name, featureClassToFilter.ShapeType, createBackingDataset,
			       featureClassToFilter.Workspace)
		{
			FeatureClassToFilter = featureClassToFilter;

			// All fields
			IFields sourceFields = featureClassToFilter.Fields;
			for (int i = 0; i < sourceFields.FieldCount; i++)
			{
				AddField(sourceFields.Field[i]);
			}

			// and the base row - idea: could we just use the same name as the base class and fake the real row?
			// In that case we could directly pass through the rows from the featureClassToFilter
			// TODO: Check if it already exists, extract to Utils class
			AddField(FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField));
		}

		public IReadOnlyFeatureClass FeatureClassToFilter { get; }

		public bool PassesFilter(IReadOnlyFeature feature)
		{
			return BackingData.PassesFilter(feature);
		}
	}
}
