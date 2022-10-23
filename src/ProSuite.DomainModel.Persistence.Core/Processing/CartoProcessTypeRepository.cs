using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.Processing;
using ProSuite.DomainModel.Core.Processing.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Processing
{
	[UsedImplicitly]
	public class CartoProcessTypeRepository : NHibernateRepository<CartoProcessType>,
	                                          ICartoProcessTypeRepository
	{
		#region ICartoProcessTypeRepository Members

		public IDictionary<string, int> GetReferencingCartoProcessesCount()
		{
			using (ISession session = OpenSession(true))
			{
				//Query "normal" CartoProcesses
				IList list = session.CreateQuery(
					                    "select cpt.Name, count(cp.CartoProcessType) " +
					                    "  from CartoProcess cp " +
					                    "   inner join cp.CartoProcessType as cpt " +
					                    " group by cpt.Name")
				                    .List();

				var result = new Dictionary<string, int>(list.Count);
				foreach (object[] values in list)
				{
					var name = (string) values[0];
					int cartoProcessCount = Convert.ToInt32(values[1]);

					result.Add(name, cartoProcessCount);
				}

				//Query Group CartoProcesses
				IList groupList = session.CreateQuery(
					                         "select cpt.Name, count(cpg.AssociatedGroupProcessType) " +
					                         "  from CartoProcessGroup cpg " +
					                         "   inner join cpg.AssociatedGroupProcessType as cpt " +
					                         " group by cpt.Name")
				                         .List();

				foreach (object[] values in groupList)
				{
					var cartoProcessTypeIdentification = (string) values[0];
					int cartoProcessCount = Convert.ToInt32(values[1]);

					result.Add(cartoProcessTypeIdentification, cartoProcessCount);
				}

				return result;
			}
		}

		#endregion
	}
}
