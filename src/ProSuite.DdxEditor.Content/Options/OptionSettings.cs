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

		/// <summary>
		/// If <c>true</c>, the classic condition specification flow (Finder + constructor
		/// combo + Blazor parameter grid) is used even if the algorithm-first UI is
		/// available. Default <c>false</c> (algorithm-first UI is used, if available).
		/// Takes effect without restarting: see <see cref="OptionsManager"/>, which
		/// reloads the "Algorithm Descriptors" tree section and the currently open
		/// condition/transformer/filter item (if any) after this setting changes.
		/// </summary>
		public bool UseClassicConditionSpecification { get; set; }
	}
}
