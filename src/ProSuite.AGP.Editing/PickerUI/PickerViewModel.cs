using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Framework.Win32;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.Logging;
using Geometry = System.Windows.Media.Geometry;
using Polygon = ArcGIS.Core.Geometry.Polygon;
using Polyline = ArcGIS.Core.Geometry.Polyline;

namespace ProSuite.AGP.Editing.PickerUI
{
	public class PickerViewModel : PropertyChangedBase
	{
		private readonly CIMLineSymbol _highlightLineSymbol;
		private readonly CIMPolygonSymbol _highlightPolygonSymbol;
		private readonly CIMPointSymbol _highlightPointSymbol;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public PickerViewModel(List<IPickableItem> pickingCandidates,
		                       bool isSingleMode)
		{
			FlashItemCmd = new RelayCommand(param =>
				                                FlashItem(param), () => true, false,
			                                true);

			CloseCommand = new RelayCommand(() =>
				                                Close(), () => true, false, true);

			PickableItems =
				new ObservableCollection<IPickableItem>(pickingCandidates);

			_isSingleMode = isSingleMode;

			CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);

			CIMStroke outline =
				SymbolFactory.Instance.ConstructStroke(
					magenta, 2, SimpleLineStyle.Solid);

			_highlightLineSymbol =
				SymbolFactory.Instance.ConstructLineSymbol(magenta, 2);

			_highlightPolygonSymbol =
				SymbolFactory.Instance.ConstructPolygonSymbol(
					magenta, SimpleFillStyle.Null, outline);

			_highlightPointSymbol =
				SymbolFactory.Instance.ConstructPointSymbol(magenta, 6);
		}

		private bool? _dialogResult;

		private ObservableCollection<IPickableItem> _pickableItems;

		protected IPickableItem _selectedItem;

		protected bool _isSingleMode;

		private List<IPickableItem> _selectedItems;

		protected readonly List<IDisposable> _overlays = new List<IDisposable>();

		public RelayCommand FlashItemCmd { get; internal set; }
		public RelayCommand CloseCommand { get; set; }

		public IPickableItem SelectedItem
		{
			get => _selectedItem;
			set
			{
				SetProperty(ref _selectedItem, value, () => SelectedItem);
				DisposeOverlays();
				DialogResult = true;
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
			set { SetProperty(ref _pickableItems, value, () => PickableItems); }
		}

		public bool? DialogResult
		{
			get => _dialogResult;
			set { SetProperty(ref _dialogResult, value, () => DialogResult); }
		}

		public List<IPickableItem> SelectedItems
		{
			get { return _pickableItems.Where(item => item.IsSelected).ToList(); }
		}

		private void AddOverlay(ArcGIS.Core.Geometry.Geometry geometry,
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
				_overlays.ForEach((overlay) => overlay.Dispose());
				_overlays.Clear();
			}
		}

		protected void Close()
		{
			_overlays.Clear();
			DialogResult = true;
		}

		protected void FlashItem(object param)
		{
			var candidate = (IPickableItem) param;
			if (candidate.Geometry == null)
			{
				return;
			}

			DisposeOverlays();

			CIMSymbol symbol = _highlightPointSymbol;

			if (candidate.Geometry is Polygon)
			{
				symbol = _highlightPolygonSymbol;
			}

			if (candidate.Geometry is Polyline)
			{
				symbol = _highlightLineSymbol;
			}

			QueuedTask.Run(() => { AddOverlay(candidate.Geometry, symbol); });
		}
	}
}
