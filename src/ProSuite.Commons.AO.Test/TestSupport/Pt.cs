using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	/// <summary>
	/// Simple point geometry class, capable of returning a full IPoint instance.
	/// </summary>
	public class Pt
	{
		#region Constructors

		public Pt(double x, double y)
		{
			X = x;
			Y = y;
		}

		public Pt(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public Pt(double x, double y, double z, double m)
		{
			X = x;
			Y = y;
			Z = z;
			M = m;
		}

		#endregion

		public double X { get; }

		public double Y { get; }

		public double Z { get; } = double.NaN;

		public double M { get; } = double.NaN;

		public IPoint CreatePoint()
		{
			IPoint point = new PointClass();
			ConfigurePoint(point);

			return point;
		}

		public void ConfigurePoint(IPoint point)
		{
			point.X = X;
			point.Y = Y;

			if (! double.IsNaN(Z))
			{
				((IZAware) point).ZAware = true;
				point.Z = Z;
			}

			if (! double.IsNaN(M))
			{
				((IMAware) point).MAware = true;
				point.M = M;
			}
		}
	}
}
