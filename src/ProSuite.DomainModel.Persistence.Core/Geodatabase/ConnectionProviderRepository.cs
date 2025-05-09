using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.Geodatabase;
using ProSuite.DomainModel.Core.Geodatabase.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Geodatabase
{
	[UsedImplicitly]
	public class ConnectionProviderRepository :
		NHibernateRepository<ConnectionProvider>, IConnectionProviderRepository
	{
		#region IConnectionProviderRepository Members

		public IList<T> GetAll<T>() where T : ConnectionProvider
		{
			return GetAllCore<T>();
		}

		public ConnectionProvider Get(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			using (ISession session = OpenSession(true))
			{
				return session.QueryOver<ConnectionProvider>()
				              .WhereRestrictionOn(c => c.Name)
				              .IsInsensitiveLike(name).SingleOrDefault();
			}
		}

		#endregion
	}
}
