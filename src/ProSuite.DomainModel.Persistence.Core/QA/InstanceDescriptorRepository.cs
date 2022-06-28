using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	public class InstanceDescriptorRepository : NHibernateRepository<InstanceDescriptor>,
	                                            IInstanceDescriptorRepository
	{
		#region Implementation of IInstanceDescriptorRepository

		public IList<TransformerDescriptor> GetTransformerDescriptors()
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(TransformerDescriptor))
				              .List<TransformerDescriptor>();
			}
		}

		public IList<IssueFilterDescriptor> GetIssueFilterDescriptors()
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(IssueFilterDescriptor))
				              .List<IssueFilterDescriptor>();
			}
		}

		public IList<RowFilterDescriptor> GetRowFilterDescriptors()
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(RowFilterDescriptor))
				              .List<RowFilterDescriptor>();
			}
		}

		#endregion
	}
}
