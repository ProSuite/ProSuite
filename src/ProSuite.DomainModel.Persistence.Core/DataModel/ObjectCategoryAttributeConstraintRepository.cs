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
	public class ObjectCategoryAttributeConstraintRepository :
		NHibernateRepository<ObjectCategoryAttributeConstraint>,
		IObjectCategoryAttributeConstraintRepository
	{
		#region IObjectCategoryAttributeConstraintRepository Members

		/// <summary>
		/// Gets all object category attribute constraints for the specified dataset.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <returns></returns>
		public IList<ObjectCategoryAttributeConstraint> Get(IDdxDataset dataset)
		{
			return Get<ObjectCategoryAttributeConstraint>(dataset);
		}

		public IList<T> Get<T>(IDdxDataset dataset)
			where T : ObjectCategoryAttributeConstraint
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			if (! (dataset is ObjectDataset))
			{
				return new List<T>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateCriteria(typeof(T))
				              .CreateCriteria("ObjectCategory").Add(
					              Restrictions.Eq("ObjectDataset", dataset))
				              .List<T>();
			}
		}

		#endregion
	}
}
