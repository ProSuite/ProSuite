using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ProSuite.AGP.Editing.PickerUI;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Editing.Picker
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
		public Task<T> Pick<T>(List<IPickableItem> items,
		                       IPickerPrecedence precedence) where T : class, IPickableItem
		{
			if (items.Count == 1)
			{
				return Task.FromResult(precedence.PickBest<T>(items));
			}

			return PickSingle<T>(items, precedence);
		}

		// todo daro rename?
		public Task<T> PickSingle<T>(IEnumerable<IPickableItem> items,
		                             IPickerPrecedence precedence)
			where T : class, IPickableItem
		{
			// todo daro refactor. maybe add new dedicated method
			PickerViewModel viewModel;

			if (typeof(T) == typeof(IPickableFeatureItem))
			{
				var candidates = precedence.Order(items).OfType<IPickableFeatureItem>();

				viewModel = new PickerViewModel(candidates, precedence.SelectionGeometry);
			}
			else if (typeof(T) == typeof(IPickableFeatureClassItem))
			{
				viewModel = new PickerViewModel(items, precedence.SelectionGeometry);
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}

			return ShowPickerControlAsync<T>(viewModel, precedence.PickerLocation);
		}

		private static async Task<T> ShowPickerControlAsync<T>(PickerViewModel vm, Point location)
			where T : class, IPickableItem
		{
			var dispatcher = Application.Current.Dispatcher;

			return await dispatcher.Invoke(async () =>
			{
				using var window = new PickerWindow(vm);

				SetWindowLocation(window, location);

				window.Show();

				IPickableItem pickable = await window.Task;

				return (T) pickable;
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
