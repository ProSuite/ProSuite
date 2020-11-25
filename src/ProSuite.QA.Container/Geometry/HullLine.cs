namespace ProSuite.QA.Container.Geometry
{
	public abstract class HullLine
	{
		public Lin2D Lin { get; set; }
		public double Inflate { get; protected set; }

		public CutPart CutPart { get; set; }
	}
}
