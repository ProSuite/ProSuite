using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Geodatabase.Repositories
{
	public interface IConnectionProviderRepository : IRepository<ConnectionProvider>
	{
		[NotNull]
		IList<T> GetAll<T>() where T : ConnectionProvider;

		ConnectionProvider Get([NotNull] string name);
	}
}
