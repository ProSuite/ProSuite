using System.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain;

public class WorkListFactory : WorkListFactoryBase
{
	public WorkListFactory(IWorkList workList)
	{
		WorkList = workList;
	}

	public override string Name => WorkList.Name;

	public override IWorkList Get()
	{
		return WorkList;
	}

	public override Task<IWorkList> GetAsync()
	{
		return Task.FromResult(WorkList);
	}
}
