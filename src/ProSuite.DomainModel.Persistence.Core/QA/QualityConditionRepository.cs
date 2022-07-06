using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	[UsedImplicitly]
	public class QualityConditionRepository : NHibernateRepository<QualityCondition>,
	                                          IQualityConditionRepository
	{
		private const int _maxInParameterCount = 1000;

		#region IQualityConditionRepository Members

		public QualityCondition Get(string name)
		{
			return GetUniqueResult("Name", name, ignoreCase: true);
		}

		public IDictionary<int, int> GetReferencingQualitySpecificationCount()
		{
			using (ISession session = OpenSession(true))
			{
				IList list = session.CreateQuery(
					                    "select element.QualityCondition.Id, count(distinct qspec.Id) " +
					                    "  from QualitySpecification qspec " +
					                    "   inner join qspec.Elements as element " +
					                    " group by element.QualityCondition.Id")
				                    .List();

				var result = new Dictionary<int, int>(list.Count);
				foreach (object[] values in list)
				{
					var qualityConditionId = (int) values[0];
					int qualitySpecificationCount = Convert.ToInt32(values[1]);

					result.Add(qualityConditionId, qualitySpecificationCount);
				}

				return result;
			}
		}

		public IList<QualityCondition> Get(IList<int> idList)
		{
			using (ISession session = OpenSession(true))
			{
				if (idList.Count <= _maxInParameterCount)
				{
					// use a NOT IN query to return the result
					return session.CreateQuery(
						              "select qcon " +
						              "  from QualityCondition qcon " +
						              " where id in (:qualityConditionIds)")
					              .SetParameterList("qualityConditionIds", idList)
					              .List<QualityCondition>();
				}

				// too many affected quality conditions; get all and filter locally
				return GetAll()
				       .Where(condition => idList.Contains(condition.Id))
				       .ToList();
			}
		}

		public IList<QualityCondition> Get(TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			if (! testDescriptor.IsPersistent)
			{
				return new List<QualityCondition>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "select qc " +
					              "  from QualityCondition qc " +
					              " where qc.TestDescriptor = :testDescriptor")
				              .SetEntity("testDescriptor", testDescriptor)
				              .List<QualityCondition>();
			}
		}

		public IList<QualityCondition> Get(DdxModel model)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			if (! model.IsPersistent)
			{
				return new List<QualityCondition>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "select distinct qcon " +
					              "  from QualityCondition qcon " +
					              "  join qcon.ParameterValues paramVal " +
					              " where paramVal.Id in (select paramVal.Id " +
					              "                     from DatasetTestParameterValue paramVal " +
					              "                    where paramVal.DatasetValue.Model = :model)")
				              .SetEntity("model", model)
				              .List<QualityCondition>();
			}
		}

		public IList<string> GetNames(bool includeQualityConditionsBasedOnDeletedDatasets = false)
		{
			if (includeQualityConditionsBasedOnDeletedDatasets)
			{
				using (ISession session = OpenSession(true))
				{
					return session.CreateQuery("select qc.Name from QualityCondition qc")
					              .List<string>();
				}
			}

			// exclude qcon's based on deleted datasets
			using (ISession session = OpenSession(true))
			{
				IList<int> deletedDatasetParameterIds = DatasetParameterFetchingUtils.GetDeletedDatasetParameterIds(session);

				if (deletedDatasetParameterIds.Count == 0)
				{
					return session.CreateQuery("select qc.Name from QualityCondition qc")
					              .List<string>();
				}

				if (deletedDatasetParameterIds.Count > _maxInParameterCount)
				{
					return GetAllNotInvolvingDeletedDatasets().Select(qc => qc.Name)
					                                          .ToList();
				}

				// use a single query to return result
				return session.CreateQuery(
					              "select qcon.Name " +
					              "  from QualityCondition qcon " +
					              " where not exists (from qcon.ParameterValues v " +
					              "                  where v.Id in (:deletedDatasetParameterIds))")
				              .SetParameterList("deletedDatasetParameterIds",
				                                deletedDatasetParameterIds)
				              .List<string>();
			}
		}

		public IList<QualityCondition> GetAll(bool fetchParameterValues)
		{
			if (! fetchParameterValues)
			{
				return GetAll();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(QualityCondition))
				              .Fetch(SelectMode.Fetch, "ParameterValues")
				              .SetResultTransformer(new DistinctRootEntityResultTransformer())
				              .List<QualityCondition>();
			}
		}

		public IList<QualityCondition> GetAllNotInvolvingDeletedDatasets()
		{
			using (ISession session = OpenSession(true))
			{
				IList<int> deletedDatasetParameterIds = DatasetParameterFetchingUtils.GetDeletedDatasetParameterIds(session);

				if (deletedDatasetParameterIds.Count == 0)
				{
					return GetAll();
				}

				if (deletedDatasetParameterIds.Count <= _maxInParameterCount)
				{
					// use a single query to return result
					return session.CreateQuery(
						              "select qcon " +
						              "  from QualityCondition qcon " +
						              " where not exists (from qcon.ParameterValues v " +
						              "                  where v.Id in (:deletedDatasetParameterIds))")
					              .SetParameterList("deletedDatasetParameterIds",
					                                deletedDatasetParameterIds)
					              .List<QualityCondition>();
				}

				// get the list of quality condition ids that involve one of the 
				// parameters that is based on a deleted dataset
				HashSet<int> deletedQualityConditionIds =
					GetQualityConditionIdsForParameterIds(session,
					                                      deletedDatasetParameterIds,
					                                      _maxInParameterCount);

				if (deletedQualityConditionIds.Count == 0)
				{
					return GetAll();
				}

				if (deletedQualityConditionIds.Count <= _maxInParameterCount)
				{
					// use a NOT IN query to return the result
					return session.CreateQuery(
						              "select qcon " +
						              "  from QualityCondition qcon " +
						              " where id not in (:deletedQualityConditionIds)")
					              .SetParameterList("deletedQualityConditionIds",
					                                deletedQualityConditionIds)
					              .List<QualityCondition>();
				}

				// too many affected quality conditions; get all and filter locally
				return GetAll()
				       .Where(condition => ! deletedQualityConditionIds.Contains(condition.Id))
				       .ToList();
			}
		}

		public IDictionary<QualityCondition, IList<DatasetTestParameterValue>>
			GetWithDatasetParameterValues(DataQualityCategory category = null)
		{
			var result = new Dictionary<QualityCondition, IList<DatasetTestParameterValue>>();

			if (category != null && ! category.IsPersistent)
			{
				return result;
			}

			using (ISession session = OpenSession(true))
			{
				//// TEST:

				//var paramsByConfig = DatasetParameterFetchingUtils
				//	.GetDatasetParameterValuesByConfiguration<QualityCondition>(category, session);

				//int countNew = paramsByConfig.Count();

				//// END TEST:
				
				Dictionary<int, QualityCondition> conditionsById =
					Get(category, session)
						.ToDictionary(qcon => qcon.Id);

				foreach (KeyValuePair<int, List<DatasetTestParameterValue>> pair in
					GetDatasetValuesByConditionId(category, session))
				{
					int conditionId = pair.Key;
					List<DatasetTestParameterValue> values = pair.Value;

					result.Add(conditionsById[conditionId], values);
				}

				//Assert.AreEqual(result.Count, countNew, "Query error in config count!");

				return result;
			}
		}

		public HashSet<int> GetIdsInvolvingDeletedDatasets()
		{
			using (ISession session = OpenSession(true))
			{
				return GetIdsInvolvingDeletedDatasets(session);
			}
		}

		public IList<QualityCondition> Get(
			DataQualityCategory category,
			bool includeQualityConditionsBasedOnDeletedDatasets = true)
		{
			if (category != null && ! category.IsPersistent)
			{
				return new List<QualityCondition>();
			}

			using (ISession session = OpenSession(true))
			{
				return Get(category, session, includeQualityConditionsBasedOnDeletedDatasets);
			}
		}

		public IList<QualityCondition> Get(IEnumerable<DataQualityCategory> categories)
		{
			Assert.ArgumentNotNull(categories, nameof(categories));

			var uniqueCategoryIds = new HashSet<int>();
			foreach (DataQualityCategory category in categories.Where(c => c.IsPersistent))
			{
				uniqueCategoryIds.Add(category.Id);
			}

			if (uniqueCategoryIds.Count == 0)
			{
				return new List<QualityCondition>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "select qcon " +
					              "  from QualityCondition qcon " +
					              " where qcon.Category.Id in (:categoryIds)")
				              .SetParameterList("categoryIds",
				                                uniqueCategoryIds)
				              .List<QualityCondition>();
			}
		}

		[NotNull]
		private static HashSet<int> GetQualityConditionIdsInvolvingDeletedDatasets(
			[NotNull] ISession session)
		{
			IList<int> deletedDatasetParameterIds = DatasetParameterFetchingUtils.GetDeletedDatasetParameterIds(session);

			var result = new HashSet<int>();

			if (deletedDatasetParameterIds.Count == 0)
			{
				return result;
			}

			if (deletedDatasetParameterIds.Count <= _maxInParameterCount)
			{
				// use a single query to return result
				IList<int> ids = session.CreateQuery(
					                        "select qcon.Id " +
					                        "  from QualityCondition qcon " +
					                        " where exists (from qcon.ParameterValues v " +
					                        "                  where v.Id in (:deletedDatasetParameterIds))")
				                        .SetParameterList("deletedDatasetParameterIds",
				                                          deletedDatasetParameterIds)
				                        .List<int>();

				foreach (int id in ids)
				{
					result.Add(id);
				}

				return result;
			}

			// get the list of quality condition ids that involve one of the 
			// parameters that is based on a deleted dataset
			HashSet<int> deletedQualityConditionIds =
				GetQualityConditionIdsForParameterIds(session,
				                                      deletedDatasetParameterIds,
				                                      _maxInParameterCount);

			foreach (int id in deletedQualityConditionIds)
			{
				result.Add(id);
			}

			return result;
		}

		[NotNull]
		private static HashSet<int> GetIdsInvolvingDeletedDatasets([NotNull] ISession session)
		{
			IList<int> datasetParameterIds = DatasetParameterFetchingUtils.GetDeletedDatasetParameterIds(session);

			// New implementation to be tested
			HashSet<int> queryOverResult =
				DatasetParameterFetchingUtils.GetInstanceConfigurationIdsForParameterIds<QualityCondition>(
					session, datasetParameterIds, _maxInParameterCount);

			HashSet<int> originalResult =
				GetQualityConditionIdsForParameterIds(session, datasetParameterIds,
				                                      _maxInParameterCount);

			Assert.AreEqual(originalResult.Count, queryOverResult.Count,
			                "Implementation change results in result difference");

			return originalResult;
		}

		[NotNull]
		private static IEnumerable<KeyValuePair<int, List<DatasetTestParameterValue>>>
			GetDatasetValuesByConditionId(
				[CanBeNull] DataQualityCategory category,
				[NotNull] ISession session)
		{
			var result = new Dictionary<int, List<DatasetTestParameterValue>>();

			IDictionary<int, HashSet<int>> dsParamsByQualityCondition =
				GetDatasetParameterIdsByQualityConditionId(session, category);

			if (dsParamsByQualityCondition.Count == 0)
			{
				return result;
			}

			// flatmap to get the complete parameter id list
			var allDsParamIds = new HashSet<int>(
				dsParamsByQualityCondition.SelectMany(pair => pair.Value));

			Dictionary<int, DatasetTestParameterValue> datasetValuesById =
				GetDatasetTestParameterValuesById(session, allDsParamIds);

			foreach (KeyValuePair<int, HashSet<int>> pair in dsParamsByQualityCondition)
			{
				var values = new List<DatasetTestParameterValue>();

				foreach (int paramValId in pair.Value)
				{
					DatasetTestParameterValue value;
					if (datasetValuesById.TryGetValue(paramValId, out value))
					{
						values.Add(value);
					}
				}

				result.Add(pair.Key, values);
			}

			return result;
		}

		[NotNull]
		private static Dictionary<int, DatasetTestParameterValue>
			GetDatasetTestParameterValuesById(
				[NotNull] ISession session,
				[NotNull] IEnumerable<int> allDsParamIds)
		{
			var inExpressions =
				CollectionUtils.Split(allDsParamIds, 1000)
				               .Select(ids => ids.Cast<object>().ToList())
				               .Where(ids => ids.Count > 0)
				               .Select(ids => new InExpression("Id", ids.ToArray()))
				               .ToList();

			var result = new List<DatasetTestParameterValue>();

			foreach (InExpression inExpression in inExpressions)
			{
				result.AddRange(session.CreateCriteria(typeof(DatasetTestParameterValue))
				                       .Add(inExpression)
				                       .List<DatasetTestParameterValue>());
			}

			return result.ToDictionary(param => param.Id);
		}

		[NotNull]
		private static IDictionary<int, HashSet<int>>
			GetDatasetParameterIdsByQualityConditionId(
				[NotNull] ISession session,
				[CanBeNull] DataQualityCategory category)
		{
			IQuery query = category == null
				               ? session.CreateQuery("select qcon.Id, paramVal.Id " +
				                                     "  from QualityCondition qcon " +
				                                     " inner join qcon.ParameterValues paramVal " +
				                                     " where qcon.Category is null " +
				                                     "   and paramVal.class = DatasetTestParameterValue")
				               : session.CreateQuery("select qcon.Id, paramVal.Id " +
				                                     "  from QualityCondition qcon " +
				                                     " inner join qcon.ParameterValues paramVal " +
				                                     " where qcon.Category = :category " +
				                                     "   and paramVal.class = DatasetTestParameterValue")
				                        .SetEntity("category", category);

			IDictionary<int, HashSet<int>> result = new Dictionary<int, HashSet<int>>();

			foreach (object item in query.List())
			{
				var pair = (object[]) item;

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
		private static IList<QualityCondition> Get(
			[CanBeNull] DataQualityCategory category,
			[NotNull] ISession session,
			bool includeQualityConditionsBasedOnDeletedDatasets = true)
		{
			ICriteria criteria = session.CreateCriteria(typeof(QualityCondition));

			const string categoryProperty = "Category";

			ICriterion filterCriterion =
				category == null
					? (ICriterion) new NullExpression(categoryProperty)
					: Restrictions.Eq(categoryProperty, category);

			IList<QualityCondition> all = criteria.Add(filterCriterion)
			                                      .List<QualityCondition>();

			if (all.Count == 0 || includeQualityConditionsBasedOnDeletedDatasets)
			{
				return all;
			}

			HashSet<int> excludedIds = GetQualityConditionIdsInvolvingDeletedDatasets(session);

			return all.Where(qc => ! excludedIds.Contains(qc.Id))
			          .ToList();
		}
		
		[NotNull]
		private static HashSet<int> GetQualityConditionIdsForParameterIds(
			[NotNull] ISession session,
			[NotNull] ICollection<int> parameterIds,
			int maxInParameterCount)
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
				IList<int> qualityConditionIds =
					session.CreateQuery(
						       "select qcon.Id " +
						       "  from QualityCondition qcon " +
						       " where exists (from qcon.ParameterValues v " +
						       "              where v.Id in (:subList))")
					       .SetParameterList("subList", subList)
					       .List<int>();

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

		#endregion
	}
}
