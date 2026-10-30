using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

/// <summary>
/// A tool that draws an edit sketch, possibly after a selection phase, and that accepts a
/// sketch from outside, for example a sketch from the sketch history.
/// </summary>
public interface IEditSketchTool
{
	/// <summary>
	/// Whether the tool currently shows its edit sketch, and not a selection sketch.
	/// </summary>
	bool IsInEditSketchPhase { get; }

	/// <summary>
	/// The geometry type of the edit sketch, also while the tool is in its selection phase.
	/// Null if the tool cannot tell it yet, e.g. a create tool without a template.
	/// </summary>
	SketchGeometryType? EditSketchGeometryType { get; }

	/// <summary>
	/// Replaces the edit sketch by the specified sketch. A tool in its selection phase first
	/// starts the sketch phase if the current selection allows it.
	/// </summary>
	/// <returns>False if the tool stays in its selection phase and the sketch was not set.
	/// </returns>
	Task<bool> TrySetEditSketchAsync([NotNull] Geometry sketch);
}
