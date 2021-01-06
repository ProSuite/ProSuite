using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface IAssociationEndRepository : IRepository<AssociationEnd>
	{
		IList<S> GetAll<S>() where S : AssociationEnd;
	}
}
