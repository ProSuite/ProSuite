using System.ComponentModel;
using System.Drawing.Design;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class GeometricNetworkDatasetConfig : DatasetConfig
	{
		protected override TestParameterType TestParameterTypes
		{
			get { return TestParameterType.GeometricNetworkDataset; }
		}

		[Editor(typeof(DatasetEditor), typeof(UITypeEditor))]
		[DisplayName("Geometric Network")]
		public override Dataset Data
		{
			get { return base.Data; }
			set { base.Data = value; }
		}
	}
}