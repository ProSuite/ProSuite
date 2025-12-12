using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.UI.Core.QA.Customize
{
	[UsedImplicitly]
	public class CustomizeQASpecFormState : FormState
	{
		[DefaultValue(-1)]
		public int ListHeight { get; set; } = -1;

		[DefaultValue(-1)]
		public int ListWidth { get; set; } = -1;

		[DefaultValue(DisplayMode.QualityConditionList)]
		public DisplayMode ActiveMode { get; set; } = DisplayMode.QualityConditionList;

		public bool MatchCase { get; set; }

		public bool FilterRows { get; set; }

		[CanBeNull]
		public DataGridViewSortState ConditionsSortState { get; set; }

		[CanBeNull]
		public DataGridViewSortState DatasetsSortState { get; set; }
	}
}
