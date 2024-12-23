using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework
{
	public static class DockPaneUtils
	{
		public static DockPaneManager DockPaneManager => FrameworkApplication.DockPaneManager;

		public static void Show<T>([NotNull] string id) where T : DockPane
		{
			EnsureDockPaneExists(id);

			var pane = DockPaneManager.Find(id) as T;

			if (pane is IFrameworkWindow window)
			{
				window.Activate();
			}
		}

		public static T Toggle<T>([NotNull] string id) where T : DockPane
		{
			EnsureDockPaneExists(id);

			T dockPane = DockPaneManager.Find(id) as T;

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

		private static void EnsureDockPaneExists([NotNull] string id)
		{
			if (! DockPaneExists(id))
			{
				DockPaneManager.Find(id);
			}

			Assert.True(DockPaneExists(id), $"DockPane {id} has not been created");
		}

		private static bool DockPaneExists([NotNull] string id)
		{
			return DockPaneManager.IsDockPaneCreated(id);
		}
	}
}
