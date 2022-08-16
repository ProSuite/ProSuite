using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Criterion;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	[UsedImplicitly]
	public class QualitySpecificationRepository :
		NHibernateRepository<QualitySpecification>,
		IQualitySpecificationRepository
	{
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
			// TODO: Include indirectly referenced datasets via transformers
			// Get all IDs of all Dataset Test parameter values for the specified ids to find
			// out if
			// - They are all referenced directly by a condition -> done, use conditions' specs
			// - Some are referenced by a transformer -> Build the tree, e.g. by fully loading all
			//   transformer configs and all dataset parameters with ValueSource not null.
			//   Hierarchical queries are not supported in hql.

			if (datasetIds.Count <= 0)
			{
				return new List<QualitySpecification>();
			}

			using (ISession session = OpenSession(true))
			{
				// NOTE: for handling dataset lists larger than 1000 elements: 
				// see QualityVerificationRepository.Get(Model model)

				string hql = string.Format(
					"select distinct qspec " +
					"  from QualitySpecification qspec " +
					"  join qspec.Elements elem " +
					"  join elem.QualityCondition.ParameterValues paramVal " +
					" where paramVal.Id in (select paramVal.Id " +
					"                         from DatasetTestParameterValue paramVal " +
					"                        where paramVal.DatasetValue.Id in (:datasetIds)) " +
					"                          and paramVal.UsedAsReferenceData = {0}{1}",
					GetHqlLiteral(false, session),
					excludeHidden
						? string.Format(" and qspec.Hidden <> {0}", GetHqlLiteral(true, session))
						: string.Empty);

				return session.CreateQuery(hql)
				              .SetParameterList("datasetIds", datasetIds)
				              .List<QualitySpecification>();
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
