using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class ZSettingsModel : IZSettingsModel
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly Map _mapWithSurfaceLayer;
	private readonly ElevationSurfaceLayer _surfaceLayer;

	public ZSettingsModel(ZMode defaultZMode,
	                      [CanBeNull] Map mapWithSurfaceLayer,
	                      ElevationSurfaceLayer surfaceLayer)
	{
		_mapWithSurfaceLayer = mapWithSurfaceLayer;
		_surfaceLayer = surfaceLayer;
		CurrentMode = defaultZMode;
	}

	public ZMode CurrentMode { get; set; }

	public Multipart ApplyUndefinedZs(Multipart geometry)
	{
		if (CurrentMode == ZMode.None)
		{
			return geometry;
		}

		if (CurrentMode == ZMode.Dtm)
		{
			if (_mapWithSurfaceLayer == null || _surfaceLayer == null)
			{
				_msg.Warn("No map / surface layer available to apply DTM Zs.");
			}
			else
			{
				var surfaceZsMissingHandler = new SurfaceZsMissingHandler
				                              {
					                              OnlyProcessMissingZs = true,
					                              OutputZ = double.NaN
				                              };

#if ARCGISPRO_GREATER_3_2
				SurfaceZsResult result =
					_mapWithSurfaceLayer.GetZsFromSurface(
						geometry, _surfaceLayer, surfaceZsMissingHandler);

				if (result.Status == SurfaceZsResultStatus.Ok)
				{
					return result.Geometry as Multipart ?? geometry;
				}

				_msg.WarnFormat("Failed to apply Z values: {0}", result.Status);
#else
					_msg.Warn("GetZsFromSurface is not available in this version of the API.");
					return geometry;
#endif
			}
		}
		else if (CurrentMode == ZMode.Interpolate)
		{
			return GeometryEngine.Instance.CalculateNonSimpleZs(geometry, 0d);
		}

		return geometry;
	}
}