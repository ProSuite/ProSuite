using System.ComponentModel;
using System.Drawing.Design;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class PointDatasetConfig : VectorDatasetConfig
	{
		protected override TestParameterType TestParameterTypes
		{
			get { return TestParameterType.PointDataset; }
		}

		[Editor(typeof(DatasetEditor), typeof(UITypeEditor))]
		[DisplayName("Point Feature Class")]
		public override Dataset Data
		{
			get { return base.Data; }
			set { base.Data = value; }
		}
	}
}
