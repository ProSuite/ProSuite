using System;
using System.ComponentModel;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class SortingColumnEventArgs : EventArgs
	{
		public SortingColumnEventArgs([NotNull] DataGridViewColumn column,
		                              ListSortDirection direction)
		{
			Assert.ArgumentNotNull(column, nameof(column));

			Column = column;
			Direction = direction;
		}

		[NotNull]
		public DataGridViewColumn Column { get; }

		public ListSortDirection Direction { get; }
	}
}
