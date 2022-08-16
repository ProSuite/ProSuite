using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public abstract class FilteredBackingDataset : TransformedBackingData
	{
		[NotNull]
		protected IReadOnlyFeatureClass FeatureClassToFilter { get; set; }

		[NotNull]
		protected FilteredFeatureClass ResultFeatureClass { get; }

		protected FilteredBackingDataset(
			[NotNull] FilteredFeatureClass resultFeatureClass,
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] IList<IReadOnlyTable> allInvolvedTables)
			: base(allInvolvedTables)
		{
			ResultFeatureClass = resultFeatureClass;
			FeatureClassToFilter = featureClassToFilter;
		}

		public abstract bool PassesFilter(IReadOnlyFeature resultFeature);

		#region Overrides of BackingDataset

		public override IEnvelope Extent => FeatureClassToFilter.Extent;

		public override VirtualRow GetRow(int id)
		{
			IReadOnlyFeature feature = (IReadOnlyFeature) FeatureClassToFilter.GetRow(id);

			if (PassesFilter(feature))
			{
				return CreateFeature(feature);
			}

			// Or throw?
			return null;
		}

		public override int GetRowCount(IQueryFilter queryFilter)
		{
			return Search(queryFilter, true).Count();
		}

		#endregion

		protected GdbFeature CreateFeature(IReadOnlyFeature feature)
		{
			var wrappedAttributeValues = new WrappedRowValues(feature, true);

			return new GdbFeature(feature.OID, ResultFeatureClass, wrappedAttributeValues);
		}
	}
}
