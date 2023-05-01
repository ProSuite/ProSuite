using System;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto
{
	public static class MapViewUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void NotNullCallback([CanBeNull] this MapView mapView,
		                                   [NotNull] Action<MapView> action)
		{
			if (mapView == null) return;
			try
			{
				action(mapView);
			}
			catch (Exception e)
			{
				_msg.Debug(e.Message);
			}
		}

		[CanBeNull]
		public static T NotNullCallback<T>([CanBeNull] this MapView mapView,
		                                   [NotNull] Func<MapView, T> action)
		{
			T result = default;

			try
			{
				if (mapView == null)
				{
					return result;
				}

				result = action(mapView);
			}
			catch (Exception e)
			{
				_msg.Debug(e.Message);
			}

			return result;
		}
	}
}
