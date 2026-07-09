using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

/// <summary>
/// Encapsulates the "Destroy &amp; Rebuild" replace behavior for (multipatch) construction tools:
/// The global mode toggle, the target feedback, the target geometry-type filter and the geometry
/// swap. A participating construction tool holds one instance and re-directs the relevant calls to
/// an implementation, so the construction tool base classes stay generic and the D&R logic is
/// re-usable independent of the class hierarchy of the construction tools.
/// </summary>
public interface IDestroyAndRebuilder
{
	/// <summary>
	/// Whether the replace mode is currently on (the global Destroy &amp; Rebuild mode toggle). While
	/// active the tool replaces the selected multipatch instead of inserting a new feature.
	/// </summary>
	bool IsActive { get; }

	/// <summary>Starts the target feedback. Call on tool activation.</summary>
	Task ActivateAsync([NotNull] Func<MapView> activeMapViewProvider);

	/// <summary>Stops the target feedback. Call on tool deactivation.</summary>
	void Deactivate();

	/// <summary>
	/// Redraws the target feedback once the tool is fully live (overlays added while the tool is
	/// still activating are not reliably kept). Call from the tool's mouse-move handler.
	/// </summary>
	void EnsureInitialFeedbackRefresh();

	/// <summary>Whether the geometry type is a valid replace target (multipatch only).</summary>
	bool CanSelectTargetGeometryType(GeometryType geometryType);

	/// <summary>
	/// Replaces the geometry of the single selected multipatch with <paramref name="newGeometry"/>,
	/// keeping its OID / subtype / attributes. Call from the tool's edit-sketch completion.
	/// </summary>
	Task<bool> ReplaceSelectedGeometryAsync([NotNull] Geometry newGeometry,
	                                        [NotNull] MapView activeView);
}
