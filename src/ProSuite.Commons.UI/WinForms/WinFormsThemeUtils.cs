using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

		// Matches the Attribute Table's column header look (light theme).
		private static readonly Color _headerBackgroundLight = ColorTranslator.FromHtml("#F3F3F3");
		private static readonly Color _headerBorderLight = ColorTranslator.FromHtml("#D6D6D6");

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

		/// <summary>
		/// Styles <paramref name="grid"/>'s column headers to match ArcGIS Pro's Attribute
		/// Table: a light gray background, bold header text, and thin light-gray separator
		/// lines. WinForms' own header rendering falls back to a "raised" 3-D bevel (drawn with
		/// stark <see cref="SystemColors.ControlDark"/> edges) whenever the control isn't running
		/// under native visual styles, as is the case here since the grid is hosted inside a WPF
		/// dock pane. Call this once regardless of theme; if the dark theme is active,
		/// <see cref="ApplyDarkTheme(Form)"/>/<see cref="ApplyDarkTheme(UserControl)"/> darkens
		/// the header colors afterwards and the border color follows automatically.
		/// </summary>
		public static void ApplyModernHeaderStyle([NotNull] DataGridView grid)
		{
			grid.EnableHeadersVisualStyles = false;
			grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

			// In WinForms' own dark mode the system colors are already dark.
			bool dark = IsSystemDarkModeEnabled();
			Color headerBackground = dark ? SystemColors.Control : _headerBackgroundLight;

			// Match the body cell gridlines to the header's border color (SystemColors.Control,
			// the designer default, is too faint to read as a boundary).
			grid.GridColor = dark ? _borderColor : _headerBorderLight;

			grid.ColumnHeadersDefaultCellStyle.BackColor = headerBackground;
			grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerBackground;
			grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
			grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

			// Extra vertical padding gives the header some breathing room; since the height size
			// mode is AutoSize (set in the designer), the grid grows the header row to fit it.
			DataGridViewCellStyle headerStyle = grid.ColumnHeadersDefaultCellStyle;
			headerStyle.Padding = new Padding(headerStyle.Padding.Left, 3,
			                                  headerStyle.Padding.Right, 3);

			grid.CellPainting -= PaintHeaderBorder;
			grid.CellPainting += PaintHeaderBorder;
		}

		/// <summary>
		/// Fixes the controls that stay light when a standalone app uses WinForms' own dark mode
		/// (<c>Application.SetColorMode</c>): theme-drawn grid headers, tab pages and the white
		/// borders of grids, 3-D text boxes and group boxes. Does nothing when dark mode is off.
		/// Call it after
		/// <c>InitializeComponent</c>.
		/// </summary>
		public static void ApplySystemDarkModeFixes([NotNull] Control control)
		{
			if (! IsSystemDarkModeEnabled())
			{
				return;
			}

			if (control is DataGridView grid)
			{
				grid.EnableHeadersVisualStyles = false;

				// Dark mode draws the grid's border white; the darker background sets it off.
				grid.BorderStyle = BorderStyle.None;
			}
			else if (control is TextBoxBase textBox && textBox.BorderStyle == BorderStyle.Fixed3D)
			{
				// Dark mode draws a white Fixed3D border, but a grey FixedSingle one.
				textBox.BorderStyle = BorderStyle.FixedSingle;
			}
			else if (control is ListBox listBox && listBox.BorderStyle == BorderStyle.Fixed3D)
			{
				listBox.BorderStyle = BorderStyle.FixedSingle;
			}
			else if (control is ListView listView && listView.BorderStyle == BorderStyle.Fixed3D)
			{
				listView.BorderStyle = BorderStyle.FixedSingle;
			}
			else if (control is Panel panel && panel.BorderStyle == BorderStyle.Fixed3D)
			{
				panel.BorderStyle = BorderStyle.FixedSingle;
			}
			else if (control is TabPage tabPage)
			{
				tabPage.UseVisualStyleBackColor = false;
			}
			else if (control is GroupBox groupBox)
			{
				// Dark mode (experimental up to .NET 10) draws the group box frame white, whatever
				// the FlatStyle or ForeColor. A future .NET version might fix this; then drop it.
				groupBox.Paint -= PaintGroupBox;
				groupBox.Paint += PaintGroupBox;
			}

			foreach (Control child in control.Controls)
			{
				ApplySystemDarkModeFixes(child);
			}
		}

		#region Non-public members

		internal static bool IsSystemDarkModeEnabled()
		{
#if NET9_0_OR_GREATER
#pragma warning disable WFO5001
			return Application.IsDarkModeEnabled;
#pragma warning restore WFO5001
#else
			return false;
#endif
		}

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

				if (control is ComboBox comboBox &&
				    comboBox.DropDownStyle == ComboBoxStyle.DropDownList)
				{
					// The visual style draws a drop-down list light, whatever its BackColor.
					comboBox.FlatStyle = FlatStyle.Flat;
				}

				if (control is TextBoxBase)
				{
					// Multiline text boxes (e.g. the details view) have their own scrollbars.
					EnableDarkScrollBars(control);
				}
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

				if (control is TabPage tabPage)
				{
					// Otherwise the visual style paints the page light over the BackColor.
					tabPage.UseVisualStyleBackColor = false;
				}
				else if (control is TabControl tabControl)
				{
					// Native tab headers, the strip behind them and the page frame ignore
					// BackColor, and owner-drawn tabs only cover the tabs themselves.
					_tabPainters.GetValue(tabControl, t => new DarkTabControlPainter(t));
				}
				else if (control is Form form)
				{
					EnableDarkTitleBar(form);
				}
				else if (control is GroupBox groupBox)
				{
					// The native frame is drawn white.
					groupBox.Paint -= PaintGroupBox;
					groupBox.Paint += PaintGroupBox;
				}
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

			EnableDarkScrollBars(grid);

			if (grid.ContextMenuStrip != null)
			{
				ApplyDarkTheme(grid.ContextMenuStrip);
			}
		}

		/// <summary>
		/// Re-colors <paramref name="toolStrip"/> and its items to match ArcGIS Pro's dark theme.
		/// Use it for context menus that are not reachable from a control tree, e.g. those
		/// handed out in <see cref="DataGridView.CellContextMenuStripNeeded"/>.
		/// Call this only when the dark theme is active.
		/// </summary>
		public static void ApplyDarkTheme([NotNull] ToolStrip toolStrip)
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

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName,
		                                         string pszSubIdList);

		private const string _darkExplorerTheme = "DarkMode_Explorer";

		/// <summary>
		/// Switches a control's native scrollbars to the dark Explorer theme so they are not drawn
		/// light-grey in the otherwise dark UI. Handles the control's own scrollbars and any child
		/// <see cref="ScrollBar"/> controls (e.g. a <see cref="DataGridView"/>'s row/column
		/// scrollbars). Re-applies on handle creation, since the scrollbar handles may not exist
		/// yet when the theme is first applied. Best effort: <c>SetWindowTheme</c> simply has no
		/// visible effect on OS versions that do not support the dark scrollbar theme.
		/// </summary>
		private static void EnableDarkScrollBars([NotNull] Control control)
		{
			void Apply()
			{
				if (! control.IsHandleCreated)
				{
					return;
				}

				SetWindowTheme(control.Handle, _darkExplorerTheme, null);

				foreach (Control child in control.Controls)
				{
					if (child is ScrollBar)
					{
						// Accessing Handle forces creation for scrollbars not yet visible.
						SetWindowTheme(child.Handle, _darkExplorerTheme, null);
					}
				}
			}

			if (control.IsHandleCreated)
			{
				Apply();
			}

			control.HandleCreated += (sender, args) => Apply();
		}

		/// <summary>
		/// Draws a thin bottom and right border on each header cell instead of the default
		/// WinForms bevel (see <see cref="ApplyModernHeaderStyle"/>). The border color is derived
		/// from the header's current background so it stays legible whether the light or dark
		/// theme's header colors are applied.
		/// </summary>
		private static void PaintHeaderBorder(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (e.RowIndex != -1 || e.ColumnIndex < 0)
			{
				return;
			}

			var grid = (DataGridView) sender;

			e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);

			Color headerBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
			Color borderColor = headerBackColor.GetBrightness() < 0.5f
				                    ? _borderColor
				                    : _headerBorderLight;

			using (var pen = new Pen(borderColor))
			{
				e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Top,
				                    e.CellBounds.Right, e.CellBounds.Top);
				e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1,
				                    e.CellBounds.Right, e.CellBounds.Bottom - 1);
				e.Graphics.DrawLine(pen, e.CellBounds.Right - 1, e.CellBounds.Top,
				                    e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
			}

			e.Handled = true;
		}

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute,
		                                                ref int value, int size);

		private const int _dwmUseImmersiveDarkModeOld = 19; // Windows 10 before 20H1
		private const int _dwmUseImmersiveDarkMode = 20;
		private const int _dwmBorderColor = 34; // Windows 11 and later
		private const int _dwmCaptionColor = 35;

		/// <summary>
		/// Lets the window manager draw the title bar and frame dark. Best effort: older Windows
		/// versions ignore the attributes they do not know.
		/// </summary>
		private static void EnableDarkTitleBar([NotNull] Form form)
		{
			void Apply()
			{
				var on = 1;
				if (DwmSetWindowAttribute(form.Handle, _dwmUseImmersiveDarkMode, ref on,
				                          sizeof(int)) != 0)
				{
					DwmSetWindowAttribute(form.Handle, _dwmUseImmersiveDarkModeOld, ref on,
					                      sizeof(int));
				}

				int caption = ColorTranslator.ToWin32(_dialogBackground);
				DwmSetWindowAttribute(form.Handle, _dwmCaptionColor, ref caption, sizeof(int));

				int border = ColorTranslator.ToWin32(_borderColor);
				DwmSetWindowAttribute(form.Handle, _dwmBorderColor, ref border, sizeof(int));
			}

			if (form.IsHandleCreated)
			{
				Apply();
			}

			form.HandleCreated -= ApplyDarkTitleBarOnHandleCreated;
			form.HandleCreated += ApplyDarkTitleBarOnHandleCreated;
		}

		private static void ApplyDarkTitleBarOnHandleCreated(object sender, EventArgs e)
		{
			EnableDarkTitleBar((Form) sender);
		}

		private static readonly ConditionalWeakTable<TabControl, DarkTabControlPainter>
			_tabPainters = new ConditionalWeakTable<TabControl, DarkTabControlPainter>();

		/// <summary>
		/// Paints the whole tab control (strip, tabs and page frame) dark after the native
		/// painting. The tab pages are child windows and clipped out, so they paint themselves.
		/// </summary>
		private class DarkTabControlPainter : NativeWindow
		{
			private const int WM_PAINT = 0x000F;

			private readonly TabControl _tabControl;

			public DarkTabControlPainter([NotNull] TabControl tabControl)
			{
				_tabControl = tabControl;

				if (tabControl.IsHandleCreated)
				{
					AssignHandle(tabControl.Handle);
				}

				tabControl.HandleCreated += (sender, args) => AssignHandle(_tabControl.Handle);
				tabControl.HandleDestroyed += (sender, args) => ReleaseHandle();
			}

			protected override void WndProc(ref Message m)
			{
				base.WndProc(ref m);

				if (m.Msg == WM_PAINT)
				{
					using (Graphics graphics = Graphics.FromHwnd(Handle))
					{
						Paint(graphics);
					}
				}
			}

			private void Paint([NotNull] Graphics graphics)
			{
				using (var dialogBrush = new SolidBrush(_dialogBackground))
				using (var controlBrush = new SolidBrush(_controlBackground))
				using (var borderPen = new Pen(_borderColor))
				{
					graphics.FillRectangle(dialogBrush, _tabControl.ClientRectangle);

					Rectangle page = _tabControl.DisplayRectangle;
					page.Inflate(1, 1);
					graphics.DrawRectangle(borderPen, page);

					for (var i = 0; i < _tabControl.TabCount; i++)
					{
						Rectangle tab = _tabControl.GetTabRect(i);
						bool selected = i == _tabControl.SelectedIndex;

						if (selected)
						{
							// Joins the page: no bottom line, one pixel taller.
							tab.Height += 1;
						}

						graphics.FillRectangle(selected ? dialogBrush : controlBrush, tab);
						graphics.DrawLines(borderPen, new[]
						                              {
							                              new Point(tab.Left, tab.Bottom),
							                              new Point(tab.Left, tab.Top),
							                              new Point(tab.Right, tab.Top),
							                              new Point(tab.Right, tab.Bottom)
						                              });

						TextRenderer.DrawText(graphics, _tabControl.TabPages[i].Text,
						                      _tabControl.Font, tab, _textColor,
						                      TextFormatFlags.HorizontalCenter |
						                      TextFormatFlags.VerticalCenter);
					}
				}
			}
		}

		/// <summary>
		/// Paints over the group box's own (white) frame and caption with a grey frame.
		/// </summary>
		private static void PaintGroupBox(object sender, PaintEventArgs e)
		{
			var box = (GroupBox) sender;
			Graphics graphics = e.Graphics;
			Rectangle client = box.ClientRectangle;

			// Same caption offset as the native group box.
			const int textLeft = 8;
			Size textSize = TextRenderer.MeasureText(graphics, box.Text, box.Font);
			int top = textSize.Height / 2;
			int right = client.Right - 1;
			int bottom = client.Bottom - 1;

			using (var back = new SolidBrush(box.BackColor))
			{
				graphics.FillRectangle(back, client);
			}

			using (var pen = new Pen(_borderColor))
			{
				graphics.DrawLine(pen, 0, top, 0, bottom);
				graphics.DrawLine(pen, 0, bottom, right, bottom);
				graphics.DrawLine(pen, right, top, right, bottom);

				if (string.IsNullOrEmpty(box.Text))
				{
					graphics.DrawLine(pen, 0, top, right, top);
					return;
				}

				graphics.DrawLine(pen, 0, top, textLeft, top);
				graphics.DrawLine(pen, textLeft + textSize.Width, top, right, top);
			}

			TextRenderer.DrawText(graphics, box.Text, box.Font, new Point(textLeft, 0),
			                      box.Enabled ? box.ForeColor : SystemColors.GrayText);
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
