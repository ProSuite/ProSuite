using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	public class InstanceConfigurationRepository : NHibernateRepository<InstanceConfiguration>,
	                                               IInstanceConfigurationRepository
	{
		#region Implementation of IInstanceConfigurationRepository

		public IList<TransformerConfiguration> GetTransformerConfigurations()
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(TransformerConfiguration))
				              .List<TransformerConfiguration>();
			}
		}

		public IList<RowFilterConfiguration> GetRowFilterConfigurations()
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(RowFilterConfiguration))
				              .List<RowFilterConfiguration>();
			}
		}

		public IList<IssueFilterConfiguration> GetIssueFilterConfigurations()
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(IssueFilterConfiguration))
				              .List<IssueFilterConfiguration>();
			}
		}

		#endregion
	}
}
