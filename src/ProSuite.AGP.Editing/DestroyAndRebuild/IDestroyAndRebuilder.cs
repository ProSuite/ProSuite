using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

/// <summary>
/// The outcome of asking the rebuilder to replace the current selection's geometry (see
/// <see cref="IDestroyAndRebuilder.TryReplaceSelectedGeometryAsync"/>). It tells the calling
/// construction tool whether it still has to create a new feature itself.
/// </summary>
public enum ReplaceGeometryResult
{
	/// <summary>
	/// The current selection is not an appropriate replace target (no single multipatch feature
	/// is selected). The calling tool should create a new feature instead, as if the replace mode
	/// were off.
	/// </summary>
	NoTarget,

	/// <summary>
	/// A valid target was found and its geometry was replaced. The calling tool must not also
	/// create a new feature.
	/// </summary>
	Replaced,

	/// <summary>
	/// A valid target was found but its geometry was not replaced: it is not visible in the
	/// current map extent, or the edit operation failed. A message has been logged; the calling
	/// tool must not create a new feature.
	/// </summary>
	NotReplaced
}

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
	/// Attempts to replace the geometry of the single selected multipatch with
	/// <paramref name="newGeometry"/> (keeping its OID / subtype / attributes), but only when the
	/// current selection is an appropriate, visible replace target. Call from the tool's edit-sketch
	/// completion and act on the result:
	/// <list type="bullet">
	/// <item><see cref="ReplaceGeometryResult.NoTarget"/>: no single multipatch is selected, so the
	/// tool should create a new feature as usual.</item>
	/// <item><see cref="ReplaceGeometryResult.Replaced"/>: the selected multipatch was replaced.</item>
	/// <item><see cref="ReplaceGeometryResult.NotReplaced"/>: a target is selected but was not
	/// replaced (e.g. it is outside the current map extent); a message was logged and the tool must
	/// not create a new feature.</item>
	/// </list>
	/// </summary>
	Task<ReplaceGeometryResult> TryReplaceSelectedGeometryAsync(
		[NotNull] Geometry newGeometry, [NotNull] MapView activeView);
}
