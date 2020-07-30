using System.Collections.Generic;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkListRegistry
	{
		IWorkList Get(string name);

		void Add(IWorkList workList);

		bool Remove(IWorkList workList);

		bool Remove(string name);

		IList<IWorkList> GetAll();
	}
}
