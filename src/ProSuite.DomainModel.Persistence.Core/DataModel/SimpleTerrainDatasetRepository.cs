using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class SimpleTerrainDatasetRepository : NHibernateRepository<SimpleTerrainDataset>,
	                                              ISimpleTerrainDatasetRepository
	{
		public IList<SimpleTerrainDataset> GetByModelId(int modelId)
		{
			return GetAll().Where(ln => ln.ModelId == modelId).ToList();
		}

		public IList<SimpleTerrainDataset> GetByDatasets(IEnumerable<Dataset> datasets)
		{
			return GetByDatasetIds(datasets.Select(ds => ds.Id).ToList()).ToList();
		}

		public IEnumerable<SimpleTerrainDataset> GetByDatasetIds(ICollection<int> datasetIds)
		{
			// Could be optimized using a subquery, but usually the surface count is small

			return GetAll()
				.Where(sf => sf.Sources.Any(ds => datasetIds.Contains(ds.Dataset.Id)));
		}
	}
}
