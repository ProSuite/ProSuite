using System;
using ArcGIS.Core.Geometry;
using ProSuite.ArcGIS.Geometry.AO;

namespace ESRI.ArcGIS.Geometry
{
	public class ArcEnvelope : IEnvelope
	{
		private readonly Envelope _proEnvelope;

		public ArcEnvelope(Envelope envelope)
		{
			_proEnvelope = envelope;
		}

		public Envelope ProEnvelope => _proEnvelope;

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => (esriGeometryType) _proEnvelope.GeometryType;

		public esriGeometryDimension Dimension => (esriGeometryDimension) _proEnvelope.Dimension;

		public ISpatialReference SpatialReference
		{
			get => new ArcSpatialReference(_proEnvelope.SpatialReference);
			set => throw new NotImplementedException();
		}

		public bool IsEmpty => _proEnvelope.IsEmpty;

		public void SetEmpty()
		{
			throw new NotImplementedException();
		}

		public void QueryEnvelope(IEnvelope outEnvelope)
		{
			outEnvelope.XMin = _proEnvelope.XMin;
			outEnvelope.XMax = _proEnvelope.XMax;
			outEnvelope.YMin = _proEnvelope.YMin;
			outEnvelope.YMax = _proEnvelope.YMax;
		}

		public IEnvelope Envelope => new ArcEnvelope(_proEnvelope.Extent);

		public void Project(ISpatialReference newReferenceSystem)
		{
			var newProSpatialRef = ((ArcSpatialReference) newReferenceSystem).ProSpatialReference;

			GeometryEngine.Instance.Project(_proEnvelope, newProSpatialRef);
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

		public IGeometry Clone()
		{
			Envelope clone = (Envelope) _proEnvelope.Clone();
			return new ArcEnvelope(clone);
		}

		#endregion

		#region Implementation of IEnvelope

		public double Width
		{
			get => _proEnvelope.Width;
			set => throw new NotImplementedException();
		}

		public double Height
		{
			get => _proEnvelope.Height;
			set => throw new NotImplementedException();
		}

		public double Depth
		{
			get => _proEnvelope.Depth;
			set => throw new NotImplementedException();
		}

		public IPoint LowerLeft
		{
			get => new ArcPoint(MapPointBuilderEx.CreateMapPoint(
				                    _proEnvelope.XMin, _proEnvelope.YMin,
				                    _proEnvelope.SpatialReference));
			set => throw new NotImplementedException();
		}

		public IPoint UpperLeft
		{
			get => new ArcPoint(MapPointBuilderEx.CreateMapPoint(
				                    _proEnvelope.XMin, _proEnvelope.YMax,
				                    _proEnvelope.SpatialReference));
			set => throw new NotImplementedException();
		}

		public IPoint UpperRight
		{
			get => new ArcPoint(MapPointBuilderEx.CreateMapPoint(
				                    _proEnvelope.XMax, _proEnvelope.YMax,
				                    _proEnvelope.SpatialReference));
			set => throw new NotImplementedException();
		}

		public IPoint LowerRight
		{
			get => new ArcPoint(MapPointBuilderEx.CreateMapPoint(
				                    _proEnvelope.XMax, _proEnvelope.YMin,
				                    _proEnvelope.SpatialReference));
			set => throw new NotImplementedException();
		}

		public double XMin
		{
			get => _proEnvelope.XMin;
			set => throw new NotImplementedException();
		}

		public double YMin
		{
			get => _proEnvelope.YMin;
			set => throw new NotImplementedException();
		}

		public double XMax
		{
			get => _proEnvelope.XMax;
			set => throw new NotImplementedException();
		}

		public double YMax
		{
			get => _proEnvelope.YMax;
			set => throw new NotImplementedException();
		}

		public double MMin
		{
			get => _proEnvelope.MMin;
			set => throw new NotImplementedException();
		}

		public double MMax
		{
			get => _proEnvelope.MMax;
			set => throw new NotImplementedException();
		}

		public double ZMin
		{
			get => _proEnvelope.ZMin;
			set => throw new NotImplementedException();
		}

		public double ZMax
		{
			get => _proEnvelope.ZMax;
			set => throw new NotImplementedException();
		}

		public void Union(IEnvelope inEnvelope)
		{
			var aoEnvelope = ((ArcEnvelope) inEnvelope).ProEnvelope;

			Envelope result = _proEnvelope.Union(aoEnvelope);

			// TODO: Change semantics, return result
			throw new NotImplementedException();
		}

		public void Intersect(IEnvelope inEnvelope)
		{
			var aoEnvelope = ((ArcEnvelope) inEnvelope).ProEnvelope;

			Envelope result = _proEnvelope.Intersection(aoEnvelope);

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