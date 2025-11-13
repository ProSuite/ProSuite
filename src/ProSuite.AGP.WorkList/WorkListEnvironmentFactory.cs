using System;
using System.Threading.Tasks;

namespace ProSuite.AGP.WorkList;

public class WorkListEnvironmentFactory : IWorkListEnvironmentFactory
{
	private WorkListEnvironmentFactory() { }

	public static IWorkListEnvironmentFactory Instance { get; set; } =
		new WorkListEnvironmentFactory();

	public IWorkEnvironment CreateWorkEnvironment(string path, string typeName)
	{
		throw new NotImplementedException();
	}

	public Task<IWorkEnvironment> CreateWorkEnvironmentAsync(string path, string typeName)
	{
		throw new NotImplementedException();
	}
}
