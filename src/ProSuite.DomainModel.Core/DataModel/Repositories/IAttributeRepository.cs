using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface IAttributeRepository : IRepository<Attribute>
	{
		IList<T> GetAll<T>() where T : Attribute;

		IList<ObjectAttribute> Get(AttributeType type);
	}
}
