namespace ProSuite.QA.Container.Geometry
{
	public class HullLineArc : HullLine
	{
		private double _radius;

		public double Radius
		{
			get { return _radius; }
			set
			{
				_radius = value;
				Inflate = value;
			}
		}

		public double StartDirection { get; set; }
		public double Angle { get; set; }
	}
}
