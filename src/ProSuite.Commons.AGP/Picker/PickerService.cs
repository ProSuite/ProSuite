using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.PickerUI;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.AGP.Picker
{
	// TODOs:
	// - Consider tool tip for pickable items with all attributes
	// - Check performance, consider not just clipping but also weeding
	// - Configurable selection tolerance (consider using snapping?)
	// - The highlighting in the map happens after the hovering over the list item
	//   -> About 1-2 pixels of extra tolerance.
	// - All tools: Select by polygon (currently just for  RAEF). Decide on mode vs keep P pressed.
	//              Use sketch output mode Screen (and convert before selection)

	public class PickerService : IPickerService
	{
		private readonly IPickerPrecedence _precedence;

		public PickerService(IPickerPrecedence precedence)
		{
			_precedence = precedence ?? throw new ArgumentNullException(nameof(precedence));
		}

		public Task<IPickableItem> Pick(IEnumerable<IPickableItem> items, IPickerViewModel viewModel)
		{
			viewModel.Items = new ObservableCollection<IPickableItem>(_precedence.Order(items));

			return ShowPickerControlAsync(viewModel, _precedence.PickerLocation);
		}

		private static async Task<IPickableItem> ShowPickerControlAsync(
			IPickerViewModel vm, Point location)
		{
			var dispatcher = Application.Current.Dispatcher;

			List<Geometry> geometries = new() { };
			WindowPositioner positioner =
				new WindowPositioner(geometries, WindowPositioner.PreferredPlacement.MainWindow,
				                     WindowPositioner.EvaluationMethod.DistanceToRect);

			return await dispatcher.Invoke(async () =>
			{
				using var window = new PickerWindow(vm);

				SetWindowLocation(window, location);

				positioner.SetWindow(window, location);

				window.Show();

				IPickableItem pickable = await window.Task;

				return pickable;
			});
		}

		/// <remarks>Must call on main (UI) thread</remarks>
		private static void SetWindowLocation(Window window, Point location)
		{
			Window ownerWindow = Assert.NotNull(Application.Current.MainWindow);

			window.Owner = ownerWindow;

			if (LocationUnknown(location))
			{
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				return;
			}

			// NOTE: The window's Top/Left coordinates must be set in logical units or DIP (1/96 inch)
			Point dipLocation = DisplayUtils.ToDeviceIndependentPixels(location, ownerWindow);

			window.Left = dipLocation.X;
			window.Top = dipLocation.Y;
		}

		private static bool LocationUnknown(Point location)
		{
			return double.IsNaN(location.X) || double.IsNaN(location.Y);
		}
	}
}
