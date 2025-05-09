using NUnit.Framework;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Geodatabase;
using ProSuite.DomainModel.Core.Geodatabase.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.Geodatabase
{
	public abstract class ConnectionProviderRepositoryTestBase
		: RepositoryTestBase<IConnectionProviderRepository>
	{
		protected abstract ConnectionProvider CreateConnectionProvider();

		protected abstract DdxModel CreateModel();

		[Test]
		public void CanGetByName()
		{
			DdxModel m = CreateModel();
			ConnectionProvider c = CreateConnectionProvider();
			m.UserConnectionProvider = c;

			CreateSchema(m, c);

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					ConnectionProvider result =
						Repository.Get(m.UserConnectionProvider.Name);

					Assert.IsNotNull(result);
					Assert.AreEqual(c.Name, result.Name);
				});
		}
	}
}
