using System.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class DatasetRepository : NHibernateRepository<Dataset>, IDatasetRepository
	{
		#region IDatasetRepository Members

		public IList<Dataset> Get(string name)
		{
			const bool includeDeleted = false;
			return Get(name, includeDeleted);
		}

		public IList<Dataset> Get(string name, bool includeDeleted)
		{
			using (ISession session = OpenSession(true))
			{
				ICriteria criteria = session.CreateCriteria(typeof(Dataset));

				const bool ignoreCase = true;
				criteria.Add(GetEqualityExpression("Name", name, ignoreCase));

				if (! includeDeleted)
				{
					criteria.Add(GetEqualityExpression("Deleted", false));
				}

				return criteria.List<Dataset>();
			}
		}

		public Dataset GetByAbbreviation(DdxModel model, string abbreviation)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(abbreviation, nameof(abbreviation));

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "from Dataset ds " +
					              "where ds.Model = :model " +
					              "  and upper(ds.Abbreviation) = :abbreviation")
				              .SetEntity("model", model)
				              .SetString("abbreviation", abbreviation.ToUpper())
				              .UniqueResult<Dataset>();
			}
		}

		public IList<T> Get<T>(DdxModel model) where T : Dataset
		{
			using (ISession session = OpenSession(true))
			{
				AssertInTransaction(session);

				return session.CreateCriteria(typeof(T))
				              .Add(Restrictions.Eq("Model", model))
				              .List<T>();
			}
		}

		public IList<T> GetAll<T>() where T : Dataset
		{
			return GetAllCore<T>();
		}

		public IList<Dataset> Get(DatasetCategory datasetCategory)
		{
			Assert.ArgumentNotNull(datasetCategory, nameof(datasetCategory));

			if (! datasetCategory.IsPersistent)
			{
				return new List<Dataset>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "from Dataset ds " +
					              "where ds.DatasetCategory = :datasetCategory")
				              .SetEntity("datasetCategory", datasetCategory)
				              .List<Dataset>();
			}
		}

		#endregion
	}
}
