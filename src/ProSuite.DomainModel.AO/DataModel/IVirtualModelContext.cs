using System.Collections.Generic;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IVirtualModelContext
	{
		void InitializeSchema(ICollection<Dataset> datasets);
	}
}
