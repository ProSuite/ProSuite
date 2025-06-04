using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	internal static class QualityRepositoryUtils
	{
		public static TestDescriptor GetDescriptorWithSameImplementation(
			[NotNull] ISession session,
			[NotNull] TestDescriptor testDescriptor)
		{
			ICriteria criteria = session.CreateCriteria(typeof(TestDescriptor));

			if (testDescriptor.TestClass != null)
			{
				criteria.Add(Restrictions.And(
					             Restrictions.Eq("TestClass", testDescriptor.TestClass),
					             Restrictions.Eq("ConstructorId",
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

		public static InstanceDescriptor GetWithSameImplementation<T>(
			[NotNull] ISession session,
			[NotNull] T descriptor) where T : InstanceDescriptor
		{
			return session.QueryOver<T>()
			              .Where(i => i.Class == descriptor.Class &&
			                          i.ConstructorId == descriptor.ConstructorId)
			              .SingleOrDefault();
		}

		[NotNull]
		public static IList<int> GetDeletedDatasetParameterIds([NotNull] ISession session)
		{
			IQueryOver<DatasetTestParameterValue, Dataset> parametersQuery =
				session.QueryOver<DatasetTestParameterValue>()
				       .Select(p => p.Id)
				       .Where(p => p.DatasetValue != null)
				       .JoinQueryOver(p => p.DatasetValue)
				       //.Where(d => d != null)
				       .And(d => d.Deleted);

			return parametersQuery.List<int>();
		}

		[NotNull]
		public static HashSet<int> GetInstanceConfigurationIdsForParameterIds<T>(
			[NotNull] ISession session,
			[NotNull] ICollection<int> parameterIds,
			int maxInParameterCount) where T : InstanceConfiguration
		{
			Assert.ArgumentNotNull(parameterIds, nameof(parameterIds));
			Assert.ArgumentNotNull(session, nameof(session));

			var result = new HashSet<int>();

			if (parameterIds.Count == 0)
			{
				return result;
			}

			var first = true;
			foreach (IList<int> subList in
			         CollectionUtils.Split(parameterIds, maxInParameterCount))
			{
				var subQuery =
					QueryOver.Of<TestParameterValue>()
					         .Select(p => p.Id)
					         .WhereRestrictionOn(p => p.Id).IsIn(subList.ToArray());

				IList<int> qualityConditionIds =
					session.QueryOver<T>()
					       .Select(i => i.Id)
					       .JoinQueryOver<TestParameterValue>(i => i.ParameterValues)
					       .WithSubquery.WhereProperty(p => p.Id)
					       .In(subQuery)
					       .List<int>(); //.WhereExists(subQuery).List<int>();

				//IList<int> qualityConditionIds =
				//	session.CreateQuery(
				//		       "select qcon.Id " +
				//		       "  from QualityCondition qcon " +
				//		       " where exists (from qcon.ParameterValues v " +
				//		       "              where v.Id in (:subList))")
				//	       .SetParameterList("subList", subList)
				//	       .List<int>();

				foreach (int qualityConditionId in qualityConditionIds)
				{
					// avoid the cost of unnecessary Contains() call for first sub list
					// (quality condition ids should be unique *within* each sub list)
					if (first || ! result.Contains(qualityConditionId))
					{
						result.Add(qualityConditionId);
					}
				}

				first = false;
			}

			return result;
		}

		[NotNull]
		public static IDictionary<int, HashSet<int>>
			GetDatasetParameterIdsByInstanceConfigurationId<T>(
				[NotNull] ISession session,
				[CanBeNull] DataQualityCategory category) where T : InstanceConfiguration
		{
			string categoryProperty = nameof(InstanceConfiguration.Category);
			ICriterion categoryFilter =
				category == null
					? (ICriterion) new NullExpression(categoryProperty)
					: Restrictions.Eq(categoryProperty, category);

			T instanceConfigAlias = null;
			TestParameterValue parameterValueAlias = null;

			var parametersQuery =
				session.QueryOver(() => instanceConfigAlias)
				       .Select(i => i.Id)
				       .Where(categoryFilter)
				       .JoinAlias(i => instanceConfigAlias.ParameterValues,
				                  () => parameterValueAlias)
				       .Where(Restrictions.Eq("class", typeof(DatasetTestParameterValue)))
				       .Select(i => i.Id, i => parameterValueAlias.Id);

			IDictionary<int, HashSet<int>> result = new Dictionary<int, HashSet<int>>();

			foreach (object[] pair in parametersQuery.List<object[]>())
			{
				var qconId = (int) pair[0];
				var paramValId = (int) pair[1];

				HashSet<int> paramIds;
				if (! result.TryGetValue(qconId, out paramIds))
				{
					paramIds = new HashSet<int>();
					result.Add(qconId, paramIds);
				}

				paramIds.Add(paramValId);
			}

			return result;
		}

		[NotNull]
		public static IEnumerable<KeyValuePair<T, List<DatasetTestParameterValue>>>
			GetDatasetParameterValuesByConfiguration<T>(
				[CanBeNull] DataQualityCategory category,
				[NotNull] ISession session) where T : InstanceConfiguration
		{
			// TODO: Check SQL!

			string categoryProperty = nameof(InstanceConfiguration.Category);
			ICriterion categoryFilter =
				category == null
					? (ICriterion) new NullExpression(categoryProperty)
					: Restrictions.Eq(categoryProperty, category);

			T instanceConfigAlias = null;
			DatasetTestParameterValue parameterValueAlias = null;

			var parametersQuery =
				session.QueryOver(() => instanceConfigAlias)
				       .Where(categoryFilter)
				       .JoinAlias(i => instanceConfigAlias.ParameterValues,
				                  () => parameterValueAlias)
				       //.Where(Restrictions.Eq("parameterValueAlias.class", nameof(DatasetTestParameterValue)))
				       .Where(() => parameterValueAlias.GetType() ==
				                    typeof(DatasetTestParameterValue))
				       .Select(i => i.AsEntity(), i => parameterValueAlias.AsEntity());

			var parametersByConfigId = new Dictionary<int, List<DatasetTestParameterValue>>();
			var configsById = new Dictionary<int, T>();
			foreach (object[] pair in parametersQuery.List<object[]>())
			{
				T instanceConfig = (T) pair[0];
				DatasetTestParameterValue parameterValue = (DatasetTestParameterValue) pair[1];

				configsById[instanceConfig.Id] = instanceConfig;

				if (! parametersByConfigId.TryGetValue(instanceConfig.Id,
				                                       out List<DatasetTestParameterValue>
					                                           parameters))
				{
					parameters = new List<DatasetTestParameterValue>();
					parametersByConfigId.Add(instanceConfig.Id, parameters);
				}

				parameters.Add(parameterValue);
			}

			foreach (KeyValuePair<int, List<DatasetTestParameterValue>> paramsById in
			         parametersByConfigId)
			{
				int id = paramsById.Key;

				yield return new KeyValuePair<T, List<DatasetTestParameterValue>>(
					configsById[id], paramsById.Value);
			}
		}

		[NotNull]
		public static IList<T> GetParentConfiguration<T>(
			[NotNull] ISession session,
			Expression<Func<bool>> parameterExpression) where T : InstanceConfiguration
		{
			// TODO: Check SQL!

			T instanceConfigAlias = null;
			DatasetTestParameterValue parameterValueAlias = null;

			var result =
				session.QueryOver(() => instanceConfigAlias)
				       //.Where(categoryFilter)
				       .JoinAlias(i => instanceConfigAlias.ParameterValues,
				                  () => parameterValueAlias)
				       //.Where(Restrictions.Eq("parameterValueAlias.class", nameof(DatasetTestParameterValue)))
				       .Where(() => parameterValueAlias.GetType() ==
				                    typeof(DatasetTestParameterValue))
				       .And(parameterExpression).List();

			return result;
		}

		[NotNull]
		public static IEnumerable<KeyValuePair<T, List<DatasetTestParameterValue>>>
			GetParameterValuesByConfiguration<T>(
				[CanBeNull] DataQualityCategory category,
				[NotNull] ISession session,
				Expression<Func<bool>> parameterExpression) where T : InstanceConfiguration
		{
			// TODO: Check SQL!

			// TODO: Category
			//const string categoryProperty = "Category";
			//ICriterion categoryFilter =
			//	category == null
			//		? (ICriterion)new NullExpression(categoryProperty)
			//		: Restrictions.Eq(categoryProperty, category);

			T instanceConfigAlias = null;
			DatasetTestParameterValue parameterValueAlias = null;

			//Expression<Func<bool>> extraParameterExpression =
			//	() => p => parameterExpression(parameterValueAlias);

			var parametersQuery =
				session.QueryOver(() => instanceConfigAlias)
				       //.Where(categoryFilter)
				       .JoinAlias(i => instanceConfigAlias.ParameterValues,
				                  () => parameterValueAlias)
				       //.Where(Restrictions.Eq("parameterValueAlias.class", nameof(DatasetTestParameterValue)))
				       .Where(() => parameterValueAlias.GetType() ==
				                    typeof(DatasetTestParameterValue))
				       .And(parameterExpression)
				       .Select(i => i.AsEntity(), i => parameterValueAlias.AsEntity());

			var parametersByConfigId = new Dictionary<int, List<DatasetTestParameterValue>>();
			var configsById = new Dictionary<int, T>();
			foreach (object[] pair in parametersQuery.List<object[]>())
			{
				T instanceConfig = (T) pair[0];
				DatasetTestParameterValue parameterValue = (DatasetTestParameterValue) pair[1];

				configsById[instanceConfig.Id] = instanceConfig;

				if (! parametersByConfigId.TryGetValue(instanceConfig.Id,
				                                       out List<DatasetTestParameterValue>
					                                           parameters))
				{
					parameters = new List<DatasetTestParameterValue>();
					parametersByConfigId.Add(instanceConfig.Id, parameters);
				}

				parameters.Add(parameterValue);
			}

			foreach (KeyValuePair<int, List<DatasetTestParameterValue>> paramsById in
			         parametersByConfigId)
			{
				int id = paramsById.Key;

				yield return new KeyValuePair<T, List<DatasetTestParameterValue>>(
					configsById[id], paramsById.Value);
			}
		}

		#region Get Datasets from instance configuration (recursive)

		/// <summary>
		/// Returns all the IDs of transformers that directly or indirectly reference one of the
		/// datasets identified by the provided <paramref name="datasetIds"/>.
		/// </summary>
		public static IEnumerable<int> GetAllTransformerIdsForDatasets(
			[NotNull] ISession session,
			IEnumerable<int> datasetIds,
			bool excludeReferenceData = true)
		{
			int[] datasetIdArray = datasetIds.ToArray();

			DatasetTestParameterValue parameterValueAlias = null;

			TransformerConfiguration transformerAlias = null;
			var query =
				session.QueryOver(() => transformerAlias)
				       .JoinAlias(i => transformerAlias.ParameterValues,
				                  () => parameterValueAlias)
				       .Where(() => parameterValueAlias.GetType() ==
				                    typeof(DatasetTestParameterValue))
				       .And(() => parameterValueAlias.DatasetValue.Id.IsIn(datasetIdArray))
				       .And(() => ! parameterValueAlias.UsedAsReferenceData)
				       .Select(t => t.Id);

			if (excludeReferenceData)
			{
				query = query.Where(() => ! parameterValueAlias.UsedAsReferenceData);
			}

			IList<int> transformerIds = query.List<int>();

			List<int> allTransformerIds = new List<int>(transformerIds);

			// Walk up the dependency tree, max round trip count == depth of the tree
			while (transformerIds.Count > 0)
			{
				transformerIds = GetReferencedTransformerIdentifiers(session, transformerIds);

				allTransformerIds.AddRange(transformerIds);
			}

			return allTransformerIds;
		}

		public static IEnumerable<Dataset> GetAllReferencedDatasets(
			[NotNull] ISession session,
			[NotNull] IEnumerable<QualityCondition> qualityConditions,
			bool includeReferenceViaIssueFilters,
			Predicate<DatasetTestParameterValue> testParameterPredicate = null)
		{
			List<InstanceConfiguration> referencedTransformers =
				new List<InstanceConfiguration>();

			foreach (Dataset directlyReferenced in GetReferencedDatasets(
				         session, qualityConditions, referencedTransformers,
				         includeReferenceViaIssueFilters, testParameterPredicate))
			{
				yield return directlyReferenced;
			}

			List<int> transformerIds = referencedTransformers.Select(t => t.Id).ToList();
			while (transformerIds.Count > 0)
			{
				var referencedTransformerIds = new List<InstanceConfiguration>();
				foreach (Dataset referencedDataset in GetReferencedDatasets(
					         session, transformerIds, referencedTransformerIds,
					         testParameterPredicate))
				{
					yield return referencedDataset;
				}

				transformerIds = referencedTransformerIds.Select(t => t.Id).ToList();
			}
		}

		private static IList<int> GetReferencedTransformerIdentifiers(
			[NotNull] ISession session,
			[NotNull] IEnumerable<int> transformerIds)
		{
			TransformerConfiguration transformerAlias = null;
			DatasetTestParameterValue parameterValueAlias = null;

			var transformerIdArray = transformerIds.ToArray();

			IList<int> result =
				session.QueryOver(
					       () => transformerAlias)
				       .JoinAlias(i => transformerAlias.ParameterValues,
				                  () => parameterValueAlias)
				       .Where(() => parameterValueAlias.GetType() ==
				                    typeof(DatasetTestParameterValue))
				       .And(() => parameterValueAlias.ValueSource.Id.IsIn(transformerIdArray))
				       .Select(t => t.Id).List<int>();

			return result;
		}

		private static IEnumerable<Dataset> GetReferencedDatasets(
			[NotNull] ISession session,
			[NotNull] IEnumerable<QualityCondition> referencedByConditions,
			[NotNull] List<InstanceConfiguration> referencedTransformers,
			bool includeReferencesViaIssueFilters,
			[CanBeNull] Predicate<DatasetTestParameterValue> testParameterPredicate = null)
		{
			var conditionIds = referencedByConditions.Select(t => t.Id).ToArray();

			QualityCondition qualityConditionAlias = null;
			TestParameterValue testParamAlias = null;

			// Initial query, eagerly fetching transformers including their parameters.
			// NOTE: Do not restrict to only dataset parameter type, otherwise the scalar
			// parameters will be null in the ParameterValues list. However, the performance
			// would increase considerably by just loading the dataset test parameter values.
			IQueryOver<QualityCondition> parametersQuery =
				session.QueryOver(() => qualityConditionAlias)
				       .Fetch(SelectMode.FetchLazyProperties,
				              () => qualityConditionAlias.ParameterValues)
				       .JoinQueryOver(qc => qc.ParameterValues, () => testParamAlias)
				       .Fetch(SelectMode.FetchLazyProperties,
				              () => testParamAlias.ValueSource.ParameterValues)
				       .Where(() => qualityConditionAlias.Id.IsIn(conditionIds))
				       .TransformUsing(Transformers.DistinctRootEntity);

			if (includeReferencesViaIssueFilters)
			{
				// Also fetch issue filters (without parameters, because that extra round trip is probably rare
				// and the extra join probably not worth it) -> profile real-world data
				parametersQuery.Fetch(SelectMode.FetchLazyProperties,
				                      () => qualityConditionAlias.IssueFilterConfigurations);
			}

			var initialResult = parametersQuery.List<QualityCondition>();

			var returnedDatasetIds = new HashSet<int>();
			foreach (Dataset dataset in
			         GetReferencedDatasets(initialResult, referencedTransformers,
			                               testParameterPredicate))
			{
				if (! returnedDatasetIds.Contains(dataset.Id))
				{
					yield return dataset;
					returnedDatasetIds.Add(dataset.Id);
				}
			}

			if (includeReferencesViaIssueFilters)
			{
				var issueFilters = initialResult.SelectMany(c => c.IssueFilterConfigurations);

				foreach (Dataset dataset in
				         GetReferencedDatasets(issueFilters, referencedTransformers,
				                               testParameterPredicate))
				{
					if (! returnedDatasetIds.Contains(dataset.Id))
					{
						yield return dataset;
						returnedDatasetIds.Add(dataset.Id);
					}
				}
			}
		}

		private static IEnumerable<Dataset> GetReferencedDatasets(
			[NotNull] ISession session,
			[NotNull] ICollection<int> forTransformerIds,
			[NotNull] List<InstanceConfiguration> referencedTransformers,
			[CanBeNull] Predicate<DatasetTestParameterValue> testParameterPredicate = null)
		{
			TransformerConfiguration transformerConfigAlias = null;
			TestParameterValue testParamAlias = null;
			IQueryOver<TransformerConfiguration> transformersQuery =
				session.QueryOver(() => transformerConfigAlias)
				       .Fetch(SelectMode.FetchLazyProperties,
				              () => transformerConfigAlias.ParameterValues)
				       .JoinQueryOver(tc => tc.ParameterValues, () => testParamAlias)
				       .Fetch(SelectMode.FetchLazyProperties,
				              () => testParamAlias.ValueSource.ParameterValues)
				       .Where(() => transformerConfigAlias.Id.IsIn(
					              forTransformerIds.ToArray()));

			IEnumerable<InstanceConfiguration> resultList =
				transformersQuery.List<TransformerConfiguration>();

			foreach (Dataset dataset in GetReferencedDatasets(
				         resultList, referencedTransformers, testParameterPredicate))
			{
				yield return dataset;
			}
		}

		private static IEnumerable<Dataset> GetReferencedDatasets(
			[NotNull] IEnumerable<InstanceConfiguration> instanceConfigurations,
			[NotNull] ICollection<InstanceConfiguration> referencedTransformers,
			[CanBeNull] Predicate<DatasetTestParameterValue> testParameterPredicate = null)
		{
			foreach (InstanceConfiguration configuration in instanceConfigurations)
			{
				foreach (TestParameterValue parameterValue in configuration.ParameterValues)
				{
					if (TryGetDataset(parameterValue, testParameterPredicate, out Dataset dataset,
					                  out TransformerConfiguration referencedTransformer))
					{
						yield return dataset;
					}
					else if (referencedTransformer != null)
					{
						// These have already been fetched. The goal is not to find all referenced
						// transformers, but those which need loading of more upstream transformers
						// or datasets.
						foreach (TestParameterValue sourceParameterValue in referencedTransformer
							         .ParameterValues)
						{
							if (TryGetDataset(sourceParameterValue, testParameterPredicate,
							                  out Dataset sourceRefDataset,
							                  out TransformerConfiguration sourceRefTransformer))
							{
								yield return sourceRefDataset;
							}
							else if (sourceRefTransformer != null)
							{
								// not yet fetched, add for next round trip:
								referencedTransformers.Add(sourceRefTransformer);
							}
						}
					}
				}
			}
		}

		private static bool TryGetDataset(
			TestParameterValue testParameterValue,
			[CanBeNull] Predicate<DatasetTestParameterValue> testParameterPredicate,
			out Dataset result, out TransformerConfiguration transformerConfig)
		{
			result = null;
			transformerConfig = null;
			if (! (testParameterValue is DatasetTestParameterValue datasetParameterValue))
			{
				return false;
			}

			if (testParameterPredicate?.Invoke(datasetParameterValue) == false)
			{
				return false;
			}

			if (datasetParameterValue.DatasetValue != null)
			{
				result = datasetParameterValue.DatasetValue;
				return true;
			}

			if (testParameterValue.ValueSource != null)
			{
				transformerConfig = datasetParameterValue.ValueSource;
			}

			return false;
		}

		#endregion
	}
}
