using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Editing.PickerUI
{
	public class PickerViewModel : PropertyChangedBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private readonly CIMLineSymbol _highlightLineSymbol;

		private readonly CIMPointSymbol _highlightPointSymbol;
		private readonly CIMPolygonSymbol _highlightPolygonSymbol;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();
		private readonly TaskCompletionSource<bool> _resultTaskCompletionSource;

		private bool? _dialogResult;

		private bool _isClosing;

		private bool _isSingleMode;

		private ObservableCollection<IPickableItem> _pickableItems;

		private IPickableItem _selectedPickableItem;

		public PickerViewModel() : this(new List<IPickableItem>(), true) { }

		public PickerViewModel(IList<IPickableItem> pickingCandidates,
		                       bool isSingleMode)
		{
			FlashItemCommand = new RelayCommand<IPickableItem>(FlashItem);

			CloseCommand = new RelayCommand<PickerWindow>(Close);

			PickableItems =
				new ObservableCollection<IPickableItem>(pickingCandidates);

			_isSingleMode = isSingleMode;

			CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);

			CIMStroke outline =
				SymbolFactory.Instance.ConstructStroke(
					magenta, 4, SimpleLineStyle.Solid);

			_highlightLineSymbol =
				SymbolFactory.Instance.ConstructLineSymbol(magenta, 4);

			_highlightPolygonSymbol =
				SymbolFactory.Instance.ConstructPolygonSymbol(
					magenta, SimpleFillStyle.Null, outline);

			_highlightPointSymbol = SymbolUtils.CreatePointSymbol(magenta, 6);

			_resultTaskCompletionSource = new TaskCompletionSource<bool>();
		}

		public ICommand FlashItemCommand { get; }

		public ICommand CloseCommand { get; }

		[CanBeNull]
		public IPickableItem SelectedPickableItem
		{
			get => _selectedPickableItem;
			set
			{
				SetProperty(ref _selectedPickableItem, value, () => SelectedPickableItem);

				try
				{
					if (value == null)
					{
						return;
					}

					DialogResult = true;

					if (IsSingleMode)
					{
						CloseAction();
					}
				}
				catch (Exception e)
				{
					_msg.Error("Error setting SelectedPickableItem", e);
				}
			}
		}

		public bool IsSingleMode
		{
			get => _isSingleMode;
			set { SetProperty(ref _isSingleMode, value, () => IsSingleMode); }
		}

		public ObservableCollection<IPickableItem> PickableItems
		{
			get => _pickableItems;
			private set { SetProperty(ref _pickableItems, value, () => PickableItems); }
		}

		public IEnumerable<IPickableItem> SelectedItems
		{
			get { return _pickableItems.Where(item => item.Selected); }
		}

		/// <summary>
		/// The awaitable task that provides the result when the dialog is closed.
		/// True means a selection has been made, false means nothing was picked.
		/// </summary>
		public Task<bool> ResultTask => _resultTaskCompletionSource.Task;

		public Action CloseAction { get; set; }

		private bool? DialogResult
		{
			get => _dialogResult;
			set
			{
				SetProperty(ref _dialogResult, value, () => DialogResult);

				bool taskResult = _dialogResult ?? false;

				_resultTaskCompletionSource.SetResult(taskResult);
			}
		}

		private void AddOverlay(Geometry geometry,
		                        CIMSymbol symbol)
		{
			IDisposable addedOverlay =
				MapView.Active.AddOverlay(geometry, symbol.MakeSymbolReference());

			_overlays.Add(addedOverlay);
		}

		public void DisposeOverlays()
		{
			if (_overlays.Any())
			{
				_overlays.ForEach(overlay => overlay.Dispose());
				_overlays.Clear();
			}
		}

		private void Close([CanBeNull] PickerWindow window)
		{
			try
			{
				if (_isClosing)
				{
					return;
				}

				if (window != null)
				{
					window.Close();
				}
				else
				{
					CloseAction();
				}
			}
			catch (Exception exception)
			{
				_msg.Error("Error closing picker window", exception);
			}
		}

		private void FlashItem(IPickableItem candidate)
		{
			try
			{
				Geometry flashGeometry = candidate.Geometry;

				if (flashGeometry == null)
				{
					return;
				}

				DisposeOverlays();

				CIMSymbol symbol = _highlightPointSymbol;

				if (flashGeometry is Polygon)
				{
					symbol = _highlightPolygonSymbol;

					Envelope clipExtent = MapView.Active?.Extent;

					if (clipExtent != null)
					{
						double mapRotation = MapView.Active.Camera.Heading;

						flashGeometry =
							GeometryUtils.GetClippedPolygon((Polygon) flashGeometry, clipExtent,
							                                mapRotation);
					}
				}

				if (flashGeometry is Polyline)
				{
					symbol = _highlightLineSymbol;
				}

				QueuedTask.Run(() => { AddOverlay(flashGeometry, symbol); });
			}
			catch (Exception exception)
			{
				_msg.Warn("Error flashing pickable candidate", exception);
			}
		}

		public void OnWindowDeactivated(object sender, EventArgs e)
		{
			_msg.DebugFormat("PickerWindow_Deactivated. Already closing: {0}", _isClosing);

			Close((PickerWindow) sender);
		}

		public void OnWindowClosing(object sender, CancelEventArgs e)
		{
			_msg.DebugFormat("PickerWindow_Closing");

			DisposeOverlays();

			// Ensure The task completes and the main UI thread continues
			if (DialogResult == null)
			{
				DialogResult = false;
			}

			_isClosing = true;
		}

		public void OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			// NOTE: This event is not fired if it is a normal WPF window (the active tool
			// receives ESC). It only works for ProWindows!
			if (e.Key == Key.Escape)
			{
				Close((PickerWindow) sender);
			}
		}
	}
}
