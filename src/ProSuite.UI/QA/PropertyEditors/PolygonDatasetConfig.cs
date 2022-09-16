using System.ComponentModel;
using System.Drawing.Design;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class PolygonDatasetConfig : VectorDatasetConfig
	{
		protected override TestParameterType TestParameterTypes
		{
			get { return TestParameterType.PolygonDataset; }
		}

		[Editor(typeof(DatasetEditor), typeof(UITypeEditor))]
		[DisplayName("Polygon Feature Class")]
		public override Dataset Data
		{
			get { return base.Data; }
			set { base.Data = value; }
		}
	}
}