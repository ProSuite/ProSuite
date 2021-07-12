using System;
using System.Collections.Generic;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public class RoundCap : SegmentCap
	{
		public override bool IsFullDeflatable
		{
			get { return true; }
		}

		public override double GetDeflateRadius(double offset)
		{
			return offset;
		}

		public override IEnumerable<HullLine> GetHullLines(
			Lin2D lin, double meanOffset, bool atEnd)
		{
			double linDir = Math.Atan2(lin.L.Y, lin.L.X);
			double startDir = atEnd ? linDir - Math.PI / 2 : linDir + Math.PI / 2;
			yield return new HullLineArc
			             {
				             Lin = lin, Radius = meanOffset, StartDirection = startDir,
				             Angle = Math.PI
			             };
		}

		public override IEnumerable<IHullPart> GetInflatedHullParts(
			Pnt at, Pnt opposite, double offset, double inflate)
		{
			var part = new CircleHullPart(at, offset + inflate);
			if (at.Dist2(opposite) < offset * offset)
			{
				part.Angle = Math.PI;

				Pnt l = opposite - at;

				part.StartDirection = Math.Atan2(l.X, -l.Y);
			}

			yield return part;
		}
	}
}
