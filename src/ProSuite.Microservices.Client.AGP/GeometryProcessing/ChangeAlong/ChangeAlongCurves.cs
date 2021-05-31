using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong
{
	public class ChangeAlongCurves
	{
		private readonly ReshapeAlongCurveUsability _curveUsability;
		private readonly List<CutSubcurve> _reshapeSubcurves;

		public ChangeAlongCurves(IEnumerable<CutSubcurve> subcurves, ReshapeAlongCurveUsability curveUsability)
		{
			_curveUsability = curveUsability;
			_reshapeSubcurves = new List<CutSubcurve>(subcurves);
		}

		public IReadOnlyCollection<CutSubcurve> ReshapeCutSubcurves => _reshapeSubcurves.AsReadOnly();

		public bool HasSelectableCurves => _curveUsability == ReshapeAlongCurveUsability.CanReshape ||
		                                   (_reshapeSubcurves?.Any() ?? false);

		public IList<Feature> TargetFeatures { get; set; }

		//public void Recalculate(IList<Feature> sourceFeatures, 
		//	IList<Feature> targetFeatures)
		//{

		//	_reshapeSubcurves = new List<CutSubcurve>();

		//}
	}
}
