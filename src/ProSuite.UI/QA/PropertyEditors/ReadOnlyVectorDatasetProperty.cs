using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyVectorDatasetProperty :
		VectorDatasetPropertyBase<ReadOnlyVectorDatasetConfig>
	{
		public ReadOnlyVectorDatasetProperty()
			: base(new ReadOnlyVectorDatasetConfig()) { }

		public ReadOnlyVectorDatasetProperty(ReadOnlyVectorDatasetConfig dataset)
			: base(dataset) { }

		[Description("Feature Class")]
		[DisplayName("Feature Class")]
		[TypeConverter(typeof(DatasetConverter))]
		[UsedImplicitly]
		public ReadOnlyVectorDatasetConfig FeatureClass
		{
			get { return GetParameterConfig(); }
			set { SetParameterConfig(value); }
		}
	}
}
