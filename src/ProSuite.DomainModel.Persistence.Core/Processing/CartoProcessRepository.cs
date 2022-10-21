using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Processing;
using ProSuite.DomainModel.Core.Processing.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Processing
{
	[UsedImplicitly]
	public class CartoProcessRepository : NHibernateRepository<CartoProcess>,
	                                      ICartoProcessRepository
	{
		#region ICartoProcessRepository Members

		public IList<CartoProcess> GetAll(CartoProcessType cartoProcessType)
		{
			Assert.ArgumentNotNull(cartoProcessType, nameof(cartoProcessType));

			if (! cartoProcessType.IsPersistent)
			{
				return new List<CartoProcess>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "from CartoProcess cp where cp.CartoProcessType = :cartoProcessType")
				              .SetEntity("cartoProcessType", cartoProcessType)
				              .List<CartoProcess>();
			}
		}

		public IList<CartoProcess> GetAll(DdxModel model)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "from CartoProcess cp where cp.Model = :model")
				              .SetEntity("model", model)
				              .List<CartoProcess>();
			}
		}

		#endregion
	}
}
