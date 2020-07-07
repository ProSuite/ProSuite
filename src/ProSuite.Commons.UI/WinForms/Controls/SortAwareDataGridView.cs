using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class SortAwareDataGridView : DataGridView
	{
		public event EventHandler<SortingColumnEventArgs> SortingColumn;

		public override void Sort(DataGridViewColumn dataGridViewColumn,
		                          ListSortDirection direction)
		{
			SortingColumn?.Invoke(this,
			                      new SortingColumnEventArgs(dataGridViewColumn,
			                                                 direction));

			base.Sort(dataGridViewColumn, direction);
		}
	}
}
