using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Env;

/// <summary>
/// Default implementation of IArcGISProEnvironment that provides navigation functionality
/// for ArcGIS Pro map views.
/// </summary>
public class ArcGISProEnvironmentImpl : IArcGISProEnvironment
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	#region Implementation of IArcGISProEnvironment

	public async Task ZoomToAsync(MapView mapView,
	                              Envelope extent,
	                              double minScaleDenominator = 0,
	                              double expansionFactor = 1.0,
	                              TimeSpan? duration = null)
	{
		Assert.ArgumentNotNull(mapView, nameof(mapView));
		Assert.ArgumentNotNull(extent, nameof(extent));

		await QueuedTask.Run(async () =>
			                     await ZoomToCoreAsync(mapView, extent, minScaleDenominator,
			                                           expansionFactor, duration));
	}

	public async Task PanToAsync(MapView mapView,
	                             Envelope extent,
	                             double minScaleDenominator = 0,
	                             double expansionFactor = 1.0,
	                             TimeSpan? duration = null)
	{
		Assert.ArgumentNotNull(mapView, nameof(mapView));
		Assert.ArgumentNotNull(extent, nameof(extent));

		await QueuedTask.Run(async () =>
			                     await PanToCoreAsync(mapView, extent, minScaleDenominator,
			                                          expansionFactor, duration));
	}

	#endregion

	protected virtual async Task ZoomToCoreAsync([NotNull] MapView mapView,
	                                             [NotNull] Envelope extent,
	                                             double minScaleDenominator,
	                                             double expansionFactor,
	                                             TimeSpan? duration)
	{
		_msg.VerboseDebug(
			() =>
				$"Zooming to extent: {extent.XMin:F2}, {extent.YMin:F2}, {extent.XMax:F2}, {extent.YMax:F2}" +
				(duration.HasValue
					 ? $", Duration: {duration.Value.TotalMilliseconds}ms"
					 : ""));

		await MapUtils.ZoomToAsync(mapView, extent, expansionFactor, minScaleDenominator,
		                           duration);
	}

	protected virtual async Task PanToCoreAsync([NotNull] MapView mapView,
	                                            [NotNull] Envelope extent,
	                                            double minScaleDenominator,
	                                            double expansionFactor,
	                                            TimeSpan? duration)
	{
		Envelope targetExtent =
			GetTargetExtent(mapView, extent, minScaleDenominator, expansionFactor);

		_msg.VerboseDebug(() =>
			                  $"Panning to extent: {targetExtent.XMin:F2}, {targetExtent.YMin:F2}, {targetExtent.XMax:F2}, {targetExtent.YMax:F2}" +
			                  (duration.HasValue
				                   ? $", Duration: {duration.Value.TotalMilliseconds}ms"
				                   : ""));

		await mapView.PanToAsync(targetExtent, duration);
	}

	[NotNull]
	protected virtual Envelope GetTargetExtent([NotNull] MapView mapView,
	                                           [NotNull] Envelope originalExtent,
	                                           double minScaleDenominator,
	                                           double expansionFactor)
	{
		Envelope targetExtent = originalExtent;

		// Apply expansion factor if specified
		if (expansionFactor > 0 && Math.Abs(expansionFactor - 1.0) > double.Epsilon)
		{
			targetExtent = targetExtent.Expand(expansionFactor, expansionFactor, true);

			_msg.VerboseDebug(() => $"Applied expansion factor {expansionFactor:F2}");
		}

		// Ensure minimum scale if specified
		if (minScaleDenominator > 0)
		{
			targetExtent = EnsureMinimumScale(targetExtent, minScaleDenominator, mapView);
		}

		return targetExtent;
	}

	[NotNull]
	private static Envelope EnsureMinimumScale([NotNull] Envelope extent,
	                                           double minScaleDenominator,
	                                           [NotNull] MapView mapView)
	{
		// Use the existing logic from MapUtils to ensure minimum scale
		double currentScale = mapView.Camera.Scale;
		Envelope currentExtent = mapView.Extent;

		return MapUtils.GetZoomExtent(extent, currentExtent, currentScale, minScaleDenominator);
	}
}
