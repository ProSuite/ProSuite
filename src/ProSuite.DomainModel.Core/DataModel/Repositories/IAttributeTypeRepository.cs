using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface IAttributeTypeRepository : IRepository<AttributeType>
	{
		IList<S> GetAll<S>() where S : AttributeType;
	}
}
