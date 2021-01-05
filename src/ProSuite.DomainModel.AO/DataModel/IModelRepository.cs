using System;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public interface IModelRepository : IRepository<Model>
	{
		Model Get(string name);
	}
}
