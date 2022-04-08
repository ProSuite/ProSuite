using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	[UsedImplicitly]
	public class CreateQualityConditionsFormState : FormState
	{
		[DefaultValue(0)]
		public int ParametersPanelHeight { get; set; }
	}
}