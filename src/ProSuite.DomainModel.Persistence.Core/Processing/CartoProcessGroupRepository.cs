using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.Commands;
using ProSuite.DomainModel.Core.Processing;
using ProSuite.DomainModel.Core.Processing.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Processing
{
	[UsedImplicitly]
	public class CartoProcessGroupRepository : NHibernateRepository<CartoProcessGroup>,
	                                           ICartoProcessGroupRepository
	{
		#region ICartoProcessGroupRepository Members

		public CartoProcessGroup Get(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "from CartoProcessGroup cpg where cpg.Name = :name")
				              .SetString("name", name)
				              .UniqueResult<CartoProcessGroup>();
			}
		}

		public IList<CartoProcessGroup> Get(CartoProcess cartoProcess)
		{
			Assert.ArgumentNotNull(cartoProcess, nameof(cartoProcess));

			if (! cartoProcess.IsPersistent)
			{
				return new List<CartoProcessGroup>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "select distinct cpg " +
					              "  from CartoProcessGroup cpg " +
					              "  join cpg.Processes proc " +
					              " where proc = :cartoProcess")
				              .SetEntity("cartoProcess", cartoProcess)
				              .List<CartoProcessGroup>();
			}
		}

		public CartoProcessGroup Get(ICommandDescriptor commandDescriptor)
		{
			Assert.ArgumentNotNull(commandDescriptor, nameof(commandDescriptor));

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "from CartoProcessGroup cpg where cpg.AssociatedCommand = :commandDescriptor")
				              .SetEntity("commandDescriptor", commandDescriptor)
				              .UniqueResult<CartoProcessGroup>();
			}
		}

		#endregion
	}
}
