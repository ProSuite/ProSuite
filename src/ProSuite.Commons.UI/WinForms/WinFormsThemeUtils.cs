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
		private static readonly Color _disabledTextColor = ColorTranslator.FromHtml("#7A7A7A");

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
			else if (control is ComboBox comboBox &&
			         comboBox.DropDownStyle == ComboBoxStyle.DropDownList)
			{
				// Dark mode leaves some drop-down lists light. The flat style is drawn with the
				// (dark) system colors.
				comboBox.FlatStyle = FlatStyle.Flat;
			}
			else if (control is DateTimePicker dateTimePicker)
			{
				// Dark mode draws the spin buttons light.
				PaintDarkSpinButtons(dateTimePicker);
			}

			foreach (Control child in control.Controls)
			{
				ApplySystemDarkModeFixes(child);
			}
		}

		/// <summary>
		/// The dark theme counterpart of a light background color, e.g. of a configured color
		/// that marks a state: white becomes the background of dark controls, tinted colors keep
		/// their hue. Dark colors are returned unchanged.
		/// </summary>
		public static Color GetDarkBackColor(Color color)
		{
			float lightness = color.GetBrightness();

			if (lightness <= 0.5f)
			{
				return color;
			}

			float darkLightness = _controlBackground.GetBrightness() + (1 - lightness) / 2;

			return FromHsl(color.GetHue(), color.GetSaturation(), darkLightness);
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

			// Controls added later, e.g. the fields of a form loaded on demand, are themed too.
			control.ControlAdded -= ApplyDarkThemeOnControlAdded;
			control.ControlAdded += ApplyDarkThemeOnControlAdded;

			if (control is ToolStrip toolStrip)
			{
				ApplyDarkTheme(toolStrip);
			}
			else if (control is TextBoxBase || control is ComboBox || control is ListControl ||
			         control is UpDownBase)
			{
				// Keeps a dark color set before, e.g. the state color of an attribute editor field.
				if (control.BackColor.GetBrightness() > 0.5f)
				{
					control.BackColor = _controlBackground;
				}

				control.ForeColor = _textColor;

				if (control is ComboBox comboBox &&
				    comboBox.DropDownStyle != ComboBoxStyle.Simple)
				{
					// The visual style draws a drop-down list light, whatever its BackColor, and
					// the flat style draws the border and the button light.
					comboBox.FlatStyle = FlatStyle.Flat;
					_painters.GetValue(comboBox, c => new DarkComboBoxPainter((ComboBox) c));
				}

				if (control is TextBoxBase textBox)
				{
					// The visual style draws a white Fixed3D border, but a grey FixedSingle one.
					if (textBox.BorderStyle == BorderStyle.Fixed3D)
					{
						textBox.BorderStyle = BorderStyle.FixedSingle;
					}

					// Multiline text boxes (e.g. the details view) have their own scrollbars.
					EnableDarkExplorerTheme(control);
				}
				else if (control is UpDownBase)
				{
					foreach (Control child in control.Controls)
					{
						if (! (child is TextBoxBase))
						{
							_painters.GetValue(child, c => new DarkSpinPainter(c, h => h));
						}
					}
				}
			}
			else if (control is DateTimePicker dateTimePicker)
			{
				PaintDarkSpinButtons(dateTimePicker);
			}
			else if (control is TrackBar trackBar)
			{
				_painters.GetValue(trackBar, c => new DarkTrackBarPainter((TrackBar) c));
			}
			else if (control is Button button)
			{
				button.BackColor = _controlBackground;
				button.ForeColor = _textColor;
				button.UseVisualStyleBackColor = false;
				button.FlatStyle = FlatStyle.Flat;
				button.FlatAppearance.BorderColor = _borderColor;

				button.Paint -= PaintDisabledButtonText;
				button.Paint += PaintDisabledButtonText;
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
					_painters.GetValue(tabControl,
					                   t => new DarkTabControlPainter((TabControl) t));
				}
				else if (control is CheckBox || control is RadioButton)
				{
					// The default visual style draws the box and the circle light.
					EnableDarkExplorerTheme(control);
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

			// The raised headers have white edges; single borders use the GridColor.
			if (grid.ColumnHeadersBorderStyle == DataGridViewHeaderBorderStyle.Raised)
			{
				grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			}

			if (grid.RowHeadersBorderStyle == DataGridViewHeaderBorderStyle.Raised)
			{
				grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			}

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

			EnableDarkExplorerTheme(grid);

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
			else if (item is ToolStripControlHost host)
			{
				// E.g. the combo box of a ToolStripComboBox.
				ApplyDarkTheme(host.Control);
			}
		}

		private static void ApplyDarkThemeOnControlAdded(object sender, ControlEventArgs e)
		{
			ApplyDarkTheme(e.Control);
		}

		private static void PaintDarkSpinButtons([NotNull] DateTimePicker picker)
		{
			_painters.GetValue(picker, c => new DarkSpinPainter(
				                           c, h => FindWindowEx(h, IntPtr.Zero, "msctls_updown32",
				                                                null)));
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter,
		                                          string className, string windowName);

		private static Color FromHsl(float hue, float saturation, float lightness)
		{
			float chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
			float sector = hue / 60;
			float second = chroma * (1 - Math.Abs(sector % 2 - 1));
			float offset = lightness - chroma / 2;

			float red = 0, green = 0, blue = 0;

			switch ((int) sector)
			{
				case 0:
					red = chroma;
					green = second;
					break;
				case 1:
					red = second;
					green = chroma;
					break;
				case 2:
					green = chroma;
					blue = second;
					break;
				case 3:
					green = second;
					blue = chroma;
					break;
				case 4:
					red = second;
					blue = chroma;
					break;
				default:
					red = chroma;
					blue = second;
					break;
			}

			return Color.FromArgb((int) Math.Round((red + offset) * 255),
			                      (int) Math.Round((green + offset) * 255),
			                      (int) Math.Round((blue + offset) * 255));
		}

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName,
		                                         string pszSubIdList);

		private const string _darkExplorerTheme = "DarkMode_Explorer";

		/// <summary>
		/// Switches a control (e.g. the box of a check box) and its native scrollbars to the dark
		/// Explorer theme so they are not drawn light in the otherwise dark UI. Handles the
		/// control's own scrollbars and any child <see cref="ScrollBar"/> controls (e.g. a
		/// <see cref="DataGridView"/>'s row/column scrollbars). Re-applies on handle creation, since the scrollbar handles may not exist
		/// yet when the theme is first applied. Best effort: <c>SetWindowTheme</c> simply has no
		/// visible effect on OS versions that do not support the dark scrollbar theme.
		/// </summary>
		private static void EnableDarkExplorerTheme([NotNull] Control control)
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

		private static readonly ConditionalWeakTable<Control, DarkWindowPainter> _painters =
			new ConditionalWeakTable<Control, DarkWindowPainter>();

		/// <summary>
		/// Paints over the native painting of a control's window, or of a native child window
		/// found by <c>getWindow</c> from the control's handle, also after handle re-creation.
		/// </summary>
		private abstract class DarkWindowPainter : NativeWindow
		{
			private const int WM_PAINT = 0x000F;

			protected DarkWindowPainter([NotNull] Control control,
			                            [NotNull] Func<IntPtr, IntPtr> getWindow)
			{
				void Assign()
				{
					IntPtr window = getWindow(control.Handle);

					if (window != IntPtr.Zero)
					{
						AssignHandle(window);
					}
				}

				if (control.IsHandleCreated)
				{
					Assign();
				}

				control.HandleCreated += (sender, args) => Assign();
				control.HandleDestroyed += (sender, args) => ReleaseHandle();
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

			protected virtual void Paint([NotNull] Graphics graphics) { }

			protected static void DrawArrow([NotNull] Graphics graphics, Rectangle bounds,
			                                bool up, Color color, [NotNull] Control control)
			{
				int size = control.LogicalToDeviceUnits(3);
				int x = bounds.Left + bounds.Width / 2;
				int top = bounds.Top + (bounds.Height - size) / 2;

				Point[] points = up
					                 ? new[]
					                   {
						                   new Point(x - size, top + size),
						                   new Point(x + size, top + size),
						                   new Point(x, top)
					                   }
					                 : new[]
					                   {
						                   new Point(x - size, top),
						                   new Point(x + size, top),
						                   new Point(x, top + size)
					                   };

				using (var brush = new SolidBrush(color))
				{
					graphics.FillPolygon(brush, points);
				}
			}
		}

		/// <summary>
		/// Paints the border and the button of a flat combo box dark: the flat style draws them
		/// with the light system colors.
		/// </summary>
		private class DarkComboBoxPainter : DarkWindowPainter
		{
			private readonly ComboBox _comboBox;
			[UsedImplicitly] private readonly DisabledEditPainter _editPainter;

			public DarkComboBoxPainter([NotNull] ComboBox comboBox) : base(comboBox, h => h)
			{
				_comboBox = comboBox;
				_editPainter = new DisabledEditPainter(comboBox);
			}

			/// <summary>
			/// The edit field of a disabled combo box is drawn light, whatever its BackColor.
			/// </summary>
			private class DisabledEditPainter : DarkWindowPainter
			{
				private readonly ComboBox _comboBox;

				public DisabledEditPainter([NotNull] ComboBox comboBox)
					: base(comboBox, h => FindWindowEx(h, IntPtr.Zero, "Edit", null))
				{
					_comboBox = comboBox;
				}

				protected override void Paint(Graphics graphics)
				{
					if (_comboBox.Enabled)
					{
						return;
					}

					Rectangle bounds = Rectangle.Truncate(graphics.VisibleClipBounds);

					using (var back = new SolidBrush(_comboBox.BackColor))
					{
						graphics.FillRectangle(back, bounds);
					}

					TextRenderer.DrawText(graphics, _comboBox.Text, _comboBox.Font, bounds,
					                      _disabledTextColor,
					                      TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
					                      TextFormatFlags.NoPadding);
				}
			}

			protected override void Paint(Graphics graphics)
			{
				var info = new ComboBoxInfo { Size = Marshal.SizeOf(typeof(ComboBoxInfo)) };

				if (! GetComboBoxInfo(Handle, ref info))
				{
					return;
				}

				Rectangle client = _comboBox.ClientRectangle;

				// Also covers the separator left of the button.
				Rectangle button = Rectangle.FromLTRB(info.ItemRight, 1, client.Right - 1,
				                                      client.Bottom - 1);

				using (var back = new SolidBrush(_comboBox.BackColor))
				using (var ring = new Pen(_comboBox.BackColor, 3))
				using (var border = new Pen(_borderColor))
				{
					graphics.FillRectangle(back, button);

					// The inner frame of a disabled combo box is drawn light.
					graphics.DrawRectangle(ring, 2, 2, client.Width - 5, client.Height - 5);
					graphics.DrawRectangle(border, 0, 0, client.Width - 1, client.Height - 1);
				}

				DrawArrow(graphics, button, false,
				          _comboBox.Enabled ? _textColor : _disabledTextColor, _comboBox);
			}

			[DllImport("user32.dll")]
			private static extern bool GetComboBoxInfo(IntPtr comboBox, ref ComboBoxInfo info);

			[StructLayout(LayoutKind.Sequential)]
			private struct ComboBoxInfo
			{
				public int Size;
				public int ItemLeft, ItemTop, ItemRight, ItemBottom;
				public int ButtonLeft, ButtonTop, ButtonRight, ButtonBottom;
				public int ButtonState;
				public IntPtr Combo, Item, List;
			}
		}

		/// <summary>
		/// Paints vertical spin buttons dark: the visual style draws them light.
		/// </summary>
		private class DarkSpinPainter : DarkWindowPainter
		{
			private readonly Control _control;

			public DarkSpinPainter([NotNull] Control control,
			                       [NotNull] Func<IntPtr, IntPtr> getWindow)
				: base(control, getWindow)
			{
				_control = control;
			}

			protected override void Paint(Graphics graphics)
			{
				Rectangle bounds = Rectangle.Truncate(graphics.VisibleClipBounds);
				int middle = bounds.Top + bounds.Height / 2;

				using (var back = new SolidBrush(_controlBackground))
				{
					graphics.FillRectangle(back, bounds);
				}

				using (var pen = new Pen(_borderColor))
				{
					graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
					graphics.DrawLine(pen, bounds.Left, middle, bounds.Right, middle);
				}

				Color color = _control.Enabled ? _textColor : _disabledTextColor;

				DrawArrow(graphics,
				          Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, middle),
				          true, color, _control);
				DrawArrow(graphics,
				          Rectangle.FromLTRB(bounds.Left, middle, bounds.Right, bounds.Bottom),
				          false, color, _control);
			}
		}

		/// <summary>
		/// Draws the channel of a track bar dark, through the custom draw notification its parent
		/// reflects to it: the visual style draws the channel light.
		/// </summary>
		private class DarkTrackBarPainter : DarkWindowPainter
		{
			private const int WM_REFLECT_NOTIFY = 0x2000 + 0x004E;
			private const int NM_CUSTOMDRAW = -12;
			private const int CDDS_PREPAINT = 0x00001;
			private const int CDDS_ITEMPREPAINT = 0x10001;
			private const int CDRF_SKIPDEFAULT = 0x04;
			private const int CDRF_NOTIFYITEMDRAW = 0x20;
			private const int TBCD_CHANNEL = 3;

			public DarkTrackBarPainter([NotNull] TrackBar trackBar) : base(trackBar, h => h) { }

			protected override void WndProc(ref Message m)
			{
				if (m.Msg != WM_REFLECT_NOTIFY || ! TryDrawChannel(ref m))
				{
					base.WndProc(ref m);
				}
			}

			private static bool TryDrawChannel(ref Message m)
			{
				var draw = (CustomDraw) Marshal.PtrToStructure(m.LParam, typeof(CustomDraw));

				if (draw.Header.Code != NM_CUSTOMDRAW)
				{
					return false;
				}

				if (draw.DrawStage == CDDS_PREPAINT)
				{
					m.Result = (IntPtr) CDRF_NOTIFYITEMDRAW;
					return true;
				}

				if (draw.DrawStage != CDDS_ITEMPREPAINT || (int) draw.ItemSpec != TBCD_CHANNEL)
				{
					return false;
				}

				using (Graphics graphics = Graphics.FromHdc(draw.Hdc))
				using (var brush = new SolidBrush(_disabledTextColor))
				{
					graphics.FillRectangle(brush, Rectangle.FromLTRB(draw.Left, draw.Top,
					                                                 draw.Right, draw.Bottom));
				}

				m.Result = (IntPtr) CDRF_SKIPDEFAULT;
				return true;
			}

			[StructLayout(LayoutKind.Sequential)]
			private struct NotifyHeader
			{
				public IntPtr WindowFrom;
				public IntPtr IdFrom;
				public int Code;
			}

			[StructLayout(LayoutKind.Sequential)]
			private struct CustomDraw
			{
				public NotifyHeader Header;
				public int DrawStage;
				public IntPtr Hdc;
				public int Left, Top, Right, Bottom;
				public IntPtr ItemSpec;
				public int ItemState;
				public IntPtr ItemParam;
			}
		}

		/// <summary>
		/// Paints the whole tab control (strip, tabs and page frame) dark after the native
		/// painting. The tab pages are child windows and clipped out, so they paint themselves.
		/// </summary>
		private class DarkTabControlPainter : DarkWindowPainter
		{
			private readonly TabControl _tabControl;

			public DarkTabControlPainter([NotNull] TabControl tabControl) : base(tabControl, h => h)
			{
				_tabControl = tabControl;
			}

			protected override void Paint(Graphics graphics)
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
		/// <summary>
		/// A disabled flat button derives its text color from the dark background and ignores
		/// ForeColor, which leaves the caption barely readable. Redraws the caption of text-only
		/// buttons in a grey that stays readable.
		/// </summary>
		private static void PaintDisabledButtonText(object sender, PaintEventArgs e)
		{
			var button = (Button) sender;

			if (button.Enabled || button.Image != null || string.IsNullOrEmpty(button.Text))
			{
				return;
			}

			Rectangle inner = button.ClientRectangle;
			inner.Inflate(-1, -1);

			using (var back = new SolidBrush(button.BackColor))
			{
				e.Graphics.FillRectangle(back, inner);
			}

			TextRenderer.DrawText(e.Graphics, button.Text, button.Font, inner,
			                      _disabledTextColor,
			                      TextFormatFlags.HorizontalCenter |
			                      TextFormatFlags.VerticalCenter);
		}

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
