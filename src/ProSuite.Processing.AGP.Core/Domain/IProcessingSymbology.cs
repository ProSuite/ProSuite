using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.AGP.Core.Utils;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.AGP.Core.Domain;

public interface IProcessingSymbology
{
	/// <summary>
	/// Get the exact drawing outline of the given real feature.
	/// The result can be null if drawing is extinguished by
	/// overrides, effects, and/or placements.
	/// </summary>
	[CanBeNull]
	Polygon GetDrawingOutline(Feature feature);

	/// <summary>
	/// Equivalent to GetDrawingOutline()?.Extent but may be faster.
	/// </summary>
	[CanBeNull]
	Envelope GetDrawingBounds(Feature feature);

	/// <summary>
	/// Get the exact drawing outline of the given pseudo feature
	/// (geometry and named values for overrides). The result can
	/// be null if drawing is extinguished by overrides, effects,
	/// and/or placements.
	/// </summary>
	[CanBeNull]
	Polygon GetDrawingOutline(PseudoFeature feature, IMapContext mapContext,
	                          DrawingOutline.Options options = null);

	/// <summary>
	/// Equivalent to GetDrawingOutline()?.Extent but may be faster
	/// </summary>
	[CanBeNull]
	Envelope GetDrawingBounds(PseudoFeature feature, IMapContext mapContext,
	                          DrawingOutline.Options options = null);
}
