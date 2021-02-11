using System.Linq;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework
{
	public static class PaneUtils
	{
		// MapVIew.Active is null if any other pane is active, e.g. catalog pane, layout pane.
		// But an open table is not a pane.

		/// <summary>
		/// Activates the first <see cref="Pane" /> or <see cref="DockPane" /> found.
		/// </summary>
		/// <typeparam name="T"><see cref="PaneBase" /></typeparam>
		/// <param name="caption">The pane's caption. Can be null or empty.</param>
		public static void ActivatePane<T>([CanBeNull] string caption = null) where T : PaneBase
		{
			foreach (var pane in FrameworkApplication.Panes.OfType<T>())
			{
				if (! (pane is IFrameworkWindow window)) continue;

				if (string.IsNullOrEmpty(caption))
				{
					window.Activate();
					break;
				}

				if (string.Equals(pane.Caption, caption))
				{
					window.Activate();
					break;
				}
			}
		}

		/// <summary>
		/// Activates the first pane found by type, e.g. <see cref="IMapPane" />.
		/// </summary>
		/// <typeparam name="T">class</typeparam>
		/// <param name="caption">The pane's caption. Can be null or empty.</param>
		public static void Activate<T>([CanBeNull] string caption = null) where T : class
		{
			foreach (var pane in FrameworkApplication.Panes.OfType<T>())
			{
				if (! (pane is IFrameworkWindow window)) continue;
				if (! (window is PaneBase paneBase)) continue;

				if (string.IsNullOrEmpty(caption))
				{
					window.Activate();
					break;
				}

				if (string.Equals(paneBase.Caption, caption))
				{
					window.Activate();
					break;
				}
			}
		}

		/// <summary>
		/// Activates the oldest pane. The first pane added to the map
		/// is considered to be most recent.
		/// </summary>
		/// <typeparam name="T">class</typeparam>
		public static void ActivateOldest<T>() where T : class
		{
			T pane = FrameworkApplication.Panes.OfType<T>().FirstOrDefault();
			if (! (pane is IFrameworkWindow window)) return;

			window.Activate();
		}

		/// <summary>
		/// Activates the most recent pane. The last pane added to the map
		/// is considered to be most recent.
		/// </summary>
		/// <typeparam name="T">class</typeparam>
		public static void ActivateNewest<T>() where T : class
		{
			T pane = FrameworkApplication.Panes.OfType<T>().LastOrDefault();
			if (! (pane is IFrameworkWindow window)) return;

			window.Activate();
		}
	}
}
