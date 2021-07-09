using ProSuite.Commons.DomainModels;
using ProSuite.DomainModel.Core.DataModel;
using System.Collections.Generic;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface ISimpleTerrainDatasetRepository : IRepository<SimpleTerrainDataset>
	{
		IList<SimpleTerrainDataset> GetByModelId(int modelId);

		IList<SimpleTerrainDataset> GetByDatsets(IEnumerable<Dataset> datasets);

		IEnumerable<SimpleTerrainDataset> GetByDatsetIds(ICollection<int> datasetIds);
	}
}
