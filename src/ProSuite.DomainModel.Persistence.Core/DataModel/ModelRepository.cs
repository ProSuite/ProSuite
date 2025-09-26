using NHibernate;
using NHibernate.Criterion;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class ModelRepository : NHibernateRepository<DdxModel>, IModelRepository
	{
		#region IModelRepository Members

		public DdxModel Get(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			using (ISession session = OpenSession(true))
			{
				return session.QueryOver<DdxModel>()
				              .Where(Restrictions.Eq(nameof(DdxModel.Name), name).IgnoreCase())
				              .SingleOrDefault();
			}
		}

		#endregion
	}
}
