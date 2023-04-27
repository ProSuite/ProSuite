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
	public class PickerService : IPickerService
	{
		public Func<Task<T>> PickSingle<T>(IEnumerable<IPickableItem> items,
		                                   Point pickerLocation,
		                                   IPickerPrecedence precedence)
			where T : class, IPickableItem
		{
			// todo daro remove toList()
			IEnumerable<IPickableItem> orderedItems = precedence.Order(items).ToList();

			IPickableItem bestPick = precedence.PickBest(orderedItems);

			return async () =>
			{
				await Task.FromResult(typeof(T));

				return (T) bestPick;
			};

			var viewModel = new PickerViewModel0(orderedItems);

			return async () => await ShowPickerControlAsync<T>(viewModel, pickerLocation);
		}

		private static async Task<T> ShowPickerControlAsync<T>(PickerViewModel0 vm, Point location)
			where T : class, IPickableItem
		{
			using (var window = new PickerProWindow(vm))
			{
				SetWindowLocation(window, location);

				window.Show();

				IPickableItem pickable = await window.Task;

				return (T) pickable;
			}
		}

		private static void SetWindowLocation(PickerProWindow window, Point location)
		{
			Window ownerWindow = Assert.NotNull(Application.Current.MainWindow);

			window.Owner = ownerWindow;

			if (LocationUnkown(location))
			{
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				return;
			}

			// NOTE: The window's Top/Left coordinates must be set in logical units or DIP (1/96 inch)
			Point dipLocation = DisplayUtils.ToDeviceIndependentPixels(location, ownerWindow);

			window.Left = dipLocation.X;
			window.Top = dipLocation.Y;
		}

		private static bool LocationUnkown(Point location)
		{
			return double.IsNaN(location.X) || double.IsNaN(location.Y);
		}
	}
}
