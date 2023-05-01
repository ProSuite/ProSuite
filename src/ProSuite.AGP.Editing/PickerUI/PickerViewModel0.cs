using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Microsoft.Xaml.Behaviors.Core;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Editing.PickerUI
{
	public class PickerViewModel0 : PropertyChangedBase, IDisposable
	{
		private readonly Geometry _selectionGeometry;
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Latch _latch = new Latch();
		private readonly TaskCompletionSource<IPickableItem> _taskCompletionSource;

		private readonly CIMLineSymbol _lineSymbol;
		private readonly CIMPointSymbol _pointSymbol;
		private readonly CIMPolygonSymbol _polygonSymbol;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();
		[CanBeNull] private IDisposable _selectionGeometryOverlay;

		[CanBeNull] private IPickableItem _selectedItem;

		public PickerViewModel0(IEnumerable<IPickableItem> pickingCandidates)
		{
			_taskCompletionSource = new TaskCompletionSource<IPickableItem>();

			FlashItemCommand = new RelayCommand<IPickableItem>(FlashItem);
			SelectionChangedCommand = new RelayCommand<ICloseable>(OnSelectionChanged);
			DeactivatedCommand = new RelayCommand<ICloseable>(OnWindowDeactivated);
			PressEscapeCommand = new RelayCommand<ICloseable>(OnPressEscape);
			PressSpaceCommand = new ActionCommand(OnPressSpace);

			Items = new ObservableCollection<IPickableItem>(pickingCandidates);

			CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);

			_lineSymbol =
				SymbolFactory.Instance.ConstructLineSymbol(magenta, 4);

			CIMStroke outline =
				SymbolFactory.Instance.ConstructStroke(
					magenta, 4, SimpleLineStyle.Solid);

			_polygonSymbol =
				SymbolFactory.Instance.ConstructPolygonSymbol(
					magenta, SimpleFillStyle.Null, outline);

			_pointSymbol = SymbolUtils.CreatePointSymbol(magenta, 6);
		}

		public PickerViewModel0(IEnumerable<IPickableItem> pickingCandidates,
		                        Geometry selectionGeometry) : this(pickingCandidates)
		{
			_selectionGeometry = selectionGeometry;
		}

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
		public ObservableCollection<IPickableItem> Items { get; }

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

		private void FlashItem(IPickableItem candidate)
		{
			ViewUtils.Try(() =>
			{
				Geometry geometry = candidate.Geometry;

				if (geometry == null)
				{
					return;
				}

				DisposeOverlays();

				Geometry flashGeometry = null;
				CIMSymbol symbol = null;

				switch (geometry.GeometryType)
				{
					case GeometryType.Point:
						flashGeometry = geometry;
						symbol = _pointSymbol;
						break;
					case GeometryType.Polyline:
						flashGeometry = geometry;
						symbol = _lineSymbol;
						break;
					case GeometryType.Polygon:
						flashGeometry = GetPolygonGeometry(geometry);
						symbol = _polygonSymbol;
						break;
					case GeometryType.Unknown:
					case GeometryType.Envelope:
					case GeometryType.Multipoint:
					case GeometryType.Multipatch:
					case GeometryType.GeometryBag:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				QueuedTask.Run(() => { AddOverlay(flashGeometry, symbol); });
			}, _msg);
		}

		private static Geometry GetPolygonGeometry(Geometry geometry)
		{
			Envelope clipExtent = MapView.Active?.Extent;

			if (clipExtent == null)
			{
				return geometry;
			}

			double mapRotation = MapView.Active.NotNullCallback(mv => mv.Camera.Heading);

			return GeometryUtils.GetClippedPolygon((Polygon) geometry, clipExtent, mapRotation);
		}

		private void AddOverlay(Geometry geometry, CIMSymbol symbol)
		{
			MapView.Active.NotNullCallback(mv =>
			{
				_overlays.Add(
					mv.AddOverlay(
						geometry, symbol.MakeSymbolReference()));
			});
		}

		private void DisposeOverlays()
		{
			foreach (IDisposable overlay in _overlays)
			{
				overlay.Dispose();
			}

			_overlays.Clear();
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
			}, _msg);
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
			}, _msg);
		}

		private void OnPressEscape(ICloseable window)
		{
			OnWindowDeactivated(window);
		}

		private void OnPressSpace()
		{
			QueuedTask.Run(() =>
			{
				_selectionGeometryOverlay = MapView.Active.NotNullCallback(
					mv => mv.AddOverlay(_selectionGeometry, _polygonSymbol.MakeSymbolReference()));
			});
		}

		public void Dispose()
		{
			// Don't set to null here, throws an exception:
			// An attempt was made to transition a task to a final state
			// when it had already completed.
			//SelectedItem = null;
			
			_selectionGeometryOverlay?.Dispose();
			DisposeOverlays();
		}
	}
}
