using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.Callbacks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;

namespace ProSuite.Commons.AGP.Framework;

public static class DockPaneUtils
{
	public static DockPaneManager DockPaneManager => FrameworkApplication.DockPaneManager;

	private static bool _slideOutAutoHidden;

	/// <summary>
	/// Shows a tool's options pane and leaves the keyboard where it is. An auto-hidden pane
	/// only gets its tab back and is not slid out: it would cover the map until the next click
	/// into the map closes it. Within <see cref="SlideOutAutoHidden"/> it is slid out.
	/// </summary>
	public static void ShowToolOptions([NotNull] DockPane pane)
	{
		Assert.ArgumentNotNull(pane, nameof(pane));

		IInputElement focus = System.Windows.Input.Keyboard.FocusedElement;

		// A hidden pane that was auto-hidden comes back slid out.
		pane.Activate(false);

		if (_slideOutAutoHidden || pane.DockState != DockPaneState.AutoHide)
		{
			return;
		}

		// An auto-hidden pane closes when it loses the keyboard. Nothing is drawn meanwhile.
		pane.Activate(true);

		if (focus != null)
		{
			System.Windows.Input.Keyboard.Focus(focus);
			return;
		}

		// The keyboard was nowhere: give it to the active map.
		FrameworkApplication.Panes.OfType<IMapPane>()
		                    .Where(mapPane => mapPane.MapView == MapView.Active)
		                    .OfType<Pane>()
		                    .FirstOrDefault()?.Activate();
	}

	/// <summary>
	/// Makes <see cref="ShowToolOptions"/> slide out an auto-hidden pane until disposed, for
	/// an explicit request of the user.
	/// </summary>
	[NotNull]
	public static IDisposable SlideOutAutoHidden()
	{
		bool previous = _slideOutAutoHidden;
		_slideOutAutoHidden = true;

		return new DisposableCallback(() => _slideOutAutoHidden = previous);
	}

	public static void Show<T>([NotNull] string id) where T : DockPane
	{
		T pane = GetViewModel<T>(id);

		if (pane is IFrameworkWindow window)
		{
			window.Activate();
		}
	}

	/// <summary>
	/// Shows a pane on an explicit request of the user, as the O key shows a tool's options:
	/// the fixed (stereo) cursor is released, so that the pane can be used with the mouse,
	/// and the keyboard stays where it is, so that the active tool keeps receiving the keys.
	/// An auto-hidden pane is slid out.
	/// </summary>
	public static async Task ShowReleasingCursorAsync<T>([NotNull] string id) where T : DockPane
	{
		T pane = GetViewModel<T>(id);

		await UIEnvironment.ReleaseCursorAsync();

		using (SlideOutAutoHidden())
		{
			ShowToolOptions(pane);
		}
	}

	public static T Toggle<T>([NotNull] string id) where T : DockPane
	{
		T dockPane = GetViewModel<T>(id);

		if (dockPane.IsVisible)
		{
			dockPane.Hide();
		}
		else
		{
			dockPane.Activate();
		}

		return dockPane;
	}

	[NotNull]
	public static T GetViewModel<T>([NotNull] string id) where T : DockPane
	{
		Assert.True(QueuedTask.OnGUI,
		            $"Cannot get DockPane with ID: {id} from outside the GUI Thread.");

		var dockPane = DockPaneManager.Find(id);
		Assert.NotNull(dockPane, $"DockPane {id} could not be created.");

		return (T) dockPane;
	}
}
