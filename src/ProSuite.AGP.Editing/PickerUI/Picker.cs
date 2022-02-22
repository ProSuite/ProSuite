using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Editing.PickerUI
{
	public class Picker : IPicker
	{
		private readonly IList<IPickableItem> _candidateList;
		private readonly Point _pickerScreenLocation = new Point(double.NaN, double.NaN);
		private PickerViewModel _viewModel;

		public Picker([NotNull] List<IPickableItem> candidateList)
		{
			_candidateList = candidateList;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Picker" /> class.
		/// </summary>
		/// <param name="candidateList">The list of pickable items</param>
		/// <param name="pickerScreenCoordinates">
		/// The location  of the picker dialog's top left corner in screen coordinates.
		/// </param>
		public Picker([NotNull] IList<IPickableItem> candidateList,
		              Point pickerScreenCoordinates)
		{
			_candidateList = candidateList;
			_pickerScreenLocation = pickerScreenCoordinates;
		}

		[ItemCanBeNull]
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

			bool? dialogResult = await ShowPickerControl(_viewModel);

			return dialogResult == true ? _viewModel.SelectedPickableItem : null;
		}

		[NotNull]
		public async Task<IList<IPickableItem>> PickMany()
		{
			if (_candidateList.Count == 0)
			{
				return new List<IPickableItem>(0);
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

			RunOnUIThread(() => { ShowPickerControl(_viewModel); });

			_viewModel.DisposeOverlays();

			return _viewModel.SelectedItems.ToList();
		}

		private async Task<bool?> ShowPickerControl(PickerViewModel vm)
		{
			var window = new PickerWindow(vm);

			SetWindowLocation(window);

			window.Show();

			return await vm.ResultTask;
		}

		private void SetWindowLocation(Window window)
		{
			Window ownerWindow = Assert.NotNull(Application.Current.MainWindow);

			window.Owner = ownerWindow;

			if (IsUnknownLocation(_pickerScreenLocation))
			{
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				return;
			}

			// NOTE: The window's Top/Left coordinates must be set in logical units or DIP (1/96 inch)
			Point dipLocation =
				DisplayUtils.ToDeviceIndependentPixels(_pickerScreenLocation, ownerWindow);

			window.Left = dipLocation.X;
			window.Top = dipLocation.Y;
		}

		private static void RunOnUIThread(Action action)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				action(); //No invoke needed
			}
			else
				//We are not on the UI
			{
				Application.Current.Dispatcher.BeginInvoke(action);
			}
		}

		private static bool IsUnknownLocation(Point location)
		{
			return double.IsNaN(location.X) || double.IsNaN(location.Y);
		}
	}
}
