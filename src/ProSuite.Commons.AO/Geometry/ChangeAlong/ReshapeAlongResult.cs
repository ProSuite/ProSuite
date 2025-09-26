using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class ReshapeAlongResult
	{
		public ReshapeAlongResult(ReshapeAlongCurveUsability usability,
		                          [NotNull] IList<CutSubcurve> subcurves)
		{
			Usability = usability;
			Subcurves = subcurves;
		}

		public IPolyline FilterBuffer { get; set; }

		public ReshapeAlongCurveUsability Usability { get; }

		[NotNull]
		public IList<CutSubcurve> Subcurves { get; }
	}
}
