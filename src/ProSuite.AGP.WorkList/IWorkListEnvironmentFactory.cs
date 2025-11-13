using System.Threading.Tasks;

namespace ProSuite.AGP.WorkList;

public interface IWorkListEnvironmentFactory
{
	// todo: (daro): make generic instead of strings?
	IWorkEnvironment CreateWorkEnvironment(string path, string typeName);

	// TODO: No implementation uses async functionality so far. Consider removing.
	Task<IWorkEnvironment> CreateWorkEnvironmentAsync(string path, string typeName);
}
