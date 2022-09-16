using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.AttributeDependencies.Repositories;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Persistence.Core.AttributeDependencies
{
	[UsedImplicitly]
	public class AttributeDependencyRepository :
		NHibernateRepository<AttributeDependency>, IAttributeDependencyRepository
	{
		public AttributeDependency Get(Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			using (ISession session = OpenSession(true))
			{
				var entity = session.CreateQuery(
					                    "from AttributeDependency ad where ad.Dataset = :dataset")
				                    .SetEntity("dataset", dataset)
				                    .UniqueResult<AttributeDependency>();

				return entity;
			}
		}

		public IList<AttributeDependency> GetByModelId(int modelId)
		{
			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
								  "from AttributeDependency ad where ad.Dataset.Model.Id = :modelId")
				              .SetInt32("modelId", modelId)
				              .List<AttributeDependency>();
			}
		}
	}
}
