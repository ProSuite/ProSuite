namespace ProSuite.AGP.WorkList;

public interface IWorkListEnvironmentFactory
{
	// todo: (daro): make generic instead of strings?
	IWorkEnvironment CreateWorkEnvironment(string path, string typeName);
}
