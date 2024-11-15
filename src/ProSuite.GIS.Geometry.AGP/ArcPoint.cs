using System;
using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcPoint : ArcGeometry, IPoint
	{
		public ArcPoint(MapPoint proPoint) : base(proPoint)
		{
			ProPoint = proPoint;
		}

		public MapPoint ProPoint { get; set; }

		#region Implementation of IGeometry

		public void QueryCoords(out double x, out double y)
		{
			x = ProPoint.X;
			y = ProPoint.Y;
		}

		public void PutCoords(double x, double y)
		{
			throw new NotImplementedException();
		}

		public double X
		{
			get => ProPoint.X;
			set => throw new NotImplementedException();
		}

		public double Y
		{
			get => ProPoint.Y;
			set => throw new NotImplementedException();
		}

		public double Z
		{
			get => ProPoint.Z;
			set => throw new NotImplementedException();
		}

		public double M
		{
			get => ProPoint.M;
			set => throw new NotImplementedException();
		}

		public int ID
		{
			get => ProPoint.ID;
			set => throw new NotImplementedException();
		}

		public double GetDistance(IPoint otherPoint, bool in3d)
		{
			var otherCoordinate = new Coordinate2D(otherPoint.X, otherPoint.Y);

			MapPoint mapPoint =
				MapPointBuilderEx.CreateMapPoint(otherCoordinate, ProPoint.SpatialReference);

			return GeometryEngine.Instance.Distance(ProPoint, mapPoint);
		}

		public bool EqualsInXy(IPoint other, double tolerance)
		{
			double distance = GetDistance(other, false);

			return distance <= tolerance;
		}

		#endregion

		#region Overrides of ArcGeometry

		public override IGeometry Clone()
		{
			return new ArcPoint((MapPoint) ProPoint.Clone());
		}

		#endregion
	}
}
