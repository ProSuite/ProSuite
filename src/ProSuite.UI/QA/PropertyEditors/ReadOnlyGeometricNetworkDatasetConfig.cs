using System.ComponentModel;
using System.Data;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyGeometricNetworkDatasetConfig : ReadOnlyDatasetConfig
	{
		protected override TestParameterType TestParameterTypes
		{
			get { return TestParameterType.GeometricNetworkDataset; }
		}

		[DisplayName("Geometric Network")]
		[ReadOnly(true)]
		public override Dataset Data
		{
			get { return base.Data; }
			set { throw new ReadOnlyException(); }
		}
	}
}
