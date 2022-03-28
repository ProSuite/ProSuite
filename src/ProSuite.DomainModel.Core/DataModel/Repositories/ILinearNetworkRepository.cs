using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface ILinearNetworkRepository : IRepository<LinearNetwork>
	{
		IList<LinearNetwork> GetByModelId(int modelId);

		IList<LinearNetwork> GetByDatasets(IEnumerable<Dataset> datasets);

		IEnumerable<LinearNetwork> GetByDatasetIds(ICollection<int> datasetIds);
	}
}
