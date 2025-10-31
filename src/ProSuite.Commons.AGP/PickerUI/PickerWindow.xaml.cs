using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.Commons.AGP.PickerUI
{
	public partial class PickerWindow : ProWindow, IDisposable, ICloseable
	{
		private readonly IPickerViewModel _viewModel;

		public PickerWindow(IPickerViewModel viewModel)
		{
			InitializeComponent();

			_viewModel = viewModel;
			DataContext = viewModel;

			Loaded += PickerWindow_Loaded;
		}

		public Task<IPickableItem> Task => _viewModel.Task;

		public void Dispose()
		{
			_viewModel.Dispose();
		}

		private void PickerWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// Make the list the focused element in this focus scope and give keyboard focus
			FocusManager.SetFocusedElement(this, ItemListBox);
			System.Windows.Input.Keyboard.Focus(ItemListBox);
		}

		private void ListBoxItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// optional: ensure left button
			if (e.ChangedButton != MouseButton.Left)
			{
				return;
			}

			// sender should be the ListBoxItem; selection binding already updates SelectedItem
			Close();
			e.Handled = true;
		}
	}
}
