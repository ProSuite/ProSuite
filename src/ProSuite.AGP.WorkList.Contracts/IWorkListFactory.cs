using System.Threading.Tasks;

namespace ProSuite.AGP.WorkList.Contracts;

public interface IWorkListFactory
{
	string Name { get; }

	IWorkList Get();

	Task<IWorkList> GetAsync();

	void UnWire();
}
