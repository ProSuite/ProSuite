using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Microsoft.Xaml.Behaviors.Core;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.PickerUI;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Editing.PickerUI;

public abstract class PickerViewModelBase<T> : NotifyPropertyChangedBase, IPickerViewModel where T : IPickableItem
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private FlashService _flashService;
	private readonly Geometry _selectionGeometry;
	private readonly Latch _latch = new();
	private readonly TaskCompletionSource<IPickableItem> _taskCompletionSource;

	[CanBeNull] private IDisposable _selectionGeometryOverlay;
	[CanBeNull] private IPickableItem _selectedItem;
	[CanBeNull] private ObservableCollection<IPickableItem> _items;

	protected PickerViewModelBase()
	{
		_taskCompletionSource = new TaskCompletionSource<IPickableItem>();

		FlashItemCommand = new RelayCommand<T>(FlashItem);
		SelectionChangedCommand = new RelayCommand<ICloseable>(OnSelectionChanged);
		DeactivatedCommand = new RelayCommand<ICloseable>(OnWindowDeactivated);
		PressEscapeCommand = new RelayCommand<ICloseable>(OnPressEscape);
		PressSpaceCommand = new ActionCommand(OnPressSpace);
	}

	protected PickerViewModelBase(Geometry selectionGeometry) : this()
	{
		_selectionGeometry = selectionGeometry;
	}

	public FlashService FlashService
	{
		get
		{
			if (_flashService == null)
			{
				_flashService = CreateFlashService();
			}
			return _flashService;
		}
	}

	protected abstract FlashService CreateFlashService();

	public ICommand FlashItemCommand { get; }
	public ICommand SelectionChangedCommand { get; }
	public ICommand DeactivatedCommand { get; }
	public ICommand PressSpaceCommand { get; }
	public ICommand PressEscapeCommand { get; }

	/// <summary>
	/// The awaitable task that provides the result when the dialog is closed.
	/// True means a selection has been made, false means nothing was picked.
	/// </summary>
	public Task<IPickableItem> Task => _taskCompletionSource.Task;

	[NotNull]
	public ObservableCollection<IPickableItem> Items
	{
		get => Assert.NotNull(_items);
		set
		{
			_items = value;
			SetProperty(ref _items, value);
		}
	}

	[CanBeNull]
	public IPickableItem SelectedItem
	{
		get => _selectedItem;
		set
		{
			SetProperty(ref _selectedItem, value);

			_msg.Debug($"Picked {_selectedItem}");

			try
			{
				_taskCompletionSource.SetResult(_selectedItem);
			}
			catch (Exception e)
			{
				_msg.Debug("Error setting selected item", e);
			}
		}
	}

	private void FlashItem(T candidate)
	{
		FlashItemCore(candidate);
	}

	protected virtual void FlashItemCore(T item)
	{
		ViewUtils.Try(() => FlashService.Flash(item.Geometry), _msg, true);
	}

	private void OnSelectionChanged(ICloseable window)
	{
		ViewUtils.Try(() =>
		{
			if (_latch.IsLatched) return;

			_latch.RunInsideLatch(() =>
			{
				if (_selectedItem == null)
				{
					return;
				}

				window?.Close();
			});
		}, _msg, true);
	}

	private void OnWindowDeactivated(ICloseable window)
	{
		ViewUtils.Try(() =>
		{
			if (_latch.IsLatched) return;

			_latch.RunInsideLatch(() =>
			{
				window?.Close();

				// IMPORTANT set selected item otherwise
				// task never completes resulting in deadlock
				SelectedItem = null;

				Dispose();
			});
		}, _msg, true);
	}

	private void OnPressEscape(ICloseable window)
	{
		OnWindowDeactivated(window);
	}

	private void OnPressSpace()
	{
		QueuedTask.Run(() =>
		{
			CIMPolygonSymbol polygonSymbol = FlashService.PolygonSymbol;

			if (_selectionGeometryOverlay == null)
			{
				_selectionGeometryOverlay =
					MapView.Active.AddOverlay(_selectionGeometry,
					                          polygonSymbol.MakeSymbolReference());
			}
			else
			{
				MapView.Active.UpdateOverlay(_selectionGeometryOverlay, _selectionGeometry,
				                             polygonSymbol.MakeSymbolReference());
			}
		});
	}

	public void Dispose()
	{
		// Don't set to null here, throws an exception:
		// An attempt was made to transition a task to a final state
		// when it had already completed.
		//SelectedItem = null;

		_selectionGeometryOverlay?.Dispose();
		FlashService?.Dispose();
	}
}
