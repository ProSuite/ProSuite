using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.PickerUI;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Env;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.AGP.Picker;
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
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	// The space that the picker window leaves free on its monitor, in device independent
	// pixels, so that it does not touch both the upper and the lower edge of the monitor.
	private const double _monitorMargin = 40;

	private readonly IPickerPrecedence _precedence;

	public PickerService(IPickerPrecedence precedence)
	{
		_precedence = precedence ?? throw new ArgumentNullException(nameof(precedence));
	}

	public Task<IPickableItem> Pick(
		IEnumerable<IPickableItem> items,
		IPickerViewModel viewModel)
	{
		viewModel.Items = new ObservableCollection<IPickableItem>(_precedence.Order(items));

		return ShowPickerControlAsync(viewModel, _precedence.PickerLocation);
	}

	private async Task<IPickableItem> ShowPickerControlAsync(
		IPickerViewModel vm, Point location)
	{
		_msg.DebugFormat("Showing picker at screen location {0}, position preference: {1}",
		                 location, _precedence.PositionPreference);

		WindowPositioner positioner = GetWindowPositioner(_precedence.PositionPreference);

		IPickableItem pickable = null;

		Dispatcher dispatcher = Assert.NotNull(Application.Current?.Dispatcher);

		return await dispatcher.Invoke(async () =>
		{
			using var window = new PickerWindow(vm);

			SetWindowLocation(window, location);

			RestrictToMonitor(window, location, moveIntoMonitor: positioner == null);

			positioner?.SetWindow(window, location);

			await UIEnvironment.WithReleasedCursorAsync(async () =>
			{
				Window activeWindow = Application.Current.Windows
				                                 .OfType<Window>()
				                                 .FirstOrDefault(w => w.IsActive);

				window.Show();
				pickable = await window.Task;

				// Closing the window on mouse-up does not always work. Ensure it here.
				window.Close();

				// Important: This seems to work for both fixed// seems to work!
				activeWindow?.Activate();
				activeWindow?.Focus();
			});

			return pickable;
		});
	}

	/// <remarks>Must call on main (UI) thread</remarks>
	private static void SetWindowLocation(Window window, Point location)
	{
		Window ownerWindow = Assert.NotNull(Application.Current.MainWindow);

		// Important: setting the owner results in incorrect re-'activation' of the stereo map
		// in a way that it does not receive keyboard input. Explicitly Activate() and Focus()
		// seems to work!
		//window.Owner = ownerWindow;

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

	/// <summary>
	/// Limits the height of the window to the monitor at the location, so that a longer
	/// list gets a scroll bar. Optionally the window is also moved up or to the left if it
	/// extends beyond the lower or the right edge of the monitor.
	/// </summary>
	/// <remarks>Must call on main (UI) thread</remarks>
	private static void RestrictToMonitor(Window window, Point location, bool moveIntoMonitor)
	{
		if (LocationUnknown(location))
		{
			return;
		}

		Window ownerWindow = Assert.NotNull(Application.Current.MainWindow);

		Rect monitor = DisplayUtils.ToDeviceIndependentPixels(
			WindowPositioner.GetClosestMonitor(location), ownerWindow);

		window.MaxHeight = monitor.Height - _monitorMargin;

		_msg.DebugFormat("Monitor of the picker (device independent pixels): {0}", monitor);

		if (! moveIntoMonitor)
		{
			return;
		}

		Point desiredTopLeft = DisplayUtils.ToDeviceIndependentPixels(location, ownerWindow);

		window.SizeChanged += (_, _) =>
		{
			Point topLeft = GetTopLeftOnMonitor(desiredTopLeft, window.RenderSize, monitor);

			window.Left = topLeft.X;
			window.Top = topLeft.Y;

			_msg.DebugFormat(
				"Picker window at {0} (device independent pixels). Desired location: {1}, window size: {2}",
				topLeft, desiredTopLeft, window.RenderSize);
		};
	}

	/// <summary>
	/// Gets the location of the upper left corner of a window that shall appear at the
	/// desired location. The window is only moved up or to the left if it would extend
	/// beyond the lower or the right edge of the monitor.
	/// </summary>
	/// <param name="desiredTopLeft">The desired location of the upper left corner</param>
	/// <param name="windowSize">The size of the window</param>
	/// <param name="monitor">The working area of the monitor</param>
	/// <returns></returns>
	/// <remarks>All arguments must be in the same unit.</remarks>
	public static Point GetTopLeftOnMonitor(Point desiredTopLeft, Size windowSize, Rect monitor)
	{
		double lowestBottom = monitor.Bottom - _monitorMargin / 2;

		double top = Math.Min(desiredTopLeft.Y, lowestBottom - windowSize.Height);
		double left = Math.Min(desiredTopLeft.X, monitor.Right - windowSize.Width);

		return new Point(Math.Max(left, monitor.Left), Math.Max(top, monitor.Top));
	}

	private static bool LocationUnknown(Point location)
	{
		return double.IsNaN(location.X) || double.IsNaN(location.Y);
	}

	private static WindowPositioner GetWindowPositioner(
		PickerPositionPreference positionPreference)
	{
		if (positionPreference == PickerPositionPreference.MouseLocation)
		{
			return null;
		}

		WindowPositioner.PreferredPlacement preferredPlacement = positionPreference switch
		{
			PickerPositionPreference.MouseLocationMapOptimized =>
				WindowPositioner.PreferredPlacement.MapView,
			PickerPositionPreference.MouseLocationMainWindowOptimized =>
				WindowPositioner.PreferredPlacement.MainWindow,
			_ => throw new ArgumentOutOfRangeException(nameof(positionPreference))
		};

		return new WindowPositioner(preferredPlacement,
		                            WindowPositioner.EvaluationMethod.DistanceToRect);
	}
}
