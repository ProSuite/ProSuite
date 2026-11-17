using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Pane = ArcGIS.Desktop.Framework.Contracts.Pane;

namespace ProSuite.Commons.AGP.Framework;

/// <summary>
/// Gives the keyboard from a pane back to the active map view, so that the active tool keeps
/// receiving the keys. This happens when the pane is shown with the focus
/// (DockPane.Activate(true)) and after a click into the pane. Text boxes and combo boxes keep
/// the keyboard while they are used; Enter or Escape in a text box and closing a combo box's
/// drop-down or a date picker's calendar give it back.
/// </summary>
public class KeyboardFocusReturner
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly FrameworkElement _content;

	// The element in a map pane that had the keyboard before the pane, e.g. the ViewerHost.
	[CanBeNull] private IInputElement _mapFocus;

	private bool _returnOnMouseUp;

	private KeyboardFocusReturner([NotNull] FrameworkElement content)
	{
		Assert.ArgumentNotNull(content, nameof(content));

		_content = content;

		_content.AddHandler(UIElement.GotKeyboardFocusEvent,
		                    new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus), true);
		_content.AddHandler(UIElement.PreviewMouseUpEvent,
		                    new MouseButtonEventHandler(OnPreviewMouseUp), true);
		_content.AddHandler(UIElement.PreviewKeyDownEvent,
		                    new KeyEventHandler(OnPreviewKeyDown), true);
		_content.AddHandler(UIElement.PreviewMouseDownEvent,
		                    new MouseButtonEventHandler(OnPreviewMouseDown), true);
	}

	/// <summary>
	/// Makes the given pane content give the keyboard back to the active map view.
	/// </summary>
	public static void Attach([NotNull] FrameworkElement content)
	{
		// The handlers keep the instance alive as long as the content lives.
		_ = new KeyboardFocusReturner(content);
	}

	private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		try
		{
			var oldFocus = e.OldFocus as DependencyObject;
			bool cameFromOutside = ! IsInside(oldFocus);

			if (cameFromOutside && FindMapPane(oldFocus) != null)
			{
				_mapFocus = e.OldFocus;
			}

			var newFocus = e.NewFocus as DependencyObject;

			if (KeepsKeyboard(newFocus))
			{
				return;
			}

			if (newFocus is ComboBox or ComboBoxItem)
			{
				// Choosing with the keys in the drop-down: the keyboard goes back once it closes.
				ComboBox comboBox = FindComboBox(newFocus);

				if (comboBox != null)
				{
					comboBox.DropDownClosed -= OnDropDownClosed;
					comboBox.DropDownClosed += OnDropDownClosed;
				}

				return;
			}

			if (IsMouseButtonDown() && _content.IsMouseOver)
			{
				// A button loses its click when it loses the focus while pressed.
				_returnOnMouseUp = true;
				return;
			}

			if (! cameFromOutside)
			{
				// Moving with Tab inside the pane.
				return;
			}

			ReturnLater();
		}
		catch (Exception ex)
		{
			// Runs from a WPF event, so nothing may escape.
			_msg.Warn("Could not give the keyboard back from the pane", ex);
		}
	}

	private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (! _returnOnMouseUp || IsMouseButtonDown())
			{
				return;
			}

			_returnOnMouseUp = false;

			// After the click is handled: the button raises it on this mouse up.
			ReturnLater();
		}
		catch (Exception ex)
		{
			// Runs from a WPF event, so nothing may escape.
			_msg.Warn("Could not give the keyboard back from the pane", ex);
		}
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		try
		{
			if (e.Key is not (Key.Enter or Key.Escape) ||
			    e.OriginalSource is not TextBoxBase { AcceptsReturn: false })
			{
				return;
			}

			// After the text box has taken the Enter. Its binding writes the value back when
			// it loses the focus.
			ReturnLater(leaveTextBox: true);
		}
		catch (Exception ex)
		{
			// Runs from a WPF event, so nothing may escape.
			_msg.Warn("Could not give the keyboard back from the pane", ex);
		}
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			// Opening a date picker's calendar does not move the keyboard, so the date picker
			// is found by the click.
			DatePicker datePicker = FindAncestor<DatePicker>(e.OriginalSource as DependencyObject);

			if (datePicker is null)
			{
				return;
			}

			datePicker.CalendarClosed -= OnCalendarClosed;
			datePicker.CalendarClosed += OnCalendarClosed;
		}
		catch (Exception ex)
		{
			// Runs from a WPF event, so nothing may escape.
			_msg.Warn("Could not give the keyboard back from the pane", ex);
		}
	}

	private void OnCalendarClosed(object sender, RoutedEventArgs e)
	{
		try
		{
			((DatePicker) sender).CalendarClosed -= OnCalendarClosed;

			// The date picker puts the keyboard into its text box once a date is chosen from
			// the calendar.
			ReturnLater(leaveTextBox: true);
		}
		catch (Exception ex)
		{
			// Runs from a WPF event, so nothing may escape.
			_msg.Warn("Could not give the keyboard back from the pane", ex);
		}
	}

	private void OnDropDownClosed(object sender, EventArgs e)
	{
		try
		{
			var comboBox = (ComboBox) sender;
			comboBox.DropDownClosed -= OnDropDownClosed;

			if (comboBox.IsEditable)
			{
				return;
			}

			ReturnLater();
		}
		catch (Exception ex)
		{
			// Runs from a WPF event, so nothing may escape.
			_msg.Warn("Could not give the keyboard back from the pane", ex);
		}
	}

	private void ReturnLater(bool leaveTextBox = false)
	{
		_content.Dispatcher.BeginInvoke(DispatcherPriority.Background,
		                                new Action(() => Return(leaveTextBox)));
	}

	private void Return(bool leaveTextBox)
	{
		try
		{
			var focused =
				System.Windows.Input.Keyboard.FocusedElement as DependencyObject;

			if (! IsInside(focused) || KeepsKeyboard(focused) && ! leaveTextBox)
			{
				// The keyboard has moved on meanwhile, or is in a text box.
				return;
			}

			MapView mapView = MapView.Active;

			if (mapView is null)
			{
				return;
			}

			if (_mapFocus is DependencyObject target &&
			    PresentationSource.FromDependencyObject(target) != null &&
			    FindMapPane(target)?.MapView == mapView)
			{
				Window window = Window.GetWindow(target);

				if (window is { IsActive: false })
				{
					// E.g. a floating stereo map window.
					window.Activate();
				}

				_msg.VerboseDebug(() => $"Keyboard back to {target.GetType().Name}");

				System.Windows.Input.Keyboard.Focus(_mapFocus);
				return;
			}

			// The keyboard came from elsewhere, or the active map has changed since.
			Pane pane = FrameworkApplication.Panes.OfType<IMapPane>()
			                                .Where(mapPane => mapPane.MapView == mapView)
			                                .OfType<Pane>()
			                                .FirstOrDefault();

			_msg.VerboseDebug(() => $"Keyboard back to the map pane {pane?.Caption}");

			pane?.Activate();
		}
		catch (Exception ex)
		{
			// Runs from the dispatcher, so nothing may escape.
			_msg.Warn("Could not give the keyboard back from the pane", ex);
		}
	}

	/// <summary>
	/// Whether the element is used with the keyboard: a text box.
	/// </summary>
	private static bool KeepsKeyboard([CanBeNull] DependencyObject element)
	{
		return element is TextBoxBase or PasswordBox;
	}

	/// <summary>
	/// The map pane that contains the element, found by the data context of its ancestors.
	/// </summary>
	[CanBeNull]
	private static IMapPane FindMapPane([CanBeNull] DependencyObject element)
	{
		for (DependencyObject current = element; current != null; current = Parent(current))
		{
			if (current is FrameworkElement { DataContext: IMapPane mapPane })
			{
				return mapPane;
			}
		}

		return null;
	}

	private bool IsInside([CanBeNull] DependencyObject element)
	{
		for (DependencyObject current = element; current != null; current = Parent(current))
		{
			if (ReferenceEquals(current, _content))
			{
				return true;
			}
		}

		return false;
	}

	[CanBeNull]
	private static T FindAncestor<T>([CanBeNull] DependencyObject element)
		where T : DependencyObject
	{
		for (DependencyObject current = element; current != null; current = Parent(current))
		{
			if (current is T match)
			{
				return match;
			}
		}

		return null;
	}

	[CanBeNull]
	private static ComboBox FindComboBox([NotNull] DependencyObject element)
	{
		return element as ComboBox ??
		       ItemsControl.ItemsControlFromItemContainer(element) as ComboBox;
	}

	[CanBeNull]
	private static DependencyObject Parent([NotNull] DependencyObject element)
	{
		// The logical parent leads out of a popup, e.g. a combo box's drop-down.
		DependencyObject parent = element is Visual or System.Windows.Media.Media3D.Visual3D
			                          ? VisualTreeHelper.GetParent(element)
			                          : null;

		return parent ?? LogicalTreeHelper.GetParent(element);
	}

	private static bool IsMouseButtonDown()
	{
		return Mouse.LeftButton == MouseButtonState.Pressed ||
		       Mouse.RightButton == MouseButtonState.Pressed ||
		       Mouse.MiddleButton == MouseButtonState.Pressed;
	}
}
