using System;
using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class RasterDatasetProperty : DatasetProperty<RasterDatasetConfig>
	{
		public RasterDatasetProperty() : this(new RasterDatasetConfig()) { }

		public RasterDatasetProperty([NotNull] RasterDatasetConfig parameterConfig)
			: base(parameterConfig) { }

		protected override Type GetParameterType()
		{
			return typeof(IRasterDataset);
		}

		[Description("Raster Dataset")]
		[DisplayName("Raster Dataset")]
		[TypeConverter(typeof(DatasetConverter))]
		[UsedImplicitly]
		public RasterDatasetConfig RasterDataset
		{
			get { return GetParameterConfig(); }
			set { SetParameterConfig(value); }
		}
	}
}
