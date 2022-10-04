using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.AttributeDependencies.Repositories
{
	public interface IAttributeDependencyRepository : IRepository<AttributeDependency>
	{
		[CanBeNull]
		AttributeDependency Get([NotNull] Dataset dataset);

		[NotNull]
		IList<AttributeDependency> GetByModelId(int modelId);
	}
}
