using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class SortAwareDataGridView : DataGridView
	{
		/// <summary>
		/// The single-line row height in logical pixels, see <see cref="ListRowHeight" />.
		/// </summary>
		[DefaultValue(ListRowHeight.Default)]
		public int LogicalRowHeight
		{
			get => _logicalRowHeight;
			set
			{
				_logicalRowHeight = value;

				// The handle can exist already, e.g. created in the owner's InitializeComponent.
				if (IsHandleCreated)
				{
					ListRowHeight.Apply(this, value);
				}
			}
		}

		private int _logicalRowHeight = ListRowHeight.Default;

		public event EventHandler<SortingColumnEventArgs> SortingColumn;

		public override void Sort(DataGridViewColumn dataGridViewColumn,
		                          ListSortDirection direction)
		{
			SortingColumn?.Invoke(this,
			                      new SortingColumnEventArgs(dataGridViewColumn,
			                                                 direction));

			base.Sort(dataGridViewColumn, direction);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			foreach (DataGridViewColumn column in Columns)
			{
				if (column is DataGridViewImageColumn imageColumn &&
				    imageColumn.ImageLayout == DataGridViewImageCellLayout.Zoom)
				{
					// Also on the column style, in case auto-sizing measures without the
					// formatting event.
					imageColumn.DefaultCellStyle.Padding = GetImagePadding(imageColumn);
				}
			}

			ListRowHeight.Apply(this, LogicalRowHeight);
		}

		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			base.OnCellFormatting(e);

			if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
			    ! (Columns[e.ColumnIndex] is DataGridViewImageColumn imageColumn) ||
			    imageColumn.ImageLayout != DataGridViewImageCellLayout.Zoom)
			{
				return;
			}

			e.CellStyle.Padding = GetImagePadding(imageColumn);
		}

		/// <summary>
		/// Keeps zoomed images at 16 logical px. Zoom keeps the aspect ratio and centers the
		/// image, so side padding alone sets its size. Vertical padding would count as content
		/// and keep auto-sized rows from getting lower than image plus padding.
		/// </summary>
		private Padding GetImagePadding(DataGridViewColumn imageColumn)
		{
			int horizontal = Math.Max(0, (imageColumn.Width - LogicalToDeviceUnits(16)) / 2);

			return new Padding(horizontal, 0, horizontal, 0);
		}
	}
}
