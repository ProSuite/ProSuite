using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface IObjectCategoryRepository : IRepository<ObjectCategory>
	{
		[NotNull]
		IList<ObjectCategory> Get([NotNull] IEnumerable<int> ids);
	}
}
