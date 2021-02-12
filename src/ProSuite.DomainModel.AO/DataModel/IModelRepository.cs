using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IModelRepository : IRepository<Model>
	{
		Model Get(string name);
	}
}
