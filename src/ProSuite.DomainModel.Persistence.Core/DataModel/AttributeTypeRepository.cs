using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class AttributeTypeRepository : NHibernateRepository<AttributeType>,
	                                       IAttributeTypeRepository
	{
		#region IAttributeTypeRepository Members

		public IList<S> GetAll<S>() where S : AttributeType
		{
			return GetAllCore<S>();
		}

		#endregion
	}
}
