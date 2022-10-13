using System.ComponentModel;
using System.Data;
using System.Drawing.Design;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class RasterMosaicDatasetConfig : DatasetConfig
	{
		protected override TestParameterType TestParameterTypes
		{
			get { return TestParameterType.RasterMosaicDataset; }
		}

		[Editor(typeof(DatasetEditor), typeof(UITypeEditor))]
		[DisplayName("Mosaic Dataset")]
		public override Dataset Data
		{
			get { return base.Data; }
			set
			{
				if (IsReadOnly)
				{
					throw new ReadOnlyException();
				}

				base.Data = value;
			}
		}

		protected virtual bool IsReadOnly
		{
			get { return false; }
		}
	}
}
