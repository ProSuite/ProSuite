using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

			string categoryProperty = nameof(InstanceConfiguration.Category);
			ICriterion categoryFilter =
				category == null
					? (ICriterion) new NullExpression(categoryProperty)
					: Restrictions.Eq(categoryProperty, category);

			T instanceConfigAlias = null;
			DatasetTestParameterValue parameterValueAlias = null;

			var parametersQuery =
				session.QueryOver<T>(() => instanceConfigAlias)
				       .Where(categoryFilter)
				       .JoinAlias(i => instanceConfigAlias.ParameterValues,
				                  () => parameterValueAlias)
				       //.Where(Restrictions.Eq("parameterValueAlias.class", nameof(DatasetTestParameterValue)))
				       .Where(() => parameterValueAlias.GetType() ==
				                    typeof(DatasetTestParameterValue))
				       .Select(i => i.AsEntity<T>(), i => parameterValueAlias.AsEntity());

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

			var result =
				session.QueryOver<T>(() => instanceConfigAlias)
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
				session.QueryOver<T>(() => instanceConfigAlias)
				       //.Where(categoryFilter)
				       .JoinAlias(i => instanceConfigAlias.ParameterValues,
				                  () => parameterValueAlias)
				       //.Where(Restrictions.Eq("parameterValueAlias.class", nameof(DatasetTestParameterValue)))
				       .Where(() => parameterValueAlias.GetType() ==
				                    typeof(DatasetTestParameterValue))
				       .And(parameterExpression)
				       .Select(i => i.AsEntity<T>(), i => parameterValueAlias.AsEntity());

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
	}
}
