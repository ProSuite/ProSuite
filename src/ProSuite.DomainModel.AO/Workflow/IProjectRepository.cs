using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public interface IProjectRepository<TModel> where TModel : ProductionModel
	{
		[NotNull]
		IList<Project<TModel>> GetAll(bool withProductionModel);
	}
}
