using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class LinearNetworkRepository : NHibernateRepository<LinearNetwork>,
	                                       ILinearNetworkRepository
	{
		public IList<LinearNetwork> GetByModelId(int modelId)
		{
			return GetAll().Where(ln => ln.ModelId == modelId).ToList();
		}

		public IList<LinearNetwork> GetByDatasets(IEnumerable<Dataset> datasets)
		{
			return GetByDatasetIds(datasets.Select(ds => ds.Id).ToList()).ToList();
		}

		public IEnumerable<LinearNetwork> GetByDatasetIds(ICollection<int> datasetIds)
		{
			// Could be optimized using a subquery, but usually the linear network count is small

			return GetAll()
				.Where(ln => ln.NetworkDatasets.Any(ds => datasetIds.Contains(ds.Dataset.Id)));
		}
	}
}
