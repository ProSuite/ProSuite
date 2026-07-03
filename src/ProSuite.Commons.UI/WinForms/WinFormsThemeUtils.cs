using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms
{
	/// <summary>
	/// Applies ArcGIS Pro's dark theme to WinForms windows.
	/// </summary>
	/// <remarks>
	/// ArcGIS Pro is a WPF application: switching its theme (Options > General > Theme)
	/// has no effect on hosted WinForms windows — WinForms always renders with the default
	/// (light) <see cref="SystemColors"/>. This utility re-colors a WinForms control tree
	/// to approximate the Pro dark theme. Colors match
	/// https://esri.github.io/arcgis-pro-sdk/content/brushescolors/brushes.html
	/// (dark theme values of Esri_DialogFrameBackgroundBrush, Esri_ControlBackgroundBrush,
	/// Esri_BorderBrush, Esri_TextControlBrush, Esri_BackgroundSelectedBrush).
	/// </remarks>
	public static class WinFormsThemeUtils
	{
		private static readonly Color _dialogBackground = ColorTranslator.FromHtml("#333333");
		private static readonly Color _controlBackground = ColorTranslator.FromHtml("#242424");
		private static readonly Color _borderColor = ColorTranslator.FromHtml("#4A4A4A");
		private static readonly Color _textColor = ColorTranslator.FromHtml("#D1D1D1");
		private static readonly Color _selectionBackground = ColorTranslator.FromHtml("#1B394C");

		/// <summary>
		/// Re-colors <paramref name="form"/> (and all its child controls, menus and tool strips)
		/// to match ArcGIS Pro's dark theme. Call this only when the dark theme is active.
		/// </summary>
		public static void ApplyDarkTheme([NotNull] Form form)
		{
			ApplyDarkTheme((Control) form);
		}

		/// <summary>
		/// Re-colors <paramref name="control"/> (and all its child controls, menus and tool
		/// strips) to match ArcGIS Pro's dark theme. Use this for a <see cref="UserControl"/>
		/// that is hosted (e.g. via a WindowsFormsHost) rather than shown as a top-level form.
		/// Call this only when the dark theme is active.
		/// </summary>
		public static void ApplyDarkTheme([NotNull] UserControl control)
		{
			ApplyDarkTheme((Control) control);
		}

		#region Non-public members

		private static void ApplyDarkTheme([NotNull] Control control)
		{
			if (control is DataGridView dataGridView)
			{
				ApplyDarkTheme(dataGridView);
				return;
			}

			if (control is ToolStrip toolStrip)
			{
				ApplyDarkTheme(toolStrip);
			}
			else if (control is TextBoxBase || control is ComboBox || control is ListControl)
			{
				control.BackColor = _controlBackground;
				control.ForeColor = _textColor;
			}
			else if (control is Button button)
			{
				button.BackColor = _controlBackground;
				button.ForeColor = _textColor;
				button.UseVisualStyleBackColor = false;
				button.FlatStyle = FlatStyle.Flat;
				button.FlatAppearance.BorderColor = _borderColor;
			}
			else if (control is TreeView treeView)
			{
				treeView.BackColor = _controlBackground;
				treeView.ForeColor = _textColor;
			}
			else if (control is Form || control is GroupBox || control is Panel ||
			         control is SplitContainer || control is TabControl || control is TabPage ||
			         control is Label || control is CheckBox || control is RadioButton ||
			         control is UserControl)
			{
				control.BackColor = _dialogBackground;
				control.ForeColor = _textColor;
			}

			if (control.ContextMenuStrip != null)
			{
				ApplyDarkTheme(control.ContextMenuStrip);
			}

			foreach (Control child in control.Controls)
			{
				ApplyDarkTheme(child);
			}
		}

		private static void ApplyDarkTheme([NotNull] DataGridView grid)
		{
			grid.BackgroundColor = _controlBackground;
			grid.GridColor = _borderColor;
			grid.EnableHeadersVisualStyles = false;

			grid.DefaultCellStyle.BackColor = _controlBackground;
			grid.DefaultCellStyle.ForeColor = _textColor;
			grid.DefaultCellStyle.SelectionBackColor = _selectionBackground;
			grid.DefaultCellStyle.SelectionForeColor = _textColor;

			grid.AlternatingRowsDefaultCellStyle.BackColor = _controlBackground;
			grid.AlternatingRowsDefaultCellStyle.ForeColor = _textColor;
			grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = _selectionBackground;
			grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = _textColor;

			grid.ColumnHeadersDefaultCellStyle.BackColor = _dialogBackground;
			grid.ColumnHeadersDefaultCellStyle.ForeColor = _textColor;
			grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _dialogBackground;
			grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _textColor;

			grid.RowHeadersDefaultCellStyle.BackColor = _dialogBackground;
			grid.RowHeadersDefaultCellStyle.ForeColor = _textColor;
			grid.RowHeadersDefaultCellStyle.SelectionBackColor = _dialogBackground;
			grid.RowHeadersDefaultCellStyle.SelectionForeColor = _textColor;

			if (grid.ContextMenuStrip != null)
			{
				ApplyDarkTheme(grid.ContextMenuStrip);
			}
		}

		private static void ApplyDarkTheme([NotNull] ToolStrip toolStrip)
		{
			toolStrip.BackColor = _dialogBackground;
			toolStrip.ForeColor = _textColor;
			toolStrip.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());

			foreach (ToolStripItem item in toolStrip.Items)
			{
				ApplyDarkTheme(item);
			}
		}

		private static void ApplyDarkTheme([NotNull] ToolStripItem item)
		{
			item.BackColor = _dialogBackground;
			item.ForeColor = _textColor;

			if (item is ToolStripDropDownItem dropDownItem)
			{
				ApplyDarkTheme(dropDownItem.DropDown);
			}
		}

		private class DarkColorTable : ProfessionalColorTable
		{
			public override Color ToolStripDropDownBackground => _controlBackground;
			public override Color ImageMarginGradientBegin => _dialogBackground;
			public override Color ImageMarginGradientMiddle => _dialogBackground;
			public override Color ImageMarginGradientEnd => _dialogBackground;

			public override Color MenuBorder => _borderColor;
			public override Color MenuItemBorder => _borderColor;
			public override Color MenuItemSelected => _selectionBackground;
			public override Color MenuItemSelectedGradientBegin => _selectionBackground;
			public override Color MenuItemSelectedGradientEnd => _selectionBackground;
			public override Color MenuItemPressedGradientBegin => _selectionBackground;
			public override Color MenuItemPressedGradientEnd => _selectionBackground;
			public override Color MenuStripGradientBegin => _dialogBackground;
			public override Color MenuStripGradientEnd => _dialogBackground;

			public override Color SeparatorDark => _borderColor;
			public override Color SeparatorLight => _dialogBackground;

			public override Color ToolStripBorder => _borderColor;
			public override Color ToolStripGradientBegin => _dialogBackground;
			public override Color ToolStripGradientMiddle => _dialogBackground;
			public override Color ToolStripGradientEnd => _dialogBackground;

			public override Color StatusStripGradientBegin => _dialogBackground;
			public override Color StatusStripGradientEnd => _dialogBackground;

			public override Color ButtonSelectedHighlight => _selectionBackground;
			public override Color ButtonSelectedHighlightBorder => _borderColor;
			public override Color ButtonPressedHighlight => _selectionBackground;
			public override Color ButtonPressedHighlightBorder => _borderColor;
			public override Color ButtonCheckedHighlight => _selectionBackground;
			public override Color ButtonCheckedHighlightBorder => _borderColor;

			public override Color OverflowButtonGradientBegin => _dialogBackground;
			public override Color OverflowButtonGradientMiddle => _dialogBackground;
			public override Color OverflowButtonGradientEnd => _dialogBackground;

			public override Color GripDark => _borderColor;
			public override Color GripLight => _dialogBackground;
		}

		#endregion
	}
}
