namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyRasterDatasetConfig : RasterDatasetConfig
	{
		protected override bool IsReadOnly
		{
			get { return true; }
		}
	}
}