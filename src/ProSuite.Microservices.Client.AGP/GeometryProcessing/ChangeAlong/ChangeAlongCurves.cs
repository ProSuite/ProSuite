using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong
{
	public class ChangeAlongCurves
	{
		private ReshapeAlongCurveUsability _curveUsability;
		private readonly List<CutSubcurve> _reshapeSubcurves;

		public ChangeAlongCurves([NotNull] IEnumerable<CutSubcurve> subcurves,
		                         ReshapeAlongCurveUsability curveUsability)
		{
			_curveUsability = curveUsability;
			_reshapeSubcurves = new List<CutSubcurve>(subcurves);
		}

		public void Update([NotNull] ChangeAlongCurves newState)
		{
			_curveUsability = newState._curveUsability;

			_reshapeSubcurves.Clear();
			_reshapeSubcurves.AddRange(newState.ReshapeCutSubcurves);

			TargetFeatures = newState.TargetFeatures;
		}

		public IReadOnlyCollection<CutSubcurve> ReshapeCutSubcurves => _reshapeSubcurves.AsReadOnly();

		public bool HasSelectableCurves => _curveUsability == ReshapeAlongCurveUsability.CanReshape ||
		                                   (_reshapeSubcurves?.Any() ?? false);

		[CanBeNull]
		public IList<Feature> TargetFeatures { get; set; }

		//public void Recalculate(IList<Feature> sourceFeatures, 
		//	IList<Feature> targetFeatures)
		//{

		//	_reshapeSubcurves = new List<CutSubcurve>();

		//}
	}
}
