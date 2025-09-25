using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Env;

/// <summary>
/// Static facade for accessing ArcGIS Pro environment functionality.
/// Provides a singleton pattern for managing IArcGISProEnvironment implementations
/// with support for decorators.
/// </summary>
public static class ProEnvironment
{
	private static IArcGISProEnvironment _implementation;

	/// <summary>
	/// Gets the current IArcGISProEnvironment implementation.
	/// If no implementation is set, returns a default ArcGISProEnvironment instance.
	/// </summary>
	[NotNull]
	private static IArcGISProEnvironment Implementation
	{
		get { return _implementation ??= new ArcGISProEnvironmentImpl(); }
	}

	/// <summary>
	/// Sets a custom IArcGISProEnvironment implementation.
	/// </summary>
	/// <param name="implementation">The implementation to use.</param>
	public static void SetImplementation([NotNull] IArcGISProEnvironment implementation)
	{
		Assert.ArgumentNotNull(implementation, nameof(implementation));
		_implementation = implementation;
	}

	/// <summary>
	/// Adds a decorator to the current implementation.
	/// The decorator will wrap the existing implementation.
	/// </summary>
	/// <param name="decorator">The decorator to add.</param>
	public static void AddDecorator([NotNull] ArcGISProEnvironmentDecorator decorator)
	{
		Assert.ArgumentNotNull(decorator, nameof(decorator));

		decorator.SetTarget(Implementation);
		SetImplementation(decorator);
	}

	/// <summary>
	/// Zooms to the specified extent with optional minimum scale, expansion factor, and animation duration.
	/// </summary>
	/// <param name="mapView">The map view to zoom in.</param>
	/// <param name="extent">The extent to zoom to.</param>
	/// <param name="minScaleDenominator">Minimum scale denominator to ensure (0 to ignore).</param>
	/// <param name="expansionFactor">Factor to expand the extent by (1.0 for no expansion).</param>
	/// <param name="duration">Duration of the zoom animation (null for default).</param>
	public static async Task ZoomToAsync([NotNull] MapView mapView,
	                                     [NotNull] Envelope extent,
	                                     double minScaleDenominator = 0,
	                                     double expansionFactor = 1.0,
	                                     TimeSpan? duration = null)
	{
		await Implementation.ZoomToAsync(mapView, extent, minScaleDenominator, expansionFactor,
		                                 duration);
	}

	/// <summary>
	/// Pans to the specified extent with optional minimum scale, expansion factor, and animation duration.
	/// </summary>
	/// <param name="mapView">The map view to pan in.</param>
	/// <param name="extent">The extent to pan to.</param>
	/// <param name="minScaleDenominator">Minimum scale denominator to ensure (0 to ignore).</param>
	/// <param name="expansionFactor">Factor to expand the extent by (1.0 for no expansion).</param>
	/// <param name="duration">Duration of the pan animation (null for default).</param>
	public static async Task PanToAsync([NotNull] MapView mapView,
	                                    [NotNull] Envelope extent,
	                                    double minScaleDenominator = 0,
	                                    double expansionFactor = 1.0,
	                                    TimeSpan? duration = null)
	{
		await Implementation.PanToAsync(mapView, extent, minScaleDenominator, expansionFactor,
		                                duration);
	}

	/// <summary>
	/// Zooms to the specified extent with optional minimum scale, expansion factor, and animation duration using the active map view.
	/// </summary>
	/// <param name="extent">The extent to zoom to.</param>
	/// <param name="minScaleDenominator">Minimum scale denominator to ensure (0 to ignore).</param>
	/// <param name="expansionFactor">Factor to expand the extent by (1.0 for no expansion).</param>
	/// <param name="duration">Duration of the zoom animation (null for default).</param>
	public static async Task ZoomToAsync([NotNull] Envelope extent,
	                                     double minScaleDenominator = 0,
	                                     double expansionFactor = 1.0,
	                                     TimeSpan? duration = null)
	{
		MapView activeMapView = MapView.Active;
		if (activeMapView == null)
		{
			throw new InvalidOperationException(
				"No active map view available for zoom operation");
		}

		await ZoomToAsync(activeMapView, extent, minScaleDenominator, expansionFactor,
		                  duration);
	}

	/// <summary>
	/// Pans to the specified extent with optional minimum scale, expansion factor, and animation duration using the active map view.
	/// </summary>
	/// <param name="extent">The extent to pan to.</param>
	/// <param name="minScaleDenominator">Minimum scale denominator to ensure (0 to ignore).</param>
	/// <param name="expansionFactor">Factor to expand the extent by (1.0 for no expansion).</param>
	/// <param name="duration">Duration of the pan animation (null for default).</param>
	public static async Task PanToAsync([NotNull] Envelope extent,
	                                    double minScaleDenominator = 0,
	                                    double expansionFactor = 1.0,
	                                    TimeSpan? duration = null)
	{
		MapView activeMapView = MapView.Active;
		if (activeMapView == null)
		{
			throw new InvalidOperationException(
				"No active map view available for pan operation");
		}

		await PanToAsync(activeMapView, extent, minScaleDenominator, expansionFactor, duration);
	}

	/// <summary>
	/// Resets the implementation to the default ArcGISProEnvironment.
	/// This removes all decorators.
	/// </summary>
	public static void Reset()
	{
		_implementation = new ArcGISProEnvironmentImpl();
	}
}
