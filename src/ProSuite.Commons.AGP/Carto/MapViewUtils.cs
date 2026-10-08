using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

public static class MapViewUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public static IEnumerable<MapView> GetAllMapViews(bool allowNullMaps = false)
	{
		foreach (IMapPane mapPane in FrameworkApplication.Panes.OfType<IMapPane>())
		{
			MapView mapView = mapPane.MapView;

			if (mapView == null)
			{
				continue;
			}

			if (allowNullMaps || mapView.Map != null)
			{
				yield return mapView;
			}
		}
	}

	/// <summary>
	/// Gets the map view that is visible at the screen location. This is not necessarily
	/// the active map view: the mouse can be over another map view when a command is
	/// started by its keyboard shortcut. A map view behind another tab or covered by
	/// another window is not returned. Must be called on the UI thread.
	/// </summary>
	/// <param name="screenLocation">The location in screen pixels</param>
	/// <returns>The map view or null if no map view is visible at the location.</returns>
	[CanBeNull]
	public static MapView GetMapViewAt(Point screenLocation)
	{
		MapView result = GetMapPaneAt(screenLocation)?.MapView;

		_msg.DebugFormat("Map view at screen location {0}: {1}", screenLocation,
		                 result?.Map?.Name ?? "<none>");

		return result;
	}

	[CanBeNull]
	private static IMapPane GetMapPaneAt(Point screenLocation)
	{
		if (double.IsNaN(screenLocation.X) || double.IsNaN(screenLocation.Y))
		{
			return null;
		}

		// Windows returns the window on top, which excludes a map view covered by another window
		IntPtr windowHandle = WindowFromPoint(
			new System.Drawing.Point((int) screenLocation.X, (int) screenLocation.Y));

		if (windowHandle == IntPtr.Zero)
		{
			return null;
		}

		const uint topLevelWindow = 2; // GA_ROOT

		HwndSource topLevelSource =
			HwndSource.FromHwnd(GetAncestor(windowHandle, topLevelWindow));

		if (topLevelSource?.RootVisual == null)
		{
			// The window of another application or a window without WPF content
			return null;
		}

		return GetMapPaneAt(topLevelSource.RootVisual, screenLocation);
	}

	/// <summary>
	/// Searches the visible elements below the specified element for one that belongs to a
	/// map pane and contains the screen location. The elements of a map view behind another
	/// tab are not visible.
	/// </summary>
	[CanBeNull]
	private static IMapPane GetMapPaneAt([NotNull] DependencyObject element,
	                                     Point screenLocation)
	{
		if (element is UIElement { IsVisible: false })
		{
			return null;
		}

		if (element is FrameworkElement { DataContext: IMapPane mapPane } frameworkElement &&
		    ContainsScreenLocation(frameworkElement, screenLocation))
		{
			return mapPane;
		}

		int childCount = VisualTreeHelper.GetChildrenCount(element);

		for (int i = 0; i < childCount; i++)
		{
			IMapPane result = GetMapPaneAt(VisualTreeHelper.GetChild(element, i),
			                               screenLocation);

			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	private static bool ContainsScreenLocation([NotNull] FrameworkElement element,
	                                           Point screenLocation)
	{
		if (PresentationSource.FromVisual(element) == null)
		{
			return false;
		}

		Point elementLocation = element.PointFromScreen(screenLocation);

		return elementLocation.X >= 0 && elementLocation.X < element.ActualWidth &&
		       elementLocation.Y >= 0 && elementLocation.Y < element.ActualHeight;
	}

	[DllImport("user32.dll")]
	private static extern IntPtr WindowFromPoint(System.Drawing.Point point);

	[DllImport("user32.dll")]
	private static extern IntPtr GetAncestor(IntPtr windowHandle, uint flags);

	public static void NotNullCallback([CanBeNull] this MapView mv,
	                                   [NotNull] Action<MapView> action)
	{
		if (mv == null) return;
		try
		{
			action(mv);
		}
		catch (Exception e)
		{
			_msg.Debug(e.Message);
		}
	}

	[CanBeNull]
	public static T NotNullCallback<T>([CanBeNull] this MapView mv,
	                                   [NotNull] Func<MapView, T> action)
	{
		T result = default;

		try
		{
			if (mv == null)
			{
				return result;
			}

			result = action(mv);
		}
		catch (Exception e)
		{
			_msg.Debug(e.Message);
		}

		return result;
	}
}
