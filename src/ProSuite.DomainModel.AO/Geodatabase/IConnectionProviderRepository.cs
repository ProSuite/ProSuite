using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	[CLSCompliant(false)]
	public interface IConnectionProviderRepository : IRepository<ConnectionProvider>
	{
		[NotNull]
		IList<S> GetAll<S>() where S : ConnectionProvider;

		ConnectionProvider Get([NotNull] string name);
	}
}
