using System.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	[UsedImplicitly]
	public class DataQualityCategoryRepository :
		NHibernateRepository<DataQualityCategory>,
		IDataQualityCategoryRepository
	{
		public IList<DataQualityCategory> GetTopLevelCategories()
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(DataQualityCategory))
				              .Add(new NullExpression("ParentCategory"))
				              .List<DataQualityCategory>();
			}
		}
	}
}
