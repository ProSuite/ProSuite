using ArcGIS.Core.Data;
using EsriDE.ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.DataModel
{
	public class WorkspaceContext : WorkspaceContextBase
	{
		// todo daro: only pass in the connector as parameter to avoid "called
		// on wrong thread" exception?
		public WorkspaceContext(Geodatabase geodatabase) : base(geodatabase) { }

		public override Table OpenTable(IObjectDataset dataset)
		{
			// todo daro exception handling
			return Geodatabase?.OpenDataset<Table>(dataset.Name);
		}
	}
}
