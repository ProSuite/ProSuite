using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	[UsedImplicitly]
	public class ImportQualitySpecificationsFormState : FormState
	{
		public ImportQualitySpecificationsFormState()
		{
			UpdateTestDescriptorNames = false;
			UpdateTestDescriptorProperties = false;
			IgnoreQualityConditionsForUnknownDatasets = true;
		}

		[DefaultValue(false)]
		public bool UpdateTestDescriptorNames { get; set; }

		[DefaultValue(false)]
		public bool UpdateTestDescriptorProperties { get; set; }

		[DefaultValue(true)]
		public bool IgnoreQualityConditionsForUnknownDatasets { get; set; }
	}
}
