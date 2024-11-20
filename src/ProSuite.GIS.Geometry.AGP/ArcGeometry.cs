using System;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public abstract class ArcGeometry : IGeometry, IZAware, IMAware
	{
		public static IGeometry Create(ArcGIS.Core.Geometry.Geometry proGeometry)
		{
			if (proGeometry == null)
			{
				return null;
			}

			if (proGeometry is Polygon polygon)
			{
				return new ArcPolygon(polygon);
			}

			if (proGeometry is Polyline polyline)
			{
				return new ArcPolyline(polyline);
			}

			if (proGeometry is Multipoint multipoint)
			{
				return new ArcMultipoint(multipoint);
			}

			if (proGeometry is Multipatch multipatch)
			{
				return new ArcMultipatch(multipatch);
			}

			if (proGeometry is MapPoint point)
			{
				return new ArcPoint(point);
			}

			throw new ArgumentException("Unsupported geometry type: " + proGeometry.GeometryType);
		}

		protected ArcGeometry([NotNull] ArcGIS.Core.Geometry.Geometry proGeometry)
		{
			Assert.ArgumentNotNull(proGeometry, nameof(proGeometry));

			ProGeometry = proGeometry;
		}

		public ArcGIS.Core.Geometry.Geometry ProGeometry { get; set; }

		#region Implementation of IGeometry

		public esriGeometryType GeometryType => GetProGeometryType();

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

			outEnvelope.ZMin = ZAware ? ProGeometry.Extent.ZMin : double.NaN;
			outEnvelope.ZMax = ZAware ? ProGeometry.Extent.ZMax : double.NaN;

			outEnvelope.MMin = MAware ? ProGeometry.Extent.MMin : double.NaN;
			outEnvelope.MMax = MAware ? ProGeometry.Extent.MMax : double.NaN;

			outEnvelope.SpatialReference = SpatialReference;
		}

		public void SnapToSpatialReference()
		{
			throw new NotImplementedException();
		}

		public abstract IGeometry Clone();

		#endregion

		#region Implementation of IZAware

		public bool ZAware
		{
			get => ProGeometry.HasZ;
			set => throw new NotImplementedException();
		}

		public bool ZSimple
		{
			get
			{
				double zMin = double.NaN;
				double zMax = double.NaN;

				SpatialReference?.GetZDomain(out zMin, out zMax);

				foreach (MapPoint mapPoint in GeometryUtils.GetVertices(ProGeometry))
				{
					double z = mapPoint.Z;

					if (double.IsNaN(z))
					{
						return false;
					}

					if (z < zMin || z > zMax)
					{
						return false;
					}
				}

				return true;
			}
		}

		#endregion

		#region Implementation of IMAware

		public bool MAware
		{
			get => ProGeometry.HasM;
			set => throw new NotImplementedException();
		}

		public bool MSimple
		{
			get
			{
				double mMin = double.NaN;
				double mMax = double.NaN;

				SpatialReference?.GetMDomain(out mMin, out mMax);

				foreach (MapPoint mapPoint in GeometryUtils.GetVertices(ProGeometry))
				{
					double m = mapPoint.M;

					if (double.IsNaN(m))
					{
						return false;
					}

					if (m < mMin || m > mMax)
					{
						return false;
					}
				}

				return true;
			}
		}

		#endregion

		private esriGeometryType GetProGeometryType()
		{
			switch (ProGeometry.GeometryType)
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
	}
}
