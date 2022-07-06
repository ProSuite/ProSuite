using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Criterion;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	internal static class DatasetParameterFetchingUtils
	{
		[NotNull]
		public static IList<int> GetDeletedDatasetParameterIds([NotNull] ISession session)
		{
			IQueryOver<DatasetTestParameterValue, Dataset> parametersQuery =
				session.QueryOver<DatasetTestParameterValue>()
				       .Select(p => p.Id)
				       .Where(p => p.DatasetValue != null)
				       .JoinQueryOver<Dataset>(p => p.DatasetValue)
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
		public static IDictionary<int, HashSet<int>> GetDatasetParameterIdsByInstanceConfigurationId<T>(
				[NotNull] ISession session,
				[CanBeNull] DataQualityCategory category) where T : InstanceConfiguration
		{
			//IQuery query = category == null
			//	               ? session.CreateQuery("select qcon.Id, paramVal.Id " +
			//	                                     "  from QualityCondition qcon " +
			//	                                     " inner join qcon.ParameterValues paramVal " +
			//	                                     " where qcon.Category is null " +
			//	                                     "   and paramVal.class = DatasetTestParameterValue")
			//	               : session.CreateQuery("select qcon.Id, paramVal.Id " +
			//	                                     "  from QualityCondition qcon " +
			//	                                     " inner join qcon.ParameterValues paramVal " +
			//	                                     " where qcon.Category = :category " +
			//	                                     "   and paramVal.class = DatasetTestParameterValue")
			//	                        .SetEntity("category", category);

			const string categoryProperty = "Category";
			ICriterion categoryFilter =
				category == null
					? (ICriterion) new NullExpression(categoryProperty)
					: Restrictions.Eq(categoryProperty, category);

			T instanceConfigAlias = null;
			TestParameterValue parameterValueAlias = null;

			var parametersQuery =
				session.QueryOver<T>(() => instanceConfigAlias)
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

			// TODO: Category
			//const string categoryProperty = "Category";
			//ICriterion categoryFilter =
			//	category == null
			//		? (ICriterion)new NullExpression(categoryProperty)
			//		: Restrictions.Eq(categoryProperty, category);

			T instanceConfigAlias = null;
			DatasetTestParameterValue parameterValueAlias = null;

			var parametersQuery =
				session.QueryOver<T>(() => instanceConfigAlias)
				       //.Where(categoryFilter)
				       .JoinAlias(i => instanceConfigAlias.ParameterValues,
				                  () => parameterValueAlias)
				       //.Where(Restrictions.Eq("parameterValueAlias.class", nameof(DatasetTestParameterValue)))
				       .Where(() => parameterValueAlias.GetType() == typeof(DatasetTestParameterValue))
				       .Select(i => i.AsEntity<T>(), i => parameterValueAlias.AsEntity());

			//IEnumerable<KeyValuePair<T, DatasetTestParameterValue>> flatList = parametersQuery
			//	.List<object[]>()
			//	.Select(o => new KeyValuePair<T, DatasetTestParameterValue>(
			//		        (T) o[0], (DatasetTestParameterValue) o[1]));

			//foreach (var grouping in flatList.GroupBy(kvp => kvp.Key.Id))
			//{
			//	grouping.
			//}

			//foreach (var pair in flatList)
			//{
			//	T instanceConfig = (T)pair[0];
			//	DatasetTestParameterValue parameterValue = (DatasetTestParameterValue)pair[1];

			//	if (!result.TryGetValue(instanceConfig.Id, out List<DatasetTestParameterValue> parameters))
			//	{
			//		parameters = new List<DatasetTestParameterValue>();
			//		result.Add(instanceConfig.Id, parameters);
			//	}

			//	parameters.Add(parameterValue);
			//}


			var parametersByConfigId = new Dictionary<int, List<DatasetTestParameterValue>>();
			var configsById = new Dictionary<int, T>();
			foreach (object[] pair in parametersQuery.List<object[]>())
			{
				T instanceConfig = (T)pair[0];
				DatasetTestParameterValue parameterValue = (DatasetTestParameterValue)pair[1];

				configsById[instanceConfig.Id] = instanceConfig;

				if (!parametersByConfigId.TryGetValue(instanceConfig.Id, out List<DatasetTestParameterValue> parameters))
				{
					parameters = new List<DatasetTestParameterValue>();
					parametersByConfigId.Add(instanceConfig.Id, parameters);
				}

				parameters.Add(parameterValue);
			}

			foreach (KeyValuePair<int, List<DatasetTestParameterValue>> paramsById in parametersByConfigId)
			{
				int id = paramsById.Key;

				yield return new KeyValuePair<T, List<DatasetTestParameterValue>>(
					configsById[id], paramsById.Value);
			}


			//IDictionary<int, List<DatasetTestParameterValue>> dsParamsByConfigId =
			//	DatasetParameterFetchingUtils.GetDatasetParametersByInstanceConfigId<T>(
			//		session, category);






			//IDictionary<int, HashSet<int>> dsParamsByQualityCondition =
			//	DatasetParameterFetchingUtils.GetDatasetParameterIdsByInstanceConfigurationId<T>(
			//		session, category);

			//if (dsParamsByQualityCondition.Count == 0)
			//{
			//	return result;
			//}

			//// flatmap to get the complete parameter id list
			//var allDsParamIds = new HashSet<int>(
			//	dsParamsByQualityCondition.SelectMany(pair => pair.Value));

			//Dictionary<int, DatasetTestParameterValue> datasetValuesById =
			//	DatasetParameterFetchingUtils.GetDatasetTestParameterValuesById(session, allDsParamIds);

			//foreach (KeyValuePair<int, HashSet<int>> pair in dsParamsByQualityCondition)
			//{
			//	var values = new List<DatasetTestParameterValue>();

			//	foreach (int paramValId in pair.Value)
			//	{
			//		DatasetTestParameterValue value;
			//		if (datasetValuesById.TryGetValue(paramValId, out value))
			//		{
			//			values.Add(value);
			//		}
			//	}

			//	result.Add(pair.Key, values);
			//}

			//return result;
		}
	}
}
