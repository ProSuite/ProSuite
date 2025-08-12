using System.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain;

public abstract class WorkListFactoryBase : IWorkListFactory
{
	protected IWorkList WorkList { get; set; }

	public abstract string Name { get; }

	public abstract IWorkList Get();

	public abstract Task<IWorkList> GetAsync();
}
