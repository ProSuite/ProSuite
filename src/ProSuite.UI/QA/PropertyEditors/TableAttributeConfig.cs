namespace ProSuite.UI.QA.PropertyEditors
{
	public class TableAttributeConfig : ParameterConfig
	{
		private TableDatasetConfig _tableDataset;
		private string _attribute;

		public TableAttributeConfig() { }

		public TableAttributeConfig(TableDatasetConfig tableDataset, string attribute)
		{
			_tableDataset = tableDataset;
			_attribute = attribute;
		}

		public TableDatasetConfig GetTableDataset()
		{
			return _tableDataset;
		}

		public void SetTableDataset(TableDatasetConfig dataset)
		{
			_tableDataset = dataset;
		}

		public string Attribute
		{
			get { return _attribute; }
			set { _attribute = value; }
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(_attribute))
			{
				return "{null}";
			}

			return _attribute;
		}
	}
}