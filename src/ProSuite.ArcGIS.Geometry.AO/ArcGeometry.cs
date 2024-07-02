extern alias EsriGeometry;
using ProSuite.ArcGIS.Geometry.AO;

namespace ESRI.ArcGIS.Geometry
{
	public class ArcGeometry : IGeometry
	{
		public ArcGeometry(EsriGeometry::ESRI.ArcGIS.Geometry.IGeometry aoGeometry)
		{
			AoGeometry = aoGeometry;
		}

		public EsriGeometry::ESRI.ArcGIS.Geometry.IGeometry AoGeometry { get; set; }

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => (esriGeometryType)AoGeometry.GeometryType;

		public esriGeometryDimension Dimension => (esriGeometryDimension)AoGeometry.Dimension;

		public ISpatialReference SpatialReference
		{
			get => new ArcSpatialReference(AoGeometry.SpatialReference);
			set => AoGeometry.SpatialReference = ((ArcSpatialReference)value).AoSpatialReference;
		}

		public bool IsEmpty => AoGeometry.IsEmpty;

		public IEnvelope Envelope => new ArcEnvelope(AoGeometry.Envelope);

		public void SetEmpty()
		{
			AoGeometry.SetEmpty();
		}

		public void QueryEnvelope(IEnvelope outEnvelope)
		{
			outEnvelope.XMin = AoGeometry.Envelope.XMin;
			outEnvelope.XMax = AoGeometry.Envelope.XMax;
			outEnvelope.YMin = AoGeometry.Envelope.YMin;
			outEnvelope.YMax = AoGeometry.Envelope.YMax;
		}

		public void Project(ISpatialReference newReferenceSystem)
		{
			var aoSpatialReference = ((ArcSpatialReference)newReferenceSystem).AoSpatialReference;
			AoGeometry.Project(aoSpatialReference);
		}

		public void SnapToSpatialReference()
		{
			AoGeometry.SnapToSpatialReference();
		}

		public void GeoNormalize()
		{
			AoGeometry.GeoNormalize();
		}

		public void GeoNormalizeFromLongitude(double Longitude)
		{
			AoGeometry.GeoNormalizeFromLongitude(Longitude);
		}

		#endregion
	}
}
