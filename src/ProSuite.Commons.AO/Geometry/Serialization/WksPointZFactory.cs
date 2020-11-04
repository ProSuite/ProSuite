using System;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Geometry.Wkb;

namespace ProSuite.Commons.AO.Geometry.Serialization
{
	[CLSCompliant(false)]
	public class WksPointZFactory : IPointFactory<WKSPointZ>
	{
		public WKSPointZ CreatePointXy(double x, double y)
		{
			return new WKSPointZ
			       {
				       X = x,
				       Y = y,
				       Z = double.NaN
			       };
		}

		public WKSPointZ CreatePointXyz(double x, double y, double z)
		{
			return new WKSPointZ
			       {
				       X = x,
				       Y = y,
				       Z = z
			       };
		}

		public WKSPointZ CreatePointXym(double x, double y, double m)
		{
			throw new NotImplementedException();
		}

		public WKSPointZ CreatePointXyzm(double x, double y, double z, double m)
		{
			throw new NotImplementedException();
		}
	}
}
