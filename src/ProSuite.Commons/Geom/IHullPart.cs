namespace ProSuite.Commons.Geom
{
	public interface IHullPart
	{
		void Cut(Pnt segmentStart, Pnt segmentLine, double offset, ref double tMin,
		         ref double tMax);

		bool Cut(HullLineSimple lin, ref double tMin, ref double tMax);

		bool Cut(HullLineArc lin, ref double tMin, ref double tMax);

		bool Cut(HullLineLine lin, ref double tMin, ref double tMax);

		CutPart CutPart { get; set; }
	}
}
