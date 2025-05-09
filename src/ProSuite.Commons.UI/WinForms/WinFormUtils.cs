using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using IWin32Window = System.Windows.Forms.IWin32Window;
using Point = System.Drawing.Point;

namespace ProSuite.Commons.UI.WinForms
{
	public static class WinFormUtils
	{
		/// <summary>
		/// Maximizes a form on a given screen
		/// </summary>
		/// <param name="form">The form.</param>
		/// <param name="screen">The screen.</param>
		public static void MaximizeOnScreen([NotNull] Form form, [NotNull] Screen screen)
		{
			Assert.ArgumentNotNull(form, nameof(form));
			Assert.ArgumentNotNull(screen, nameof(screen));

			Rectangle rectangle = screen.Bounds;

			form.StartPosition = FormStartPosition.Manual;
			form.WindowState = FormWindowState.Normal;

			form.Size = rectangle.Size;
			form.Location = new Point(rectangle.Left, rectangle.Top);
		}

		/// <summary>
		/// Maximizes a form on all screens
		/// </summary>
		/// <param name="form">The form.</param>
		public static void MaximizeOnAllScreens([NotNull] Form form)
		{
			Assert.ArgumentNotNull(form, nameof(form));

			var rectangle = new Rectangle(0, 0, 1, 1);
			foreach (Screen screen in Screen.AllScreens)
			{
				rectangle = Rectangle.Union(rectangle, screen.Bounds);
			}

			form.StartPosition = FormStartPosition.Manual;
			form.WindowState = FormWindowState.Normal;

			form.Size = rectangle.Size;
			form.Location = new Point(rectangle.Left, rectangle.Top);
		}

		/// <summary>
		/// Handles the tab key (if pressed) and tries to move the focus to the next
		/// control in the tab order which can be tabbed-to.
		/// </summary>
		/// <param name="form">The form.</param>
		/// <param name="keyEventArgs">The <see cref="System.Windows.Forms.KeyEventArgs"/> instance containing the event data.</param>
		/// <returns>
		/// 	<c>true</c> if the tab key was pressed and the focus was moved to the next control,
		/// <c>false</c> if either another key was pressed or no control exists to tab to.
		/// </returns>
		public static bool HandleTabKey([NotNull] Form form, KeyEventArgs keyEventArgs)
		{
			return HandleTabKey(form,
			                    keyEventArgs.KeyCode,
			                    keyEventArgs.Modifiers);
		}

		/// <summary>
		/// Handles the tab key (if pressed) and tries to move the focus to the next
		/// control in the tab order which can be tabbed-to.
		/// </summary>
		/// <param name="form">The form.</param>
		/// <param name="keyCode">The key code.</param>
		/// <param name="modifiers">The modifiers.</param>
		/// <returns><c>true</c> if the tab key was pressed and the focus was moved to the next control, 
		/// <c>false</c> if either another key was pressed or no control exists to tab to.</returns>
		public static bool HandleTabKey([NotNull] Form form, Keys keyCode, Keys modifiers)
		{
			Assert.ArgumentNotNull(form, nameof(form));

			if (keyCode == Keys.Tab)
			{
				bool forward = modifiers != Keys.Shift;

				FocusNextControl(form, forward);
			}

			return false;
		}

		/// <summary>
		/// Tries to move the focus to the next control in the tab order which can be tabbed-to.
		/// </summary>
		/// <param name="form">The form.</param>
		/// <param name="forward">if set to <c>true</c> the next control in the tab order is focused, 
		/// otherwise the previous control is focused.</param>
		public static void FocusNextControl([NotNull] Form form, bool forward)
		{
			Assert.ArgumentNotNull(form, nameof(form));

			Control focusControl = GetFocusedControl(form.Controls);

			if (focusControl != null)
			{
				FocusNextControl(focusControl, forward);
			}
		}

		/// <summary>
		/// Gets the focused control in the given control collection.
		/// </summary>
		/// <param name="controls">The control collection to search for a focused control.</param>
		/// <returns>Focused control, or null if no control in the collection has focus.</returns>
		[CanBeNull]
		public static Control GetFocusedControl([NotNull] Control.ControlCollection controls)
		{
			Assert.ArgumentNotNull(controls, nameof(controls));

			// get focused control
			foreach (Control control in controls)
			{
				if (control.Focused)
				{
					return control;
				}

				if (control.ContainsFocus)
				{
					return control.Controls.Count == 0
						       ? control
						       : GetFocusedControl(control.Controls);
				}
			}

			// no focus
			return null;
		}

		public static Point GetCorrectedPopupLocation([NotNull] Control control,
		                                              Point clickLocation)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			Rectangle workingArea = Screen.GetWorkingArea(clickLocation);

			int x = clickLocation.X;
			int y = clickLocation.Y;

			if (workingArea.Right < x + control.Width)
			{
				// try to open the control to the left
				x = clickLocation.X - control.Width;

				if (x < workingArea.Left)
				{
					// too wide for work area -> fit to left
					x = workingArea.Left;
				}
			}

			if (workingArea.Bottom < y + control.Height)
			{
				// try to open the control to the top
				y = clickLocation.Y - control.Height;

				if (y < workingArea.Top)
				{
					// too high for work area -> fit to top
					y = workingArea.Top;
				}
			}

			return new Point(x, y);
		}

		/// <summary>
		/// Draws the content of a given control to a bitmap and returns it.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <returns></returns>
		[NotNull]
		public static Bitmap DrawToBitmap([NotNull] Control control)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			var bitmap = new Bitmap(control.Width, control.Height);

			control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

			return bitmap;
		}

		/// <summary>
		/// Shows a tooltip on a disabled control. Use in MouseMove event handler of the parent control.
		/// </summary>
		/// <param name="parentControl">The parent control (sender of the MouseMove event)</param>
		/// <param name="mouseLocation">The mouse location from the event args</param>
		/// <param name="toolTip">The tool tip</param>
		/// <param name="toolTipString">The string to display</param>
		/// <param name="lastTooltipShowingControl">A field in the form necessary to maintain the information 
		/// of the tooltip showing control</param>
		public static void ShowTooltipOnDisabledControl(
			[NotNull] Control parentControl,
			Point mouseLocation,
			[NotNull] ToolTip toolTip,
			[NotNull] string toolTipString,
			[CanBeNull] ref Control lastTooltipShowingControl)
		{
			const int duration = 5000;
			ShowTooltipOnDisabledControl(parentControl, mouseLocation, toolTip, toolTipString,
			                             duration, ref lastTooltipShowingControl);
		}

		/// <summary>
		/// Shows a tooltip on a disabled control. Use in MouseMove event handler of the parent control.
		/// </summary>
		/// <param name="parentControl">The parent control (sender of the MouseMove event)</param>
		/// <param name="mouseLocation">The mouse location from the event args</param>
		/// <param name="toolTip">The tool tip</param>
		/// <param name="toolTipString">The string to display</param>
		/// <param name="duration">The duration the tooltip is shown</param>
		/// <param name="lastTooltipShowingControl">A field in the form necessary to maintain the information 
		/// of the tooltip showing control</param>
		public static void ShowTooltipOnDisabledControl(
			[NotNull] Control parentControl,
			Point mouseLocation,
			[NotNull] ToolTip toolTip,
			[NotNull] string toolTipString,
			int duration,
			[CanBeNull] ref Control lastTooltipShowingControl)
		{
			Control control = parentControl.GetChildAtPoint(mouseLocation);

			if (control != null && ! control.Enabled)
			{
				if (lastTooltipShowingControl == null)
				{
					toolTip.ShowAlways = true;

					// to avoid that the cursor obscures the tooltip, add an offset
					const int offset = 16;

					// because the mouse event is from the parent and we show it on the child
					var correctedLocation = new Point(mouseLocation.X - control.Location.X + offset,
					                                  mouseLocation.Y - control.Location.Y +
					                                  offset);

					toolTip.Show(toolTipString, control, correctedLocation, duration);

					lastTooltipShowingControl = control;
				}
			}
			else
			{
				if (lastTooltipShowingControl != null)
				{
					toolTip.Hide(lastTooltipShowingControl);
				}

				lastTooltipShowingControl = null;
			}
		}

		public static IWin32Window GetWin32Window(Window wpfWindow)
		{
			return new WpfWin32Window(wpfWindow);
		}

		#region Non-public members

		private static void FocusNextControl([NotNull] Control control, bool forward)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			Control form = control.TopLevelControl;

			Assert.NotNull(form, "top level control is null");

			const bool tabStopOnly = true;
			const bool nested = true;
			const bool wrap = true;
			bool selected = form.SelectNextControl(control, forward,
			                                       tabStopOnly, nested, wrap);

			if (! selected)
			{
				Control parent = GetHighestParentWithMultipleChildren(control);
				if (parent != null)
				{
					parent.SelectNextControl(parent, forward, tabStopOnly, nested, wrap);
				}
			}
		}

		[CanBeNull]
		private static Control GetHighestParentWithMultipleChildren(
			[NotNull] Control control)
		{
			return GetHighestParentWithMultipleChildren(control, out int _);
		}

		[CanBeNull]
		private static Control GetHighestParentWithMultipleChildren(
			[NotNull] Control control, out int descendantIndex)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			descendantIndex = -1;

			if (control.Parent == null)
			{
				return null;
			}

			Control primeFather = GetHighestParentWithMultipleChildren(control.Parent,
				out descendantIndex);

			if (primeFather != null)
			{
				return primeFather;
			}

			if (control.Parent.Controls.Count > 1)
			{
				descendantIndex = control.Parent.Controls.GetChildIndex(control);
				return control.Parent;
			}

			return null;
		}

		#endregion

		private class WpfWin32Window : IWin32Window
		{
			private readonly WindowInteropHelper _interopHelper;

			public WpfWin32Window(Window window)
			{
				_interopHelper = new WindowInteropHelper(window);
			}

			public IntPtr Handle => _interopHelper.Handle;
		}
	}
}
