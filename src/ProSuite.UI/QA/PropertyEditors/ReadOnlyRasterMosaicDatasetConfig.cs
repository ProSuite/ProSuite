namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyRasterMosaicDatasetConfig : RasterMosaicDatasetConfig
	{
		protected override bool IsReadOnly
		{
			get { return true; }
		}
	}
}