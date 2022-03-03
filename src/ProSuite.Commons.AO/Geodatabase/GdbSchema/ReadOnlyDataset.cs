using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class ReadOnlyDataset : IReadOnlyDataset
	{
		private readonly IDataset _dataset;

		public ReadOnlyDataset(IDataset dataset)
		{
			_dataset = dataset;
		}

		public string Name => _dataset.Name;

		public IName FullName => _dataset.FullName;

		public IWorkspace Workspace => _dataset.Workspace;
	}
}
