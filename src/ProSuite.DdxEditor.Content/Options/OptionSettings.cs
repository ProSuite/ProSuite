using System.ComponentModel;

namespace ProSuite.DdxEditor.Content.Options
{
	public class OptionSettings
	{
		public OptionSettings()
		{
			ListQualityConditionsWithDataset = true;
		}

		public bool ShowDeletedModelElements { get; set; }

		public bool ShowQualityConditionsBasedOnDeletedDatasets { get; set; }

		[DefaultValue(true)]
		public bool ListQualityConditionsWithDataset { get; set; }
	}
}
