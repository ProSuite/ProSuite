using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.Commons.AGP.PickerUI;

/// <summary>
/// Behavior that handles KeyUp events for the picker ListBox to support navigation and selection.
/// </summary>
public class ListBoxKeyUpBehavior : Behavior<ListBox>
{
	public static readonly DependencyProperty FlashItemCommandProperty =
		DependencyProperty.Register(
			nameof(FlashItemCommand),
			typeof(ICommand),
			typeof(ListBoxKeyUpBehavior),
			new PropertyMetadata(null));

	public static readonly DependencyProperty CloseWindowActionProperty =
		DependencyProperty.Register(
			nameof(CloseWindowAction),
			typeof(ICommand),
			typeof(ListBoxKeyUpBehavior),
			new PropertyMetadata(null));

	/// <summary>
	/// Command to flash the selected item.
	/// </summary>
	public ICommand FlashItemCommand
	{
		get => (ICommand) GetValue(FlashItemCommandProperty);
		set => SetValue(FlashItemCommandProperty, value);
	}

	/// <summary>
	/// Command to close the window (e.g., on Enter key).
	/// </summary>
	public ICommand CloseWindowAction
	{
		get => (ICommand) GetValue(CloseWindowActionProperty);
		set => SetValue(CloseWindowActionProperty, value);
	}

	protected override void OnAttached()
	{
		base.OnAttached();
		AssociatedObject.PreviewKeyDown += OnListBoxPreviewKeyDown;
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();
		AssociatedObject.PreviewKeyDown -= OnListBoxPreviewKeyDown;
	}

	private void OnListBoxPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Enter)
		{
			return;
		}

		var listBox = AssociatedObject;
		if (listBox == null)
		{
			return;
		}

		int index = listBox.SelectedIndex;
		int count = listBox.Items.Count;

		if (e.Key == Key.Up && count > 0)
		{
			// Wrap-around: Up at the first item jumps to last; otherwise move up one.
			listBox.SelectedIndex = index <= 0 ? count - 1 : index - 1;

			listBox.SelectedIndex = index <= 0 ? 0 : index - 1;
			listBox.ScrollIntoView(listBox.SelectedItem);
			e.Handled = true;
		}
		else if (e.Key == Key.Down && count > 0)
		{
			// Wrap-around: Down at the last item jumps to first; otherwise move down one.
			listBox.SelectedIndex = index >= count - 1 ? 0 : index + 1;

			listBox.SelectedIndex = index >= count - 1 ? count - 1 : index + 1;
			listBox.ScrollIntoView(listBox.SelectedItem);
			e.Handled = true;
		}

		// Flash the item if FlashItemCommand is available
		var selected = listBox.SelectedItem as IPickableItem;
		var cmd = FlashItemCommand;
		if (cmd != null && cmd.CanExecute(selected))
		{
			cmd.Execute(selected);
		}

		// Close window on Enter key
		if (e.Key == Key.Enter)
		{
			var closeCmd = CloseWindowAction;
			if (closeCmd != null)
			{
				// Find the window containing this ListBox
				var window = Window.GetWindow(listBox);
				if (window != null && closeCmd.CanExecute(window))
				{
					closeCmd.Execute(window);
				}
			}

			e.Handled = true;
		}
	}
}
