using System;
using System.ComponentModel;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class RasterMosaicDatasetProperty :
		DatasetProperty<RasterMosaicDatasetConfig>
	{
		public RasterMosaicDatasetProperty() : this(new RasterMosaicDatasetConfig()) { }

		public RasterMosaicDatasetProperty(
			[NotNull] RasterMosaicDatasetConfig parameterConfig) : base(parameterConfig) { }

		protected override Type GetParameterType()
		{
			return typeof(SimpleRasterMosaic);
		}

		[Description("Mosaic Dataset")]
		[DisplayName("Mosaic Dataset")]
		[TypeConverter(typeof(DatasetConverter))]
		[UsedImplicitly]
		public RasterMosaicDatasetConfig MosaicDataset
		{
			get { return GetParameterConfig(); }
			set { SetParameterConfig(value); }
		}
	}
}
