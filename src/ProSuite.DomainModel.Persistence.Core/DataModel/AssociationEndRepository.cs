using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class AssociationEndRepository : NHibernateRepository<AssociationEnd>,
	                                        IAssociationEndRepository
	{
		#region IAssociationEndRepository Members

		public IList<T> GetAll<T>() where T : AssociationEnd
		{
			return GetAllCore<T>();
		}

		#endregion
	}
}
