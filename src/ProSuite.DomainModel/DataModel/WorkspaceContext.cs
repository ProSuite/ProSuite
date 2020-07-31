using ArcGIS.Core.Data;

namespace ProSuite.DomainModel.DataModel
{
	public class WorkspaceContext : WorkspaceContextBase
	{
		// todo daro: only pass in the connector as parameter to avoid "called
		// on wrong thread" exception?
		public WorkspaceContext(Geodatabase geodatabase) : base(geodatabase) { }

		public override Table OpenTable(string name)
		{
			// todo daro exception handling
			return Geodatabase?.OpenDataset<Table>(name);
		}
	}
}
