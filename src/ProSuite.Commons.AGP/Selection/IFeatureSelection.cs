using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection;

/// <summary>
/// Abstraction over a per-layer / per-feature class feature selection, exposing only the
/// application-independent aspects (geometry dimension, feature count, object ids).
/// </summary>
public interface IFeatureSelection
{
	/// <summary>
	/// The dimension of the selection's geometry type (0 = point, 1 = line, 2 = area).
	/// </summary>
	int ShapeDimension { get; }

	/// <summary>
	/// The number of selected features.
	/// </summary>
	int GetCount();

	/// <summary>
	/// The object ids of the selected features.
	/// </summary>
	[NotNull]
	IEnumerable<long> GetOids();
}
