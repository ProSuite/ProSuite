using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

		public IList<Association> Get(string name)
		{
			return Get(name, false);
		}

		public IList<Association> Get(string name, bool includeDeleted)
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

		public IList<Association> GetByReferencedDatasetIds(IList<int> datasetIds)
		{
			ICollection idCollection = (ICollection) datasetIds;

			using (ISession session = OpenSession(true))
			{
				AssociationEnd end1Alias = null;
				AssociationEnd end2Alias = null;

				IList<Association> end1References =
					session.QueryOver<Association>()
					       .JoinAlias(a => a.End1, () => end1Alias)
					       .AndRestrictionOn(() => end1Alias.ObjectDataset.Id).IsIn(idCollection)
					       .List();

				// Add the datasets from the End2 to the result list:
				IList<Association> end2References =
					session.QueryOver<Association>()
					       .JoinAlias(a => a.End2, () => end2Alias)
					       .AndRestrictionOn(() => end2Alias.ObjectDataset.Id).IsIn(idCollection)
					       .List();

				return end1References.Union(end2References).ToList();
			}
		}

		#endregion
	}
}
