using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyRasterMosaicDatasetProperty : RasterMosaicDatasetProperty
	{
		public ReadOnlyRasterMosaicDatasetProperty() :
			this(new ReadOnlyRasterMosaicDatasetConfig()) { }

		public ReadOnlyRasterMosaicDatasetProperty(
			[NotNull] ReadOnlyRasterMosaicDatasetConfig parameterConfig) : base(parameterConfig) { }
	}
}
