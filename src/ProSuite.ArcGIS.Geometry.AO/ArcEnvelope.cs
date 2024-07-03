using System;
using ArcGIS.Core.Geometry;
using ProSuite.ArcGIS.Geometry.AO;

namespace ESRI.ArcGIS.Geometry
{
	public class ArcEnvelope : IEnvelope
	{
		private readonly Envelope _aoEnvelope;

		public ArcEnvelope(Envelope envelope)
		{
			_aoEnvelope = envelope;
		}

		public Envelope AoEnvelope => _aoEnvelope;

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => (esriGeometryType) _aoEnvelope.GeometryType;

		public esriGeometryDimension Dimension => (esriGeometryDimension) _aoEnvelope.Dimension;

		public ISpatialReference SpatialReference
		{
			get => new ArcSpatialReference(_aoEnvelope.SpatialReference);
			set => throw new NotImplementedException();
		}

		public bool IsEmpty => _aoEnvelope.IsEmpty;

		public void SetEmpty()
		{
			throw new NotImplementedException();
		}

		public void QueryEnvelope(IEnvelope outEnvelope)
		{
			outEnvelope.XMin = _aoEnvelope.XMin;
			outEnvelope.XMax = _aoEnvelope.XMax;
			outEnvelope.YMin = _aoEnvelope.YMin;
			outEnvelope.YMax = _aoEnvelope.YMax;
		}

		public IEnvelope Envelope => new ArcEnvelope(_aoEnvelope.Extent);

		public void Project(ISpatialReference newReferenceSystem)
		{
			var newProSpatialRef = ((ArcSpatialReference) newReferenceSystem).ProSpatialReference;

			GeometryEngine.Instance.Project(_aoEnvelope, newProSpatialRef);
		}

		public void SnapToSpatialReference()
		{
			throw new NotImplementedException();
		}

		public void GeoNormalize()
		{
			throw new NotImplementedException();
		}

		public void GeoNormalizeFromLongitude(double longitude)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Implementation of IEnvelope

		public double Width
		{
			get => _aoEnvelope.Width;
			set => throw new NotImplementedException();
		}

		public double Height
		{
			get => _aoEnvelope.Height;
			set => throw new NotImplementedException();
		}

		public double Depth
		{
			get => _aoEnvelope.Depth;
			set => throw new NotImplementedException();
		}

		public IPoint LowerLeft
		{
			get => new ArcPoint(MapPointBuilderEx.CreateMapPoint(
				                    _aoEnvelope.XMin, _aoEnvelope.YMin,
				                    _aoEnvelope.SpatialReference));
			set => throw new NotImplementedException();
		}

		public IPoint UpperLeft
		{
			get => new ArcPoint(MapPointBuilderEx.CreateMapPoint(
				                    _aoEnvelope.XMin, _aoEnvelope.YMax,
				                    _aoEnvelope.SpatialReference));
			set => throw new NotImplementedException();
		}

		public IPoint UpperRight
		{
			get => new ArcPoint(MapPointBuilderEx.CreateMapPoint(
				                    _aoEnvelope.XMax, _aoEnvelope.YMax,
				                    _aoEnvelope.SpatialReference));
			set => throw new NotImplementedException();
		}

		public IPoint LowerRight
		{
			get => new ArcPoint(MapPointBuilderEx.CreateMapPoint(
				                    _aoEnvelope.XMax, _aoEnvelope.YMin,
				                    _aoEnvelope.SpatialReference));
			set => throw new NotImplementedException();
		}

		public double XMin
		{
			get => _aoEnvelope.XMin;
			set => throw new NotImplementedException();
		}

		public double YMin
		{
			get => _aoEnvelope.YMin;
			set => throw new NotImplementedException();
		}

		public double XMax
		{
			get => _aoEnvelope.XMax;
			set => throw new NotImplementedException();
		}

		public double YMax
		{
			get => _aoEnvelope.YMax;
			set => throw new NotImplementedException();
		}

		public double MMin
		{
			get => _aoEnvelope.MMin;
			set => throw new NotImplementedException();
		}

		public double MMax
		{
			get => _aoEnvelope.MMax;
			set => throw new NotImplementedException();
		}

		public double ZMin
		{
			get => _aoEnvelope.ZMin;
			set => throw new NotImplementedException();
		}

		public double ZMax
		{
			get => _aoEnvelope.ZMax;
			set => throw new NotImplementedException();
		}

		public void Union(IEnvelope inEnvelope)
		{
			var aoEnvelope = ((ArcEnvelope) inEnvelope).AoEnvelope;

			Envelope result = _aoEnvelope.Union(aoEnvelope);

			// TODO: Change semantics, return result
			throw new NotImplementedException();
		}

		public void Intersect(IEnvelope inEnvelope)
		{
			var aoEnvelope = ((ArcEnvelope) inEnvelope).AoEnvelope;

			Envelope result = _aoEnvelope.Intersection(aoEnvelope);

			// TODO: Change semantics
			throw new NotImplementedException();
		}

		public void Offset(double x, double y)
		{
			throw new NotImplementedException();
		}

		public void OffsetZ(double z)
		{
			throw new NotImplementedException();
		}

		public void OffsetM(double m)
		{
			throw new NotImplementedException();
		}

		public void Expand(double dx, double dy, bool asRatio)
		{
			throw new NotImplementedException();
		}

		public void ExpandZ(double dz, bool asRatio)
		{
			throw new NotImplementedException();
		}

		public void ExpandM(double dm, bool asRatio)
		{
			throw new NotImplementedException();
		}

		//public void DefineFromPoints(int count, ref IPoint points)
		//{
		//	_aoEnvelope.DefineFromPoints(count, ref points);
		//}

		public void PutCoords(double xMin, double yMin, double xMax, double yMax)
		{
			throw new NotImplementedException();
		}

		public void QueryCoords(out double xMin, out double yMin, out double xMax, out double yMax)
		{
			throw new NotImplementedException();
		}

		public void CenterAt(IPoint p)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
