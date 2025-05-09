using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto
{
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
}
