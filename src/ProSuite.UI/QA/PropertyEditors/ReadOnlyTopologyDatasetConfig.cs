using System.ComponentModel;
using System.Drawing.Design;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyTopologyDatasetConfig : ReadOnlyDatasetConfig
	{
		protected override TestParameterType TestParameterTypes
		{
			get { return TestParameterType.TopologyDataset; }
		}

		[Editor(typeof(DatasetEditor), typeof(UITypeEditor))]
		[DisplayName("Topology Dataset")]
		[ReadOnly(true)]
		public override Dataset Data
		{
			get { return base.Data; }
			set { base.Data = value; }
		}
	}
}
