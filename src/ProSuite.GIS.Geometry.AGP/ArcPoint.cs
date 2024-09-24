using System;
using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcPoint : IPoint
	{
		public ArcPoint(MapPoint proPoint)
		{
			ProPoint = proPoint;
		}

		public MapPoint ProPoint { get; set; }

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => GetProGeometryType();

		private esriGeometryType GetProGeometryType()
		{
			switch (ProPoint.GeometryType)
			{
				case ArcGIS.Core.Geometry.GeometryType.Unknown:
					return esriGeometryType.esriGeometryAny;
				case ArcGIS.Core.Geometry.GeometryType.Point:
					return esriGeometryType.esriGeometryPoint;
				case ArcGIS.Core.Geometry.GeometryType.Envelope:
					return esriGeometryType.esriGeometryEnvelope;
				case ArcGIS.Core.Geometry.GeometryType.Multipoint:
					return esriGeometryType.esriGeometryMultipoint;
				case ArcGIS.Core.Geometry.GeometryType.Polyline:
					return esriGeometryType.esriGeometryPolyline;
				case ArcGIS.Core.Geometry.GeometryType.Polygon:
					return esriGeometryType.esriGeometryPolygon;
				case ArcGIS.Core.Geometry.GeometryType.Multipatch:
					return esriGeometryType.esriGeometryMultiPatch;
				case ArcGIS.Core.Geometry.GeometryType.GeometryBag:
					return esriGeometryType.esriGeometryBag;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public esriGeometryDimension Dimension
		{
			get
			{
				switch (ProPoint.Dimension)
				{
					case 0:
						return esriGeometryDimension.esriGeometry0Dimension;
					case 1:
						return esriGeometryDimension.esriGeometry1Dimension;
					case 2:
						return esriGeometryDimension.esriGeometry2Dimension;
					case 3:
						return esriGeometryDimension.esriGeometry3Dimension;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public ISpatialReference SpatialReference
		{
			get => new ArcSpatialReference(ProPoint.SpatialReference);
			set => throw new NotImplementedException();
		}

		public bool IsEmpty => ProPoint.IsEmpty;

		void IGeometry.SetEmpty()
		{
			throw new NotImplementedException();
		}

		public void QueryEnvelope(IEnvelope outEnvelope)
		{
			outEnvelope.XMin = ProPoint.X;
			outEnvelope.XMax = ProPoint.X;
			outEnvelope.YMin = ProPoint.Y;
			outEnvelope.YMax = ProPoint.Y;
		}

		void IPoint.SetEmpty()
		{
			throw new NotImplementedException();
		}

		public IEnvelope Envelope => new ArcEnvelope(ProPoint.Extent);

		void IGeometry.Project(ISpatialReference newReferenceSystem)
		{
			throw new NotImplementedException();
			//_aoPoint.Project(newReferenceSystem);
		}

		void IPoint.SnapToSpatialReference()
		{
			throw new NotImplementedException();
		}

		void IPoint.GeoNormalize()
		{
			throw new NotImplementedException();
		}

		void IPoint.GeoNormalizeFromLongitude(double longitude)
		{
			throw new NotImplementedException();
		}

		public IGeometry Clone()
		{
			return new ArcPoint((MapPoint) ProPoint.Clone());
		}

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

		//public void ConstrainDistance(double constraintRadius, IPoint anchor)
		//{
		//	AoPoint.ConstrainDistance(constraintRadius, anchor);
		//}

		//public void ConstrainAngle(double constraintAngle, IPoint anchor, bool allowOpposite)
		//{
		//	AoPoint.ConstrainAngle(constraintAngle, anchor, allowOpposite);
		//}

		//public int Compare(IPoint otherPoint)
		//{
		//	return AoPoint.Compare(otherPoint);
		//}

		void IPoint.Project(ISpatialReference newReferenceSystem)
		{
			throw new NotImplementedException();
		}

		void IGeometry.SnapToSpatialReference()
		{
			throw new NotImplementedException();
		}

		void IGeometry.GeoNormalize()
		{
			throw new NotImplementedException();
		}

		void IGeometry.GeoNormalizeFromLongitude(double longitude)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
