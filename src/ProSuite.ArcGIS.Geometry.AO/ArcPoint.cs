extern alias EsriGeometry;
using System;
using ESRI.ArcGIS.Geometry;
using EnvelopeClass = EsriGeometry::ESRI.ArcGIS.Geometry.EnvelopeClass;


namespace ProSuite.ArcGIS.Geometry.AO
{
	public class ArcPoint : IPoint
	{
		public ArcPoint(EsriGeometry::ESRI.ArcGIS.Geometry.IPoint aoPoint)
		{
			AoPoint = aoPoint;
		}

		public EsriGeometry::ESRI.ArcGIS.Geometry.IPoint AoPoint { get; set; }

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => (esriGeometryType)AoPoint.GeometryType;

		public esriGeometryDimension Dimension => (esriGeometryDimension)AoPoint.Dimension;

		public ISpatialReference SpatialReference
		{
			get => new ArcSpatialReference(AoPoint.SpatialReference);
			set => AoPoint.SpatialReference = ((ArcSpatialReference)value).AoSpatialReference;
		}

		public bool IsEmpty => AoPoint.IsEmpty;

		void IGeometry.SetEmpty()
		{
			AoPoint.SetEmpty();
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
			AoPoint.SetEmpty();
		}

		public IEnvelope Envelope => new ArcEnvelope(AoPoint.Envelope);

		void IGeometry.Project(ISpatialReference newReferenceSystem)
		{
			throw new NotImplementedException();
			//_aoPoint.Project(newReferenceSystem);
		}

		void IPoint.SnapToSpatialReference()
		{
			AoPoint.SnapToSpatialReference();
		}

		void IPoint.GeoNormalize()
		{
			AoPoint.GeoNormalize();
		}

		void IPoint.GeoNormalizeFromLongitude(double longitude)
		{
			AoPoint.GeoNormalizeFromLongitude(longitude);
		}

		public void QueryCoords(out double x, out double y)
		{
			AoPoint.QueryCoords(out x, out y);
		}

		public void PutCoords(double x, double y)
		{
			AoPoint.PutCoords(x, y);
		}

		public double X
		{
			get => AoPoint.X;
			set => AoPoint.X = value;
		}

		public double Y
		{
			get => AoPoint.Y;
			set => AoPoint.Y = value;
		}

		public double Z
		{
			get => AoPoint.Z;
			set => AoPoint.Z = value;
		}

		public double M
		{
			get => AoPoint.M;
			set => AoPoint.M = value;
		}

		public int ID
		{
			get => AoPoint.ID;
			set => AoPoint.ID = value;
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
			AoPoint.Project(((ArcSpatialReference)newReferenceSystem).AoSpatialReference);
		}

		void IGeometry.SnapToSpatialReference()
		{
			AoPoint.SnapToSpatialReference();
		}

		void IGeometry.GeoNormalize()
		{
			AoPoint.GeoNormalize();
		}

		void IGeometry.GeoNormalizeFromLongitude(double longitude)
		{
			AoPoint.GeoNormalizeFromLongitude(longitude);
		}

		#endregion
	}
}
