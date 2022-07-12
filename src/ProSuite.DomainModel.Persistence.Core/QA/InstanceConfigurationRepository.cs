using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	public class InstanceConfigurationRepository : NHibernateRepository<InstanceConfiguration>,
	                                               IInstanceConfigurationRepository
	{
		private const int _maxInParameterCount = 1000;

		#region Implementation of IInstanceConfigurationRepository

		public IList<TransformerConfiguration> GetTransformerConfigurations(
			IList<int> excludedIds = null)
		{
			using (ISession session = OpenSession(true))
			{
				var query = session.QueryOver<TransformerConfiguration>();

				if (excludedIds != null)
				{
					query.WhereRestrictionOn(tr => tr.Id)
					     .Not.IsIn(excludedIds.ToArray());
				}

				return query.List();
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

		public HashSet<int> GetIdsInvolvingDeletedDatasets<T>() where T : InstanceConfiguration
		{
			using (ISession session = OpenSession(true))
			{
				return GetIdsInvolvingDeletedDatasets<T>(session);
			}
		}

		public IList<T> Get<T>(
			DataQualityCategory category,
			bool includeQualityConditionsBasedOnDeletedDatasets = true)
			where T : InstanceConfiguration
		{
			if (category != null && ! category.IsPersistent)
			{
				return new List<T>();
			}

			using (ISession session = OpenSession(true))
			{
				return Get<T>(category, session, includeQualityConditionsBasedOnDeletedDatasets);
			}
		}

		public InstanceConfiguration Get(string name,
		                                 Type targetType)
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(targetType)
				              .Add(GetEqualityExpression("Name", name, true))
				              .UniqueResult<InstanceConfiguration>();
			}
		}

		public IDictionary<T, IList<DatasetTestParameterValue>> GetWithDatasetParameterValues<T>(
			DataQualityCategory category) where T : InstanceConfiguration
		{
			var result = new Dictionary<T, IList<DatasetTestParameterValue>>();

			if (category != null && ! category.IsPersistent)
			{
				return result;
			}

			using (ISession session = OpenSession(true))
			{
				foreach (var kvp in DatasetParameterFetchingUtils
					         .GetDatasetParameterValuesByConfiguration<T>(category, session))
				{
					result.Add(kvp.Key, kvp.Value);
				}

				return result;
			}
		}

		public IList<ReferenceCount> GetReferenceCounts<T>() where T : InstanceConfiguration
		{
			using (ISession session = OpenSession(true))
			{
				ReferenceCount referenceCount = null;

				IQueryOver<DatasetTestParameterValue>
					parametersQuery =
						session.QueryOver<DatasetTestParameterValue>()
						       .Where(p => p.ValueSource != null)
						       //.JoinQueryOver(p => p.ValueSource, () => transformerAlias)
						       .SelectList(lst => lst
						                          .SelectGroup(p => p.ValueSource.Id)
						                          .WithAlias(() => referenceCount.EntityId)
						                          .SelectCount(p => p.Id)
						                          .WithAlias(() => referenceCount.UsageCount))
						       .TransformUsing(Transformers.AliasToBean<ReferenceCount>());

				return parametersQuery.List<ReferenceCount>();
			}
		}

		public IList<InstanceConfiguration> GetReferencingConfigurations(
			TransformerConfiguration transformer)
		{
			if (! transformer.IsPersistent)
			{
				return new List<InstanceConfiguration>(0);
			}

			using (ISession session = OpenSession(true))
			{
				DatasetTestParameterValue parameterValueAlias = null;
				var result =
					DatasetParameterFetchingUtils.GetParentConfiguration<InstanceConfiguration>(
						null, session, () =>
							parameterValueAlias.ValueSource != null &&
							parameterValueAlias.ValueSource == transformer);

				return result;
			}
		}

		[NotNull]
		private static HashSet<int> GetIdsInvolvingDeletedDatasets<T>([NotNull] ISession session)
			where T : InstanceConfiguration
		{
			IList<int> datasetParameterIds =
				DatasetParameterFetchingUtils.GetDeletedDatasetParameterIds(session);

			return DatasetParameterFetchingUtils.GetInstanceConfigurationIdsForParameterIds<T>(
				session,
				datasetParameterIds,
				_maxInParameterCount);
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

				return criteria.List<T>();
			}
		}

		[NotNull]
		private static IList<T> Get<T>(
			[CanBeNull] DataQualityCategory category,
			[NotNull] ISession session,
			bool includeQualityConditionsBasedOnDeletedDatasets = true)
			where T : InstanceConfiguration
		{
			ICriteria criteria = session.CreateCriteria(typeof(T));

			// TODO: Add Category to InstanceConfigs
			//const string categoryProperty = "Category";

			//ICriterion filterCriterion =
			//	category == null
			//		? (ICriterion)new NullExpression(categoryProperty)
			//		: Restrictions.Eq(categoryProperty, category);

			//IList<T> all = criteria.Add(filterCriterion)
			//					   .List<T>();
			IList<T> all = criteria.List<T>();

			if (all.Count == 0 || includeQualityConditionsBasedOnDeletedDatasets)
			{
				return all;
			}

			IList<int> datasetParameterIds =
				DatasetParameterFetchingUtils.GetDeletedDatasetParameterIds(session);

			HashSet<int> excludedIds =
				DatasetParameterFetchingUtils.GetInstanceConfigurationIdsForParameterIds<T>(
					session, datasetParameterIds, _maxInParameterCount);

			return all.Where(qc => ! excludedIds.Contains(qc.Id))
			          .ToList();
		}

		#endregion
	}
}
