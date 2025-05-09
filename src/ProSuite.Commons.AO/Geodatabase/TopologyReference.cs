using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using IDatasetContainer = ProSuite.Commons.GeoDb.IDatasetContainer;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class TopologyReference : ITopologyDef
	{
		public TopologyReference([CanBeNull] ITopology topology)
		{
			Topology = topology;
		}

		private IDataset Dataset => (IDataset) Topology;

		public ITopology Topology { get; }

		#region Implementation of IDatasetDef

		public string Name => Dataset.Name;

		public IDatasetContainer DbContainer
		{
			get
			{
				IWorkspace workspace = Dataset.Workspace;
				return new GeoDbWorkspace(workspace);
			}
		}

		public DatasetType DatasetType => DatasetType.Topology;

		public bool Equals(IDatasetDef otherDataset)
		{
			if (otherDataset is TopologyReference other)
			{
				return Topology == other.Topology;
			}

			return false;
		}

		#endregion

		#region Overrides of Object

		public override bool Equals(object obj)
		{
			if (obj is IDatasetDef otherDatasetDef)
			{
				return Equals(otherDatasetDef);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return Topology.GetHashCode();
		}

		#endregion
	}
}
