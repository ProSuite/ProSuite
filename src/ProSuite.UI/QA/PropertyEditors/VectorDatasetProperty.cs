using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class VectorDatasetProperty : VectorDatasetPropertyBase<VectorDatasetConfig>
	{
		public VectorDatasetProperty() : base(new VectorDatasetConfig()) { }

		public VectorDatasetProperty([NotNull] VectorDatasetConfig dataset)
			: base(dataset) { }

		[Description("Feature Class")]
		[DisplayName("Feature Class")]
		[TypeConverter(typeof(DatasetConverter))]
		[UsedImplicitly]
		public VectorDatasetConfig FeatureClass
		{
			get { return GetParameterConfig(); }
			set { SetParameterConfig(value); }
		}
	}
}