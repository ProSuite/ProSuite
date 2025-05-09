using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.Commons.AGP.PickerUI;

public interface IPickerViewModel : IDisposable
{
	ICommand FlashItemCommand { get; }
	ICommand SelectionChangedCommand { get; }
	ICommand DeactivatedCommand { get; }
	ICommand PressSpaceCommand { get; }
	ICommand PressEscapeCommand { get; }

	/// <summary>
	/// The awaitable task that provides the result when the dialog is closed.
	/// True means a selection has been made, false means nothing was picked.
	/// </summary>
	Task<IPickableItem> Task { get; }

	ObservableCollection<IPickableItem> Items { get; set; }
	IPickableItem SelectedItem { get; set; }
}
