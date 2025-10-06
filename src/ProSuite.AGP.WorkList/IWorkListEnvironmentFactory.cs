using System.Threading.Tasks;

namespace ProSuite.AGP.WorkList;

public interface IWorkListEnvironmentFactory
{
	// todo: (daro): make generic instead of strings?
	IWorkEnvironment CreateWorkEnvironment(string path, string typeName);

	Task<IWorkEnvironment> CreateWorkEnvironmentAsync(string path, string typeName);
}
