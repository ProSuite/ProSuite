using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Finder
{
	[UsedImplicitly]
	public class FinderFormState : FormState
	{
		public FinderFormState()
		{
			FilterRows = true;
		}

		public DataGridViewSortState DataGridViewSortState { get; set; }

		[DefaultValue(true)]
		public bool FilterRows { get; set; }

		[DefaultValue(null)]
		public string FindText { get; set; }

		[DefaultValue(false)]
		public bool MatchCase { get; set; }

		[DefaultValue(0)]
		public int FirstDisplayedScrollingRowIndex { get; set; }

		[DefaultValue(0)]
		public int FirstDisplayedScrollingColumnIndex { get; set; }
	}
}
