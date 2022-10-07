using System.ComponentModel;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class PointDatasetProperty : VectorDatasetPropertyBase<PointDatasetConfig>
	{
		public PointDatasetProperty()
			: base(new PointDatasetConfig()) { }

		public PointDatasetProperty(PointDatasetConfig dataset)
			: base(dataset) { }

		[Description("Point Feature Class")]
		[DisplayName("Point Feature Class")]
		[TypeConverter(typeof(DatasetConverter))]
		public PointDatasetConfig FeatureClass
		{
			get { return GetParameterConfig(); }
			set { SetParameterConfig(value); }
		}
	}
}
