using System;
using System.Collections.Generic;
using NHibernate;
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

		public IList<T> GetInstanceDescriptors<T>() where T : InstanceDescriptor
		{
			using (ISession session = OpenSession(true))
			{
				//alternatively could use: session.CreateCriteria(typeof(T)).List<T>();
				return session.QueryOver<T>().List();
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
				if (instanceDescriptor is TestDescriptor testDescriptor)
				{
					return QualityRepositoryUtils.GetTestDescriptorWithSameImplementation(
						session, testDescriptor);
				}

				Assert.ArgumentNotNull(instanceDescriptor.Class,
				                       $"Class of instance descriptor {instanceDescriptor} is null");

				if (instanceDescriptor is TransformerDescriptor trDescriptor)
				{
					return QualityRepositoryUtils.GetWithSameImplementation(session, trDescriptor);
				}

				if (instanceDescriptor is IssueFilterDescriptor ifDescriptor)
				{
					return QualityRepositoryUtils.GetWithSameImplementation(session, ifDescriptor);
				}

				throw new ArgumentOutOfRangeException(
					$"Unsupported instance descriptor type: {instanceDescriptor}.");
			}
		}

		public IDictionary<int, int> GetReferencingConfigurationCount<T>()
			where T : InstanceConfiguration
		{
			using (ISession session = OpenSession(true))
			{
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

		public bool SupportsTransformersAndFilters
		{
			get
			{
				Version version = GetDatabaseSchemaVersion();

				return version >= DdxSchemaVersion.FiltersAndTransformers;
			}
		}

		#endregion
	}
}
