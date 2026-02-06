using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework;

public static class DockPaneUtils
{
	public static DockPaneManager DockPaneManager => FrameworkApplication.DockPaneManager;

	public static void Show<T>([NotNull] string id) where T : DockPane
	{
		T pane = GetViewModel<T>(id);

		if (pane is IFrameworkWindow window)
		{
			window.Activate();
		}
	}

	public static T Toggle<T>([NotNull] string id) where T : DockPane
	{
		T dockPane = GetViewModel<T>(id);

		if (dockPane != null)
		{
			if (dockPane.IsVisible)
			{
				dockPane.Hide();
			}
			else
			{
				dockPane.Activate();
			}
		}

		return dockPane;
	}

	[NotNull] public static T GetViewModel<T>([NotNull] string id) where T : DockPane
	{
		Assert.True(QueuedTask.OnGUI, $"Cannot get DockPane with ID: {id} from outside the GUI Thread.");
		Assert.True(DockPaneManager.IsDockPaneCreated(id), $"DockPane with ID: {id} has not been created.");

		var dockPane = DockPaneManager.Find(id) as T;

		Assert.NotNull(dockPane, $"DockPane {id} has not been created");

		return dockPane;
	}
}
