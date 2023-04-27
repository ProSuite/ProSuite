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
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Editing.PickerUI
{
	public class PickerViewModel0 : PropertyChangedBase, IDisposable
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Latch _latch = new Latch();
		private readonly TaskCompletionSource<IPickableItem> _taskCompletionSource;

		private readonly CIMLineSymbol _highlightLineSymbol;
		private readonly CIMPointSymbol _highlightPointSymbol;
		private readonly CIMPolygonSymbol _highlightPolygonSymbol;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();

		[CanBeNull] private IPickableItem _selectedItem;

		public PickerViewModel0(IEnumerable<IPickableItem> pickingCandidates)
		{
			_taskCompletionSource = new TaskCompletionSource<IPickableItem>();

			FlashItemCommand = new RelayCommand<IPickableItem>(FlashItem);
			SelectionChangedCommand = new RelayCommand<ICloseable>(OnSelectionChanged);
			DeactivatedCommand = new RelayCommand<ICloseable>(OnWindowDeactivated);

			Items = new ObservableCollection<IPickableItem>(pickingCandidates);

			CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);

			_highlightLineSymbol =
				SymbolFactory.Instance.ConstructLineSymbol(magenta, 4);

			CIMStroke outline =
				SymbolFactory.Instance.ConstructStroke(
					magenta, 4, SimpleLineStyle.Solid);

			_highlightPolygonSymbol =
				SymbolFactory.Instance.ConstructPolygonSymbol(
					magenta, SimpleFillStyle.Null, outline);

			_highlightPointSymbol = SymbolUtils.CreatePointSymbol(magenta, 6);
		}

		public ICommand FlashItemCommand { get; }
		public ICommand SelectionChangedCommand { get; }
		public ICommand DeactivatedCommand { get; }

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

				_taskCompletionSource.SetResult(_selectedItem);
			}
		}

		private void FlashItem(IPickableItem candidate)
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
					symbol = _highlightPointSymbol;
					break;
				case GeometryType.Polyline:
					flashGeometry = geometry;
					symbol = _highlightLineSymbol;
					break;
				case GeometryType.Polygon:
					flashGeometry = GetPolygonGeometry(geometry);
					symbol = _highlightPolygonSymbol;
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
				_overlays.Add(mv.AddOverlay(geometry, symbol.MakeSymbolReference()));
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
			if (_latch.IsLatched) return;

			_latch.RunInsideLatch(() =>
			{
				if (_selectedItem == null)
				{
					return;
				}

				window?.Close();
			});
		}

		private void OnWindowDeactivated(ICloseable window)
		{
			if (_latch.IsLatched) return;

			_latch.RunInsideLatch(() =>
			{
				// IMPORTANT set selected item otherwise
				// task never completes resulting in deadlock
				SelectedItem = null;

				window?.Close();

				DisposeOverlays();
			});
		}

		public void Dispose()
		{
			DisposeOverlays();
		}
	}
}
