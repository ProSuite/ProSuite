using System;

namespace ProSuite.Commons.Geometry.Wkb
{
	public class PntFactory : IPointFactory<IPnt>
	{
		public IPnt CreatePointXy(double x, double y)
		{
			return new Pnt3D(x, y, double.NaN);
		}

		public IPnt CreatePointXyz(double x, double y, double z)
		{
			return new Pnt3D(x, y, z);
		}

		public IPnt CreatePointXym(double x, double y, double m)
		{
			throw new NotImplementedException();
		}

		public IPnt CreatePointXyzm(double x, double y, double z, double m)
		{
			throw new NotImplementedException();
		}
	}
}
