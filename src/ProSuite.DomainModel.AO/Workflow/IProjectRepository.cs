using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public interface IProjectRepository<TModel> where TModel : ProductionModel
	{
		[CanBeNull]
		Project<TModel> GetByShortName([CanBeNull] string shortName);

		[CanBeNull]
		Project<TModel> GetByName([CanBeNull] string name);

		[NotNull]
		IList<Project<TModel>> GetAll(bool withProductionModel);

		[CanBeNull]
		Project<TModel> GetById(int id);
	}
}
