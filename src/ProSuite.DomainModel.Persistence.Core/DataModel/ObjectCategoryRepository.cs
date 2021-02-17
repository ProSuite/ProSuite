using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class ObjectCategoryRepository : NHibernateRepository<ObjectCategory>,
	                                        IObjectCategoryRepository
	{
		#region IObjectCategoryRepository Members

		public IList<ObjectCategory> Get(IEnumerable<int> ids)
		{
			Assert.ArgumentNotNull(ids, nameof(ids));

			var idList = new List<int>(ids);

			if (idList.Count <= 0)
			{
				return new List<ObjectCategory>();
			}

			//var idList = new List<int>(ids.Count);
			//foreach (int id in ids)
			//{
			//    idList.Add(id);
			//}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              " from ObjectCategory as ocat " +
					              "where ocat.Id in ( :ids )")
				              .SetParameterList("ids", idList)
				              .List<ObjectCategory>();
			}
		}

		#endregion
	}
}
