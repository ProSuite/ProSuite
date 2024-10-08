using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	public class InstanceConfigurationRepository : NHibernateRepository<InstanceConfiguration>,
	                                               IInstanceConfigurationRepository
	{
		private const int _maxInParameterCount = 1000;

		#region Implementation of IInstanceConfigurationRepository

		public IList<T> GetInstanceConfigurations<T>() where T : InstanceConfiguration
		{
			if ((typeof(T) == typeof(IssueFilterConfiguration) ||
			     typeof(T) == typeof(TransformerConfiguration)) &&
			    ! AreTransformersAndFiltersSupported())
			{
				return new List<T>();
			}

			using (ISession session = OpenSession(true))
			{
				//alternatively could use: session.CreateCriteria(typeof(T)).List<T>();
				return session.QueryOver<T>().List();
			}
		}

		public IEnumerable<Dataset> GetAllReferencedDatasets(
			IEnumerable<QualityCondition> qualityConditions,
			bool includeReferenceViaIssueFilters = false,
			Predicate<DatasetTestParameterValue> testParameterPredicate = null)
		{
			// Strategy to reduce the number of round trips: collect transformer ids and
			// load via list, then collect the next round of transformer ids
			using (ISession session = OpenSession(true))
			{
				foreach (Dataset dataset in QualityRepositoryUtils.GetAllReferencedDatasets(
					         session, qualityConditions, includeReferenceViaIssueFilters,
					         testParameterPredicate))
				{
					yield return dataset;
				}
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

		public IList<T> Get<T>(IEnumerable<DataQualityCategory> categories)
			where T : InstanceConfiguration
		{
			using (ISession session = OpenSession(true))
			{
				int[] categoryIds = categories.Select(c => c.Id).ToArray();

				T instanceConfig = null;
				DataQualityCategory categoryAlias = null;

				var query =
					session.QueryOver(() => instanceConfig)
					       .JoinAlias(() => instanceConfig.Category,
					                  () => categoryAlias)
					       .WhereRestrictionOn(
						       () => categoryAlias.Id).IsIn(categoryIds);

				return query.List();
			}
		}

		public InstanceConfiguration Get(string name, Type targetType)
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
				foreach (var kvp in QualityRepositoryUtils
					         .GetDatasetParameterValuesByConfiguration<T>(category, session))
				{
					result.Add(kvp.Key, kvp.Value);
				}

				return result;
			}
		}

		public IList<ReferenceCount> GetReferenceCounts<T>() where T : InstanceConfiguration
		{
			if (typeof(T) == typeof(IssueFilterConfiguration))
			{
				return GetFilterReferenceCounts();
			}

			if (typeof(T) == typeof(TransformerConfiguration))
			{
				return GetTransformerReferenceCounts<T>();
			}

			throw new NotImplementedException("Unknown instance configuration type");
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
				Expression<Func<bool>> parameterExpression = () =>
					parameterValueAlias.ValueSource != null &&
					parameterValueAlias.ValueSource == transformer;

				var result =
					QualityRepositoryUtils.GetParentConfiguration<InstanceConfiguration>(
						session, parameterExpression);

				return result;
			}
		}

		public IList<InstanceConfiguration> Get(InstanceDescriptor descriptor)
		{
			switch (descriptor)
			{
				case TransformerDescriptor _:
					return Get<TransformerConfiguration>(descriptor).Cast<InstanceConfiguration>()
						.ToList();
				case IssueFilterDescriptor _:
					return Get<IssueFilterConfiguration>(descriptor).Cast<InstanceConfiguration>()
						.ToList();
				default:
					throw new NotImplementedException(
						$"Unsupported instance descriptor type: {descriptor}");
			}
		}

		[NotNull]
		private static HashSet<int> GetIdsInvolvingDeletedDatasets<T>([NotNull] ISession session)
			where T : InstanceConfiguration
		{
			IList<int> datasetParameterIds =
				QualityRepositoryUtils.GetDeletedDatasetParameterIds(session);

			return QualityRepositoryUtils.GetInstanceConfigurationIdsForParameterIds<T>(
				session,
				datasetParameterIds,
				_maxInParameterCount);
		}

		private IList<T> Get<T>(InstanceDescriptor descriptor) where T : InstanceConfiguration
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

			const string categoryProperty = "Category";

			ICriterion filterCriterion =
				category == null
					? (ICriterion) new NullExpression(categoryProperty)
					: Restrictions.Eq(categoryProperty, category);

			IList<T> all = criteria.Add(filterCriterion).List<T>();

			if (all.Count == 0 || includeQualityConditionsBasedOnDeletedDatasets)
			{
				return all;
			}

			HashSet<int> excludedIds = GetIdsInvolvingDeletedDatasets<T>(session);

			return all.Where(qc => ! excludedIds.Contains(qc.Id))
			          .ToList();
		}

		private IList<ReferenceCount> GetTransformerReferenceCounts<T>()
			where T : InstanceConfiguration
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

		private IList<ReferenceCount> GetFilterReferenceCounts()
		{
			using (ISession session = OpenSession(true))
			{
				ReferenceCount referenceCount = null;

				IssueFilterConfiguration issueFilterAlias = null;

				var parametersQuery =
					session.QueryOver<QualityCondition>()
					       .JoinQueryOver(qc => qc.IssueFilterConfigurations,
					                      () => issueFilterAlias)
					       .SelectList(list => list
					                           .SelectGroup(() => issueFilterAlias.Id)
					                           .WithAlias(() => referenceCount.EntityId)
					                           .SelectCount(qc => qc.Id)
					                           .WithAlias(() => referenceCount.UsageCount))
					       .TransformUsing(Transformers.AliasToBean<ReferenceCount>());

				return parametersQuery.List<ReferenceCount>();
			}
		}

		private bool AreTransformersAndFiltersSupported()
		{
			Version databaseSchemaVersion = GetDatabaseSchemaVersion();

			return databaseSchemaVersion != null &&
			       databaseSchemaVersion >= DdxSchemaVersion.FiltersAndTransformers;
		}

		#endregion
	}
}
