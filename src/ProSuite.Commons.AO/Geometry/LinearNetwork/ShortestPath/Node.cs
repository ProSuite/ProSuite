using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork.ShortestPath
{
	public class Node
	{
		public Node(IPoint point, ISpatialReference spatialReference = null)
			: this(point.X, point.Y, spatialReference ?? point.SpatialReference) { }

		public Node(double x, double y, ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			double originX, originY;
			spatialReference.GetDomain(out originX, out _, out originY, out _);

			double scale = 1 / SpatialReferenceUtils.GetXyResolution(spatialReference);

			X = SnapCoordinate(x, originX, scale);
			Y = SnapCoordinate(y, originY, scale);
		}

		public Node(double x, double y,
		            double originX, double originY, double xyResolution)
		{
			double scale = 1 / xyResolution;

			// NOTE: The nubers are still not nicely rounded but the difference is below the epsilon
			X = SnapCoordinate(x, originX, scale);
			Y = SnapCoordinate(y, originY, scale);
		}

		public double X { get; }
		public double Y { get; }

		protected bool Equals(Node other)
		{
			return X.Equals(other.X) && Y.Equals(other.Y);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;

			return Equals((Node) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (X.GetHashCode() * 397) ^ Y.GetHashCode();
			}
		}

		public override string ToString()
		{
			return $"{X} | {Y}";
		}

		private static double SnapCoordinate(double coordinateValue, double origin,
		                                     double scale)
		{
			// go through int to improve rounding issues, consider using only the decimal as X,Y (might have a little performance benefit)
			var cellCount = (long) Math.Round((coordinateValue - origin) * scale);

			return cellCount / scale + origin;
		}
	}
}
