using System;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public class XyDomainProperties
	{
		public XyDomainProperties(double xMin, double yMin, double xMax, double yMax)
		{
			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;

			Width = Math.Abs(XMax - XMin);
			Height = Math.Abs(YMax - YMin);
		}

		[DisplayName("X Minimum")]
		[UsedImplicitly]
		public double XMin { get; private set; }

		[DisplayName("Y Minimum")]
		[UsedImplicitly]
		public double YMin { get; private set; }

		[DisplayName("X Maximum")]
		[UsedImplicitly]
		public double XMax { get; private set; }

		[DisplayName("Y Maximum")]
		[UsedImplicitly]
		public double YMax { get; private set; }

		private double Width { get; set; }

		private double Height { get; set; }

		public override string ToString()
		{
			return string.Format("{0} x {1}", Width, Height);
		}
	}
}
