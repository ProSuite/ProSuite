using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public interface IConnectionProviderRepository : IRepository<ConnectionProvider>
	{
		[NotNull]
		IList<T> GetAll<T>() where T : ConnectionProvider;

		ConnectionProvider Get([NotNull] string name);
	}
}
