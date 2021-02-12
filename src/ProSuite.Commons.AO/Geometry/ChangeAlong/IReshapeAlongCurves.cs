using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public interface IReshapeAlongCurves
	{
		IList<CutSubcurve> GetSelectedReshapeCurves(
			[CanBeNull] Predicate<IPath> useSubCurvePredicate,
			bool includeAllPreSelectedCandidates,
			bool searchAllSubcurves = false);

		List<CutSubcurve> GetCombinedGeometriesReshapeCurves(
			[CanBeNull] Predicate<IPath> useSubCurvePredicate,
			bool includeAllPreSelectedCandidates,
			bool searchAllSubcurves = false);
	}
}
