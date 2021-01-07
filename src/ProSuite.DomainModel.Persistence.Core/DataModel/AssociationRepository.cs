using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class AssociationRepository : NHibernateRepository<Association>,
	                                     IAssociationRepository
	{
		#region IAssociationRepository Members

		public IList<Association> Get([NotNull] string name)
		{
			return Get(name, false);
		}

		public IList<Association> Get([NotNull] string name, bool includeDeleted)
		{
			using (ISession session = OpenSession(true))
			{
				ICriteria criteria = session.CreateCriteria(typeof(Association));

				const bool ignoreCase = true;
				criteria.Add(GetEqualityExpression("Name", name, ignoreCase));

				if (! includeDeleted)
				{
					criteria.Add(GetEqualityExpression("Deleted", false));
				}

				return criteria.List<Association>();
			}
		}

		public IList<T> GetAll<T>() where T : Association
		{
			return GetAllCore<T>();
		}

		#endregion
	}
}
