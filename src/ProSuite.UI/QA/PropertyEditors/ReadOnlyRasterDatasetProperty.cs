using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyRasterDatasetProperty : RasterDatasetProperty
	{
		public ReadOnlyRasterDatasetProperty() : this(new ReadOnlyRasterDatasetConfig()) { }

		public ReadOnlyRasterDatasetProperty(
			[NotNull] ReadOnlyRasterDatasetConfig parameterConfig) : base(parameterConfig) { }
	}
}
