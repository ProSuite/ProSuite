extern alias EsriGeometry;
using ProSuite.ArcGIS.Geometry.AO;

namespace ESRI.ArcGIS.Geometry
{
	public class ArcEnvelope : IEnvelope
	{
		private readonly EsriGeometry::ESRI.ArcGIS.Geometry.IEnvelope _aoEnvelope;

		public ArcEnvelope(EsriGeometry::ESRI.ArcGIS.Geometry.IEnvelope envelope)
		{
			_aoEnvelope = envelope;
		}

		public EsriGeometry::ESRI.ArcGIS.Geometry.IEnvelope AoEnvelope => _aoEnvelope;

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => (esriGeometryType)_aoEnvelope.GeometryType;

		public esriGeometryDimension Dimension => (esriGeometryDimension)_aoEnvelope.Dimension;

		public ISpatialReference SpatialReference
		{
			get => new ArcSpatialReference(_aoEnvelope.SpatialReference);
			set => _aoEnvelope.SpatialReference = ((ArcSpatialReference)value).AoSpatialReference;
		}

		public bool IsEmpty => _aoEnvelope.IsEmpty;

		public void SetEmpty()
		{
			_aoEnvelope.SetEmpty();
		}

		public void QueryEnvelope(IEnvelope outEnvelope)
		{
			outEnvelope.XMin = _aoEnvelope.XMin;
			outEnvelope.XMax = _aoEnvelope.XMax;
			outEnvelope.YMin = _aoEnvelope.YMin;
			outEnvelope.YMax = _aoEnvelope.YMax;
		}

		public IEnvelope Envelope => new ArcEnvelope(_aoEnvelope.Envelope);

		public void Project(ISpatialReference newReferenceSystem)
		{
			var newAoSpatialRef = ((ArcSpatialReference)newReferenceSystem).AoSpatialReference;

			_aoEnvelope.Project(newAoSpatialRef);
		}

		public void SnapToSpatialReference()
		{
			_aoEnvelope.SnapToSpatialReference();
		}

		public void GeoNormalize()
		{
			_aoEnvelope.GeoNormalize();
		}

		public void GeoNormalizeFromLongitude(double longitude)
		{
			_aoEnvelope.GeoNormalizeFromLongitude(longitude);
		}

		#endregion

		#region Implementation of IEnvelope

		public double Width
		{
			get => _aoEnvelope.Width;
			set => _aoEnvelope.Width = value;
		}

		public double Height
		{
			get => _aoEnvelope.Height;
			set => _aoEnvelope.Height = value;
		}

		public double Depth
		{
			get => _aoEnvelope.Depth;
			set => _aoEnvelope.Depth = value;
		}

		public IPoint LowerLeft
		{
			get => new ArcPoint(_aoEnvelope.LowerLeft);
			set => _aoEnvelope.LowerLeft = ((ArcPoint)value).AoPoint;
		}

		public IPoint UpperLeft
		{
			get => new ArcPoint(_aoEnvelope.UpperLeft);
			set => _aoEnvelope.UpperLeft = ((ArcPoint)value).AoPoint;
		}

		public IPoint UpperRight
		{
			get => new ArcPoint(_aoEnvelope.UpperRight);
			set => _aoEnvelope.UpperRight = ((ArcPoint)value).AoPoint;
		}

		public IPoint LowerRight
		{
			get => new ArcPoint(_aoEnvelope.LowerRight);
			set => _aoEnvelope.LowerRight = ((ArcPoint)value).AoPoint;
		}

		public double XMin
		{
			get => _aoEnvelope.XMin;
			set => _aoEnvelope.XMin = value;
		}

		public double YMin
		{
			get => _aoEnvelope.YMin;
			set => _aoEnvelope.YMin = value;
		}

		public double XMax
		{
			get => _aoEnvelope.XMax;
			set => _aoEnvelope.XMax = value;
		}

		public double YMax
		{
			get => _aoEnvelope.YMax;
			set => _aoEnvelope.YMax = value;
		}

		public double MMin
		{
			get => _aoEnvelope.MMin;
			set => _aoEnvelope.MMin = value;
		}

		public double MMax
		{
			get => _aoEnvelope.MMax;
			set => _aoEnvelope.MMax = value;
		}

		public double ZMin
		{
			get => _aoEnvelope.ZMin;
			set => _aoEnvelope.ZMin = value;
		}

		public double ZMax
		{
			get => _aoEnvelope.ZMax;
			set => _aoEnvelope.ZMax = value;
		}

		public void Union(IEnvelope inEnvelope)
		{
			var aoEnvelope = ((ArcEnvelope)inEnvelope).AoEnvelope;

			_aoEnvelope.Union(aoEnvelope);
		}

		public void Intersect(IEnvelope inEnvelope)
		{
			var aoEnvelope = ((ArcEnvelope)inEnvelope).AoEnvelope;

			_aoEnvelope.Intersect(aoEnvelope);
		}

		public void Offset(double x, double y)
		{
			_aoEnvelope.Offset(x, y);
		}

		public void OffsetZ(double z)
		{
			_aoEnvelope.OffsetZ(z);
		}

		public void OffsetM(double m)
		{
			_aoEnvelope.OffsetM(m);
		}

		public void Expand(double dx, double dy, bool asRatio)
		{
			_aoEnvelope.Expand(dx, dy, asRatio);
		}

		public void ExpandZ(double dz, bool asRatio)
		{
			_aoEnvelope.ExpandZ(dz, asRatio);
		}

		public void ExpandM(double dm, bool asRatio)
		{
			_aoEnvelope.ExpandM(dm, asRatio);
		}

		//public void DefineFromPoints(int count, ref IPoint points)
		//{
		//	_aoEnvelope.DefineFromPoints(count, ref points);
		//}

		public void PutCoords(double xMin, double yMin, double xMax, double yMax)
		{
			_aoEnvelope.PutCoords(xMin, yMin, xMax, yMax);
		}

		public void QueryCoords(out double xMin, out double yMin, out double xMax, out double yMax)
		{
			_aoEnvelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);
		}

		public void CenterAt(IPoint p)
		{
			EsriGeometry::ESRI.ArcGIS.Geometry.IPoint aoPoint = ((ArcPoint)p).AoPoint;

			_aoEnvelope.CenterAt(aoPoint);
		}

		#endregion
	}
}
