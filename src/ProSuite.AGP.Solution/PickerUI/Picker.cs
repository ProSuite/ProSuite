using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ItemPicker;
using JetBrains.Annotations;
using ProSuite.AGP.Picker;

namespace Clients.AGP.ProSuiteSolution.PickerUI
{
	class Picker : IPicker

	{
		private PickerViewModel _viewModel;
		private readonly List<IPickableItem> _candidateList;
		private Point _windowLocation;
		private const double _unknownLoc = -1.0;

		public Picker([ItemNotNull] [NotNull] List<IPickableItem> candidateList)
		{
			_candidateList = candidateList;
			_windowLocation.X = _unknownLoc;
			_windowLocation.Y = _unknownLoc;
		}

		public Picker([ItemNotNull] [NotNull] List<IPickableItem> candidateList,
		              [NotNull] Point pickerWindowLocation)
		{
			_candidateList = candidateList;
			_windowLocation = pickerWindowLocation;
		}

		[CanBeNull]
		public async Task<IPickableItem> PickSingle()
		{
			if (_candidateList.Count == 0)
			{
				return null;
			}

			if (_candidateList.Count == 1)
			{
				return _candidateList.First();
			}

			await QueuedTask.Run(() =>
			{
				_viewModel =
					new PickerViewModel(_candidateList, true);
			});

			bool dialogresult = ShowPickerControl(_viewModel);

			_viewModel.DisposeOverlays();

			return dialogresult
				       ? _viewModel.SelectedItem
				       : _viewModel.PickableItems.First();
		}

		[CanBeNull]
		public async Task<List<IPickableItem>> PickMany()
		{
			if (_candidateList.Count == 0)
			{
				return null;
			}

			if (_candidateList.Count == 1)
			{
				return _candidateList;
			}

			await QueuedTask.Run(() =>
			{
				_viewModel = new PickerViewModel(_candidateList, false);
			});

			if (_candidateList.Count == 0)
			{
				return new List<IPickableItem>();
			}

			ShowPickerControl(_viewModel);

			_viewModel.DisposeOverlays();

			return _viewModel.SelectedItems.ToList<IPickableItem>();
		}

		private bool ShowPickerControl(PickerViewModel vm)
		{
			PickerWindow window = new PickerWindow(vm);
			ManageWindowLocation(window);
			return (bool) window.ShowDialog();
		}

		private void ManageWindowLocation(Window window)
		{
			Window ownerWindow = FrameworkApplication.Current.MainWindow;
			window.Owner = ownerWindow;
			if (_windowLocation.X == _unknownLoc && _windowLocation.Y == _unknownLoc)
			{
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				return;
			}
			window.Left = _windowLocation.X;
			window.Top = _windowLocation.Y;
		}
	}
}