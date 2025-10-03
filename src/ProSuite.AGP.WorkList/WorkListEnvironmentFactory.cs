using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList;

public class WorkListEnvironmentFactory : IWorkListEnvironmentFactory
{
	private WorkListEnvironmentFactory() { }

	public static IWorkListEnvironmentFactory Instance { get; set; } =
		new WorkListEnvironmentFactory();

	[CanBeNull]
	public IWorkEnvironment CreateWorkEnvironment(string path, string typeName)
	{
		throw new NotImplementedException();
	}
}
