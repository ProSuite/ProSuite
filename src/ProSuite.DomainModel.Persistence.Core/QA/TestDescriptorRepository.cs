using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	[UsedImplicitly]
	public class TestDescriptorRepository : NHibernateRepository<TestDescriptor>,
	                                        ITestDescriptorRepository
	{
		#region ITestDescriptorRepository Members

		public TestDescriptor Get(string name)
		{
			return GetUniqueResult("Name", name, true);
		}

		public IDictionary<int, int> GetReferencingQualityConditionCount()
		{
			using (ISession session = OpenSession(true))
			{
				IList list = session.CreateQuery(
					                    "select test.id, count(qc.id) " +
					                    "  from QualityCondition qc " +
					                    "   inner join qc.TestDescriptor as test " +
					                    " group by test.id")
				                    .List();

				var result = new Dictionary<int, int>(list.Count);
				foreach (object[] values in list)
				{
					var testDescriptorId = (int) values[0];
					int qualityConditionCount = Convert.ToInt32(values[1]);

					result.Add(testDescriptorId, qualityConditionCount);
				}

				return result;
			}
		}

		public TestDescriptor GetWithSameImplementation(TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			using (ISession session = OpenSession(true))
			{
				ICriteria criteria = session.CreateCriteria(typeof(TestDescriptor));

				if (testDescriptor.TestClass != null)
				{
					criteria.Add(Restrictions.And(
						             Restrictions.Eq("TestClass", testDescriptor.TestClass),
						             Restrictions.Eq("TestConstructorId",
						                             testDescriptor.TestConstructorId)));
				}
				else if (testDescriptor.TestFactoryDescriptor != null)
				{
					criteria.Add(
						Restrictions.Eq("TestFactoryDescriptor",
						                testDescriptor.TestFactoryDescriptor));
				}
				else
				{
					// both null
					throw new ArgumentException(
						@"Both test class and test factory descriptor are null",
						nameof(testDescriptor));
				}

				return criteria.UniqueResult<TestDescriptor>();
			}
		}

		#endregion
	}
}