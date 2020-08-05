using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.DomainModel.DataModel
{
	public class WorkspaceContext : WorkspaceContextBase
	{
		// todo daro: only pass in the connector as parameter to avoid "called
		// on wrong thread" exception?
		public WorkspaceContext(GdbWorkspaceReference workspace) : base(workspace) { }
	}
}
