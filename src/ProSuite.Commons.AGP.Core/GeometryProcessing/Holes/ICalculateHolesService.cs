using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Holes;

/// <summary>
/// Abstraction for the calculation of holes in a set of features.
/// </summary>
public interface ICalculateHolesService
{
	IList<Holes> CalculateHoles(
		[NotNull] IList<Feature> selectedFeatures,
		[NotNull] IList<Envelope> clipEnvelopes,
		bool unionFeatures,
		CancellationToken cancellationToken);
}
