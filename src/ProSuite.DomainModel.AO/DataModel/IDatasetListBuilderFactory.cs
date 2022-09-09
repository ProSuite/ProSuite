using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IDatasetListBuilderFactory
	{
		IList<GeometryType> GeometryTypes { get; set; }

		[NotNull]
		IDatasetListBuilder Create([CanBeNull] string modelSchemaOwner,
		                           [CanBeNull] string modelDatasetPrefix,
		                           bool ignoreUnversionedDatasets,
		                           bool ignoreUnregisteredTables,
		                           bool unqualifyDatasetNames,
		                           [CanBeNull] IDatasetFilter datasetFilter);
	}
}
