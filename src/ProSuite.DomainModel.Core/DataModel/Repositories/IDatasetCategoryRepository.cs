using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface IDatasetCategoryRepository : IRepository<DatasetCategory>
	{
		DatasetCategory Get(string name);

		DatasetCategory GetByAbbreviation(string abbreviation);
	}
}
