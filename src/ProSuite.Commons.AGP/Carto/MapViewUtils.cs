using System;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public static class MapViewUtils
	{
		public static void NotNullCallback([CanBeNull] this MapView mapView,
		                                   [NotNull] Action<Map> action)
		{
			if (mapView?.Map == null) return;
			action(mapView.Map);
		}

		public static void NotNullCallback([CanBeNull] this MapView mapView,
		                                   [NotNull] Action<MapView> action)
		{
			if (mapView == null) return;
			action(mapView);
		}

		[CanBeNull]
		public static T NotNullCallback<T>([CanBeNull] this MapView mapView,
		                                   [NotNull] Func<MapView, T> action)
		{
			return mapView == null ? default : action(mapView);
		}
	}
}
