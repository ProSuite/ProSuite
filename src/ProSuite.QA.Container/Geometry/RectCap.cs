using System;
using System.Collections.Generic;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public class RectCap : SegmentCap
	{
		private readonly double _length;

		public RectCap(double length)
		{
			_length = length;
		}

		public override bool IsFullDeflatable
		{
			get { return false; }
		}

		public override double GetDeflateRadius(double offset)
		{
			return Math.Min(_length, offset);
		}

		public override IEnumerable<HullLine> GetHullLines(Lin2D lin, double meanOffset,
		                                                   bool atEnd)
		{
			double l = atEnd ? _length : -_length;
			Pnt offset = l * lin.L;
			Pnt rOffset = -meanOffset * lin.LNormal;
			Pnt lOffset = meanOffset * lin.LNormal;

			yield return new HullLineLine
			             {
				             Lin = lin.GetParallel(offset),
				             EndPart = new Lin2D(rOffset, lOffset),
			             };
			if (Math.Abs(l) > 1.0e-8)
			{
				var endPart = new Lin2D(new Pnt2D(), offset);
				yield return
					new HullLineLine {Lin = lin.GetParallel(lOffset), EndPart = endPart};
				yield return
					new HullLineLine {Lin = lin.GetParallel(rOffset), EndPart = endPart};
			}
		}

		public override IEnumerable<IHullPart> GetInflatedHullParts(Pnt at, Pnt opposite,
		                                                            double offset,
		                                                            double inflate)
		{
			Pnt l = opposite - at;
			double l0 = Math.Sqrt(l * l);
			Pnt lNorm = new Pnt2D(l.X / l0, l.Y / l0);
			Pnt vNorm = new Pnt2D(lNorm.Y, -lNorm.X);

			Pnt pntOffset = Math.Max(0, offset - _length) * vNorm;
			yield return new CircleHullPart(at + pntOffset, _length + inflate);

			Pnt cap = at - (_length + inflate) * lNorm;
			yield return new LineHullPart(cap + pntOffset, cap - pntOffset);

			yield return new CircleHullPart(at - pntOffset, _length + inflate);
		}
	}
}
