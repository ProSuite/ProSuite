using System;
using ArcGIS.Core.Geometry;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.ArcGIS.Geometry.AO
{
	public class ArcPoint : IPoint
	{
		public ArcPoint(MapPoint aoPoint)
		{
			AoPoint = aoPoint;
		}

		public MapPoint AoPoint { get; set; }

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => GetProGeometryType();

		private esriGeometryType GetProGeometryType()
		{
			switch (AoPoint.GeometryType)
			{
				case global::ArcGIS.Core.Geometry.GeometryType.Unknown:
					return esriGeometryType.esriGeometryAny;
				case global::ArcGIS.Core.Geometry.GeometryType.Point:
					return esriGeometryType.esriGeometryPoint;
				case global::ArcGIS.Core.Geometry.GeometryType.Envelope:
					return esriGeometryType.esriGeometryEnvelope;
				case global::ArcGIS.Core.Geometry.GeometryType.Multipoint:
					return esriGeometryType.esriGeometryMultipoint;
				case global::ArcGIS.Core.Geometry.GeometryType.Polyline:
					return esriGeometryType.esriGeometryPolyline;
				case global::ArcGIS.Core.Geometry.GeometryType.Polygon:
					return esriGeometryType.esriGeometryPolygon;
				case global::ArcGIS.Core.Geometry.GeometryType.Multipatch:
					return esriGeometryType.esriGeometryMultiPatch;
				case global::ArcGIS.Core.Geometry.GeometryType.GeometryBag:
					return esriGeometryType.esriGeometryBag;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public esriGeometryDimension Dimension
		{
			get
			{
				switch (AoPoint.Dimension)
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
			get => new ArcSpatialReference(AoPoint.SpatialReference);
			set => throw new NotImplementedException();
		}

		public bool IsEmpty => AoPoint.IsEmpty;

		void IGeometry.SetEmpty()
		{
			throw new NotImplementedException();
		}

		public void QueryEnvelope(IEnvelope outEnvelope)
		{
			outEnvelope.XMin = AoPoint.X;
			outEnvelope.XMax = AoPoint.X;
			outEnvelope.YMin = AoPoint.Y;
			outEnvelope.YMax = AoPoint.Y;
		}

		void IPoint.SetEmpty()
		{
			throw new NotImplementedException();
		}

		public IEnvelope Envelope => new ArcEnvelope(AoPoint.Extent);

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

		public void QueryCoords(out double x, out double y)
		{
			x = AoPoint.X;
			y = AoPoint.Y;
		}

		public void PutCoords(double x, double y)
		{
			throw new NotImplementedException();
		}

		public double X
		{
			get => AoPoint.X;
			set => throw new NotImplementedException();
		}

		public double Y
		{
			get => AoPoint.Y;
			set => throw new NotImplementedException();
		}

		public double Z
		{
			get => AoPoint.Z;
			set => throw new NotImplementedException();
		}

		public double M
		{
			get => AoPoint.M;
			set => throw new NotImplementedException();
		}

		public int ID
		{
			get => AoPoint.ID;
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
