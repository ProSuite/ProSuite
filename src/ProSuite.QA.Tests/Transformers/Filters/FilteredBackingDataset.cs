using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public abstract class FilteredBackingDataset : TransformedBackingData, INamedFilter
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

		public override VirtualRow GetRow(long id)
		{
			IReadOnlyFeature feature = (IReadOnlyFeature) FeatureClassToFilter.GetRow(id);

			if (PassesFilter(feature))
			{
				return CreateFeature(feature);
			}

			// Or throw?
			return null;
		}

		public override long GetRowCount(IQueryFilter queryFilter)
		{
			// TODO: Callers should refrain from getting row count unnecessarily
			// Currently the container calls row count for each tile for progress reasons
			return Search(queryFilter, true).Count();
		}

		#endregion

		protected GdbFeature CreateFeature(IReadOnlyFeature feature)
		{
			var wrappedAttributeValues = new WrappedRowValues(feature, true);

			return GdbFeature.Create(feature.OID, ResultFeatureClass, wrappedAttributeValues);
		}

		#region Implementation of INamedFilter

		public string Name
		{
			get => ResultFeatureClass.Name;
			set => throw new NotImplementedException();
		}

		#endregion
	}
}
