
using System.Collections.Generic;

namespace ProSuite.Commons.Geom
{
	public abstract class SegmentCap
	{
		public abstract bool IsFullDeflatable { get; }

		public abstract double GetDeflateRadius(double offset);

		public abstract IEnumerable<HullLine> GetHullLines(
			Lin2D lin, double meanOffset, bool atEnd);

		public abstract IEnumerable<IHullPart> GetInflatedHullParts(
			Pnt at, Pnt opposite, double offset, double inflate);
	}
}
