using System;
using System.Reflection;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.QA.Tests.Surface
{
	public static class SurfaceUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private static int? _lastErrorSurface;

		[CanBeNull]
		public static IPolygon TryGetDomain([NotNull] ISimpleSurface surface)
		{
			try
			{
				var result = surface.GetDomain();
				_lastErrorSurface = null; // success -> report the next error again
				return result;
			}
			catch (Exception e)
			{
				var hash = surface.GetHashCode();
				if (_lastErrorSurface != hash)
				{
					_lastErrorSurface = hash;

					LogDomainError(surface, e);
				}

				return null;
			}
		}

		private static void LogDomainError([NotNull] ISimpleSurface surface,
		                                   [NotNull] Exception e)
		{
			_msg.WarnFormat("Error getting surface domain: {0}", e.Message);

			try
			{
				if (surface.AsRaster() is IRaster raster)
				{
					if (raster is IRasterProps rasterProps)
					{
						_msg.DebugFormat("Raster width: {0}", rasterProps.Width);
						_msg.DebugFormat("Raster height: {0}", rasterProps.Height);
						_msg.DebugFormat("Raster envelope: {0}",
														 GeometryUtils.Format(rasterProps.Extent));
					}
				}
			}
			catch (Exception e1)
			{
				_msg.WarnFormat("Error logging raster properties: {0}", e1.Message);
			}
		}
	}
}
