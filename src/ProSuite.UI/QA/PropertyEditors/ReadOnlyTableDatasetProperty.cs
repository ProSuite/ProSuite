using System;
using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyTableDatasetProperty :
		DatasetProperty<ReadOnlyTableDatasetConfig>
	{
		public ReadOnlyTableDatasetProperty() : base(new ReadOnlyTableDatasetConfig()) { }

		public ReadOnlyTableDatasetProperty(ReadOnlyTableDatasetConfig dataset)
			: base(dataset) { }

		[Description("Table")]
		[TypeConverter(typeof(DatasetConverter))]
		[ReadOnly(true)]
		public ReadOnlyTableDatasetConfig Table
		{
			get { return GetParameterConfig(); }
			set { SetParameterConfig(value); }
		}

		protected override Type GetParameterType()
		{
			return typeof(ITable);
		}
	}
}