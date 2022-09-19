using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Framework
{
	[UsedImplicitly]
	public class DeleteItemsFormState : FormState
	{
		[DefaultValue(0)]
		public int SplitterDistance { get; set; }
	}
}
