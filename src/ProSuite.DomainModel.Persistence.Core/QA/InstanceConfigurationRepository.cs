using System.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;
using ProSuite.Commons.Essentials.Assertions;
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

		public IList<T> Get<T>(InstanceDescriptor descriptor) where T : InstanceConfiguration
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			if (! descriptor.IsPersistent)
			{
				return new List<T>();
			}

			using (ISession session = OpenSession(true))
			{
				ICriteria criteria = session.CreateCriteria(typeof(T));

				criteria.Add(Restrictions.Eq("InstanceDescriptor", descriptor));

				//return session.CreateQuery(
				//	              "select qc " +
				//	              "  from QualityCondition qc " +
				//	              " where qc.TestDescriptor = :testDescriptor")
				//              .SetEntity("testDescriptor", descriptor)
				//              .List<InstanceConfiguration>();

				return criteria.List<T>();
			}
		}

		#endregion
	}
}
