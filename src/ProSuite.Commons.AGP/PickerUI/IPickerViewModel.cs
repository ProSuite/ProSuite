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
	/// Command that confirms the currently highlighted item as the selection,
	/// completing the picker task and closing the window.
	/// Bound to the Enter key and mouse-click in the picker window.
	/// </summary>
	ICommand ConfirmSelectionCommand { get; }

	/// <summary>
	/// The awaitable task that provides the result when the dialog is closed.
	/// True means a selection has been made, false means nothing was picked.
	/// </summary>
	Task<IPickableItem> Task { get; }

	ObservableCollection<IPickableItem> Items { get; set; }

	/// <summary>
	/// The item currently highlighted by keyboard navigation or hover.
	/// Bound two-way to ListBox.SelectedItem; does NOT close the window.
	/// </summary>
	IPickableItem HighlightedItem { get; set; }

	/// <summary>
	/// The confirmed selection. Setting this completes the picker task.
	/// Should only be set via ConfirmSelectionCommand.
	/// </summary>
	IPickableItem SelectedItem { get; set; }
}
