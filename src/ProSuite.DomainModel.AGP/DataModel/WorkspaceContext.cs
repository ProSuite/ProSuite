using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.DomainModel.AGP.DataModel
{
	public class WorkspaceContext : WorkspaceContextBase
	{
		// todo daro: only pass in the connector as parameter to avoid "called
		// on wrong thread" exception?
		public WorkspaceContext(GdbWorkspaceIdentity workspace) : base(workspace) { }
	}
}
