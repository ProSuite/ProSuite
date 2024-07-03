using System;
using ProSuite.ArcGIS.Geometry.AO;

namespace ESRI.ArcGIS.Geometry
{
	public class ArcGeometry : IGeometry
	{
		public ArcGeometry(global::ArcGIS.Core.Geometry.Geometry aoGeometry)
		{
			AoGeometry = aoGeometry;
		}

		public global::ArcGIS.Core.Geometry.Geometry AoGeometry { get; set; }

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => (esriGeometryType) AoGeometry.GeometryType;

		public esriGeometryDimension Dimension => (esriGeometryDimension) AoGeometry.Dimension;

		public ISpatialReference SpatialReference
		{
			get => new ArcSpatialReference(AoGeometry.SpatialReference);
			set => throw new NotImplementedException();
		}

		public bool IsEmpty => AoGeometry.IsEmpty;

		public IEnvelope Envelope => new ArcEnvelope(AoGeometry.Extent);

		public void SetEmpty()
		{
			throw new NotImplementedException();
		}

		public void QueryEnvelope(IEnvelope outEnvelope)
		{
			outEnvelope.XMin = AoGeometry.Extent.XMin;
			outEnvelope.XMax = AoGeometry.Extent.XMax;
			outEnvelope.YMin = AoGeometry.Extent.YMin;
			outEnvelope.YMax = AoGeometry.Extent.YMax;
		}

		public void Project(ISpatialReference newReferenceSystem)
		{
			throw new NotImplementedException();
		}

		public void SnapToSpatialReference()
		{
			throw new NotImplementedException();
		}

		public void GeoNormalize()
		{
			throw new NotImplementedException();
		}

		public void GeoNormalizeFromLongitude(double Longitude)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
