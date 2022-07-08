using System;
using System.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;
using ProSuite.Commons.Essentials.Assertions;
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

		public InstanceDescriptor Get(string name)
		{
			return GetUniqueResult("Name", name, true);
		}

		public InstanceDescriptor GetWithSameImplementation(InstanceDescriptor instanceDescriptor)
		{
			Assert.ArgumentNotNull(instanceDescriptor, nameof(instanceDescriptor));

			using (ISession session = OpenSession(true))
			{
				ICriteria criteria = session.CreateCriteria(typeof(TestDescriptor));

				Assert.ArgumentNotNull(instanceDescriptor.Class,
				                       $"Class of instance descriptor {instanceDescriptor} is null");

				criteria.Add(Restrictions.And(
					             Restrictions.Eq("Class", instanceDescriptor.Class),
					             Restrictions.Eq("ConstructorId",
					                             instanceDescriptor.ConstructorId)));

				return criteria.UniqueResult<TestDescriptor>();
			}
		}

		public IDictionary<int, int> GetReferencingConfigurationCount<T>()
			where T : InstanceConfiguration
		{
			using (ISession session = OpenSession(true))
			{
				//IList<T> uniquelyNamedCats = session.CreateCriteria(typeof(T))
				//                                      .CreateEntityAlias(
				//	                                      "joinedCat",
				//	                                      Restrictions.And(
				//		                                      Restrictions.EqProperty("c.Name", "joinedCat.Name"),
				//		                                      Restrictions.NotEqProperty("c.Id", "joinedCat.Id")),
				//	                                      JoinType.InnerJoin,
				//	                                      typeof(Cat).FullName)
				//                                      .Add(Restrictions.IsNull("joinedCat.Id"))
				//                                      .List();

				//ICriteria criteria = session.CreateCriteria(typeof(T));

				//criteria.Add(.And())
				InstanceDescriptor instanceDescAlias = null;

				T instanceConfigAlias = null;
				var intermediateResult =
					session.QueryOver(() => instanceConfigAlias)
					       .JoinAlias(() => instanceConfigAlias.InstanceDescriptor,
					                  () => instanceDescAlias)
					       .SelectList(list => list.SelectCount(config => config.Id)
					                               .SelectGroup(
						                               config => config.InstanceDescriptor.Id))
					       .List<object>();

				//var list = session.CreateQuery(
				//	                    "select test.id, count(qc.id) " +
				//	                    "  from InstanceConfiguration qc " +
				//	                    "   inner join qc.InstanceDescriptor as test " +
				//	                    " group by test.id")
				//                    .List();

				var result = new Dictionary<int, int>(intermediateResult.Count);
				foreach (object[] values in intermediateResult)
				{
					int configCount = Convert.ToInt32(values[0]);
					var descriptorId = (int) values[1];

					result.Add(descriptorId, configCount);
				}

				return result;
			}
		}

		#endregion
	}
}
