using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// The row heights of lists and grids, in logical pixels (96 DPI, = WPF DIP).
	/// </summary>
	public static class ListRowHeight
	{
		/// <summary>
		/// Row height of the log windows.
		/// </summary>
		public const int Log = 20;

		/// <summary>
		/// Row height of all other lists and grids.
		/// </summary>
		public const int Default = 25;

		/// <summary>
		/// The single-line row height in device pixels, at least the font height plus a gap.
		/// </summary>
		public static int GetDeviceHeight(Control control, Font font, int logical = Default)
		{
			return Math.Max(control.LogicalToDeviceUnits(logical),
			                font.Height + control.LogicalToDeviceUnits(2));
		}

		public static int GetDeviceHeight(DataGridView grid, int logical)
		{
			return GetDeviceHeight(grid, grid.DefaultCellStyle.Font ?? grid.Font, logical);
		}

		public static void Apply(DataGridView grid, int logical)
		{
			int height = GetDeviceHeight(grid, logical);

			// A column sized to all cells measures the whole column after every row change,
			// which makes this quadratic (1500 rows: 2 minutes). Size them once at the end.
			var columnModes = new DataGridViewAutoSizeColumnMode[grid.Columns.Count];
			foreach (DataGridViewColumn column in grid.Columns)
			{
				columnModes[column.Index] = column.AutoSizeMode;
				column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
			}

			try
			{
				grid.RowTemplate.MinimumHeight = height;
				grid.RowTemplate.Height = height;

				bool autoSize = grid.AutoSizeRowsMode != DataGridViewAutoSizeRowsMode.None;

				foreach (DataGridViewRow row in grid.Rows)
				{
					if (row.MinimumHeight != height)
					{
						row.MinimumHeight = height;
					}

					if (! autoSize && row.Height != height)
					{
						row.Height = height;
					}
				}

				if (autoSize)
				{
					grid.AutoResizeRows(grid.AutoSizeRowsMode);
				}
			}
			finally
			{
				foreach (DataGridViewColumn column in grid.Columns)
				{
					column.AutoSizeMode = columnModes[column.Index];
				}
			}
		}
	}
}
