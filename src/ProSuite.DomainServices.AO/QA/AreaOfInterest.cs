using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA
{
	public class AreaOfInterest
	{
		[CLSCompliant(false)]
		public AreaOfInterest([NotNull] IGeometry geometry,
		                      [CanBeNull] string description = null,
		                      [CanBeNull] string featureSource = null,
		                      [CanBeNull] string whereClause = null,
		                      double bufferDistance = 0,
		                      double generalizationTolerance = 0,
		                      [CanBeNull] IEnvelope clipExtent = null)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentCondition(
				geometry.GeometryType == esriGeometryType.esriGeometryEnvelope ||
				geometry.GeometryType == esriGeometryType.esriGeometryPolygon,
				"geometry must be either null, envelope or polygon");

			Geometry = geometry;
			ClipExtent = clipExtent;
			Description = description;
			BufferDistance = bufferDistance;
			GeneralizationTolerance = generalizationTolerance;
			FeatureSource = featureSource;
			WhereClause = whereClause;

			IsEmpty = geometry.IsEmpty;

			Extent = IsEmpty
				         ? new EnvelopeClass()
				         : geometry.Envelope;
		}

		public bool IsEmpty { get; }

		[CLSCompliant(false)]
		[NotNull]
		public IGeometry Geometry { get; }

		[CLSCompliant(false)]
		[NotNull]
		public IPolygon CreatePolygon()
		{
			var polygon = Geometry as IPolygon;
			if (polygon != null)
			{
				return GeometryFactory.Clone(polygon);
			}

			var envelope = Geometry as IEnvelope;
			if (envelope != null)
			{
				return GeometryFactory.CreatePolygon(envelope);
			}

			throw new ArgumentException(
				"AOI geometry must be either polygon or envelope");
		}

		[CLSCompliant(false)]
		[CanBeNull]
		public IEnvelope ClipExtent { get; }

		[CanBeNull]
		public string Description { get; }

		[CLSCompliant(false)]
		[NotNull]
		public IEnvelope Extent { get; }

		public double BufferDistance { get; }

		public double GeneralizationTolerance { get; }

		[CanBeNull]
		public string FeatureSource { get; }

		[CanBeNull]
		public string WhereClause { get; }
	}
}
