using System.Collections.Generic;

namespace ProSuite.DomainModel.Core.DataModel
{
	public interface IDatasetCollection
	{
		IEnumerable<IDdxDataset> ContainedDatasets { get; }
	}
}
