using System;
using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class TableDatasetProperty : DatasetProperty<TableDatasetConfig>
	{
		public TableDatasetProperty() : base(new TableDatasetConfig()) { }

		public TableDatasetProperty(TableDatasetConfig dataset) : base(dataset) { }

		[Description("Table")]
		[TypeConverter(typeof(DatasetConverter))]
		public TableDatasetConfig Table
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
