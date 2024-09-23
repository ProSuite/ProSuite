using System;
using ProSuite.ArcGIS.Geometry.AO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ESRI.ArcGIS.Geometry
{
	public class ArcGeometry : IGeometry
	{
		public ArcGeometry([NotNull] global::ArcGIS.Core.Geometry.Geometry proGeometry)
		{
			Assert.ArgumentNotNull(proGeometry, nameof(proGeometry));

			ProGeometry = proGeometry;
		}

		public global::ArcGIS.Core.Geometry.Geometry ProGeometry { get; set; }

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => (esriGeometryType) ProGeometry.GeometryType;

		public esriGeometryDimension Dimension => (esriGeometryDimension) ProGeometry.Dimension;

		public ISpatialReference SpatialReference
		{
			get => new ArcSpatialReference(ProGeometry.SpatialReference);
			set => throw new NotImplementedException();
		}

		public bool IsEmpty => ProGeometry.IsEmpty;

		public IEnvelope Envelope => new ArcEnvelope(ProGeometry.Extent);

		public void SetEmpty()
		{
			throw new NotImplementedException();
		}

		public void QueryEnvelope(IEnvelope outEnvelope)
		{
			outEnvelope.XMin = ProGeometry.Extent.XMin;
			outEnvelope.XMax = ProGeometry.Extent.XMax;
			outEnvelope.YMin = ProGeometry.Extent.YMin;
			outEnvelope.YMax = ProGeometry.Extent.YMax;
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

		public IGeometry Clone()
		{
			return new ArcGeometry(ProGeometry.Clone());
		}

		#endregion
	}
}
