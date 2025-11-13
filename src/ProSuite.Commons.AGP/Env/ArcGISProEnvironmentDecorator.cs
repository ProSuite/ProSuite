using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Env;

/// <summary>
/// Abstract base class for decorating IArcGISProEnvironment implementations.
/// Follows the decorator pattern to allow extending or modifying behavior
/// without changing the original implementation.
/// </summary>
public abstract class ArcGISProEnvironmentDecorator : IArcGISProEnvironment
{
	private IArcGISProEnvironment _target;

	/// <summary>
	/// Gets the target IArcGISProEnvironment instance that this decorator wraps.
	/// </summary>
	[NotNull]
	protected IArcGISProEnvironment Target
	{
		get
		{
			Assert.NotNull(_target, "Target environment is not set");
			return _target;
		}
	}

	/// <summary>
	/// Sets the target IArcGISProEnvironment instance to be decorated.
	/// </summary>
	/// <param name="target">The target environment instance to wrap.</param>
	protected internal void SetTarget([NotNull] IArcGISProEnvironment target)
	{
		Assert.ArgumentNotNull(target, nameof(target));
		_target = target;
	}

	#region Implementation of IArcGISProEnvironment

	/// <summary>
	/// Zooms to the specified extent with optional minimum scale, expansion factor, and animation duration.
	/// Default implementation delegates to the target.
	/// </summary>
	/// <param name="mapView">The map view to zoom in.</param>
	/// <param name="extent">The extent to zoom to.</param>
	/// <param name="minScaleDenominator">Minimum scale denominator to ensure.</param>
	/// <param name="expansionFactor">Factor to expand the extent by.</param>
	/// <param name="duration">Duration of the zoom animation.</param>
	public virtual async Task ZoomToAsync(MapView mapView,
	                                      Envelope extent,
	                                      double minScaleDenominator = 0,
	                                      double expansionFactor = 1.0,
	                                      TimeSpan? duration = null)
	{
		await Target.ZoomToAsync(mapView, extent, minScaleDenominator, expansionFactor,
		                         duration);
	}

	/// <summary>
	/// Pans to the specified extent with optional minimum scale, expansion factor, and animation duration.
	/// Default implementation delegates to the target.
	/// </summary>
	/// <param name="mapView">The map view to pan in.</param>
	/// <param name="extent">The extent to pan to.</param>
	/// <param name="minScaleDenominator">Minimum scale denominator to ensure.</param>
	/// <param name="expansionFactor">Factor to expand the extent by.</param>
	/// <param name="duration">Duration of the pan animation.</param>
	public virtual async Task PanToAsync(MapView mapView,
	                                     Envelope extent,
	                                     double minScaleDenominator = 0,
	                                     double expansionFactor = 1.0,
	                                     TimeSpan? duration = null)
	{
		await Target.PanToAsync(mapView, extent, minScaleDenominator, expansionFactor,
		                        duration);
	}

	#endregion
}
