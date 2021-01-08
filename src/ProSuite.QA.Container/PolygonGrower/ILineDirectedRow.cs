using System;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	[CLSCompliant(false)]
	public interface ILineDirectedRow : IDirectedRow
	{
		IPoint FromPoint { get; }
		IPoint ToPoint { get; }

		double FromAngle { get; }
		double ToAngle { get; }

		void QueryEnvelope(IEnvelope queryEnvelope);

		int Orientation { get; }

		ICurve GetBaseLine();

		ISegmentCollection GetDirectedSegmentCollection();
	}
}
