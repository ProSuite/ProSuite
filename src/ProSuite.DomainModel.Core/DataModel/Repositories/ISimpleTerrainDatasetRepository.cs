using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface ISimpleTerrainDatasetRepository : IRepository<SimpleTerrainDataset>
	{
		IList<SimpleTerrainDataset> GetByModelId(int modelId);

		IList<SimpleTerrainDataset> GetByDatasets(IEnumerable<Dataset> datasets);

		IEnumerable<SimpleTerrainDataset> GetByDatasetIds(ICollection<int> datasetIds);
	}
}
