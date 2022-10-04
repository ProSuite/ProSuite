using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	[UsedImplicitly]
	public class QualitySpecificationRepository : NHibernateRepository<QualitySpecification>,
	                                              IQualitySpecificationRepository
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region IQualitySpecificationRepository Members

		public IList<QualitySpecification> Get(IList<Dataset> datasets,
		                                       bool excludeHidden = false)
		{
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			List<int> datasetIds = datasets.Select(dataset => dataset.Id).ToList();

			return Get(datasetIds, excludeHidden);
		}

		public IList<QualitySpecification> Get(IList<int> datasetIds,
		                                       bool excludeHidden)
		{
			// Get all transformer IDs that directly or indirectly reference one of the provided
			// dataset ids.
			// Then get all specs that contain a condition parameter that either references one
			// of the provided datasets directly or one of the calculated transformers.

			// Hierarchical queries are not supported in nhibernate.

			if (datasetIds.Count <= 0)
			{
				return new List<QualitySpecification>();
			}

			using (ISession session = OpenSession(true))
			{
				QualitySpecification qSpecAlias = null;
				QualitySpecificationElement element = null;
				QualityCondition qConAlias = null;
				DatasetTestParameterValue paramValueAlias = null;

				Stopwatch watch = _msg.DebugStartTiming();

				Expression<Func<bool>> noRefData = () => ! paramValueAlias.UsedAsReferenceData;

				var transformerIds =
					DatasetParameterFetchingUtils.GetAllTransformerIdsForDatasets(
						session, datasetIds, excludeReferenceData: true).ToArray();

				_msg.DebugStopTiming(watch, "Extracted {0} transformers depending on {1} datasets",
				                     transformerIds.Length, datasetIds.Count);

				int[] datasetIdArray = datasetIds.ToArray();

				watch = _msg.DebugStartTiming();

				ICriterion criterion =
					transformerIds.Length > 0
						? Restrictions.Or(
							Restrictions.On<DatasetTestParameterValue>(
								p => paramValueAlias.DatasetValue.Id).IsIn(datasetIdArray),
							Restrictions.On<DatasetTestParameterValue>(
								p => paramValueAlias.ValueSource.Id).IsIn(transformerIds))
						: Restrictions.On<DatasetTestParameterValue>(
							p => paramValueAlias.DatasetValue.Id).IsIn(datasetIdArray);

				var qSpecQuery =
					session.QueryOver(() => qSpecAlias)
					       .JoinQueryOver(() => qSpecAlias.Elements, () => element)
					       .JoinQueryOver(() => element.QualityCondition, () => qConAlias)
					       .JoinQueryOver(() => qConAlias.ParameterValues, () => paramValueAlias)
					       .Where(noRefData)
					       .And(criterion)
					       .TransformUsing(Transformers.DistinctRootEntity);

				if (excludeHidden)
				{
					qSpecQuery = qSpecQuery.Where(() => ! qSpecAlias.Hidden);
				}

				IList<QualitySpecification> result = qSpecQuery.List();

				_msg.DebugStopTiming(
					watch,
					"Found {0} specifications depending directly or indirectly on {1} datasets",
					result.Count, datasetIds.Count);

				return result;
			}
		}

		public IList<QualitySpecification> Get(QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			if (! qualityCondition.IsPersistent)
			{
				return new List<QualitySpecification>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "select distinct qspec " +
					              "  from QualitySpecification qspec " +
					              "  join qspec.Elements elem " +
					              " where elem.QualityCondition = :qualityCondition")
				              .SetEntity("qualityCondition", qualityCondition)
				              .List<QualitySpecification>();
			}
		}

		public QualitySpecification Get(string name)
		{
			return GetUniqueResult("Name", name, true);
		}

		public IList<QualitySpecification> Get(
			DataQualityCategory category,
			bool includeSubCategories = false)
		{
			if (category != null && ! category.IsPersistent)
			{
				return new List<QualitySpecification>();
			}

			if (category == null && includeSubCategories)
			{
				return GetAll();
			}

			using (ISession session = OpenSession(true))
			{
				List<QualitySpecification> result = Get(category, session).ToList();

				if (! includeSubCategories)
				{
					return result;
				}

				AddSubCategoryQualitySpecifications(category, result, session);

				return result;
			}
		}

		public IList<QualitySpecification> Get(IEnumerable<DataQualityCategory> categories)
		{
			Assert.ArgumentNotNull(categories, nameof(categories));

			var uniqueCategoryIds = new HashSet<int>();
			foreach (DataQualityCategory category in categories.Where(c => c.IsPersistent))
			{
				uniqueCategoryIds.Add(category.Id);
			}

			if (uniqueCategoryIds.Count == 0)
			{
				return new List<QualitySpecification>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "select qspec " +
					              "  from QualitySpecification qspec " +
					              " where qspec.Category.Id in (:categoryIds)")
				              .SetParameterList("categoryIds",
				                                uniqueCategoryIds)
				              .List<QualitySpecification>();
			}
		}

		private static void AddSubCategoryQualitySpecifications(
			[NotNull] DataQualityCategory category,
			[NotNull] List<QualitySpecification> result,
			[NotNull] ISession session)
		{
			foreach (DataQualityCategory subCategory in category.SubCategories)
			{
				result.AddRange(Get(subCategory, session));

				AddSubCategoryQualitySpecifications(subCategory, result, session);
			}
		}

		[NotNull]
		private static IEnumerable<QualitySpecification> Get(
			[CanBeNull] DataQualityCategory category, [NotNull] ISession session)
		{
			ICriteria criteria = session.CreateCriteria(typeof(QualitySpecification));

			const string categoryProperty = "Category";

			ICriterion filterCriterion =
				category == null
					? (ICriterion) new NullExpression(categoryProperty)
					: Restrictions.Eq(categoryProperty, category);

			return criteria.Add(filterCriterion)
			               .List<QualitySpecification>();
		}

		#endregion
	}
}
