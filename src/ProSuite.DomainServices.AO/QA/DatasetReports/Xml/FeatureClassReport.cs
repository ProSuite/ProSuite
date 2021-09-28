using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	[XmlRoot("FeatureClassReport")]
	public class FeatureClassReport : ObjectClassReport
	{
		private double? _averageVertexCount;

		private readonly List<FieldDescriptor> _fields = new List<FieldDescriptor>();

		[XmlElement("VertexCount")]
		[UsedImplicitly]
		public int VertexCount { get; set; }

		[XmlElement("MaximumVertexCount")]
		[UsedImplicitly]
		public int MaximumVertexCount { get; set; }

		[XmlElement("AverageVertexCount")]
		[UsedImplicitly]
		public double AverageVertexCount
		{
			get
			{
				if (_averageVertexCount == null)
				{
					_averageVertexCount = RowCount > 0
						                      ? VertexCount / RowCount
						                      : 0;
				}

				return _averageVertexCount.Value;
			}
			set { _averageVertexCount = value; }
		}

		[XmlElement("NullGeometryFeatureCount")]
		[UsedImplicitly]
		public int NullGeometryFeatureCount { get; set; }

		[XmlElement("EmptyGeometryFeatureCount")]
		[UsedImplicitly]
		public int EmptyGeometryFeatureCount { get; set; }

		[XmlElement("MultipartFeatureCount")]
		[UsedImplicitly]
		public int MultipartFeatureCount { get; set; }

		[XmlElement("NonLinearSegmentFeatureCount")]
		[UsedImplicitly]
		public int NonLinearSegmentFeatureCount { get; set; }

		[XmlElement("LinearSegmentCount")]
		[UsedImplicitly]
		public int LinearSegmentCount { get; set; }

		[XmlElement("NonLinearSegmentCount")]
		[UsedImplicitly]
		public int NonLinearSegmentCount { get; set; }

		[XmlElement("CircularArcCount")]
		[UsedImplicitly]
		public int CircularArcCount { get; set; }

		[XmlElement("BezierCount")]
		[UsedImplicitly]
		public int BezierCount { get; set; }

		[XmlElement("EllipticArcCount")]
		[UsedImplicitly]
		public int EllipticArcCount { get; set; }

		[XmlElement("FeatureType")]
		[UsedImplicitly]
		public esriFeatureType FeatureType { get; set; }

		[XmlElement("ShapeType")]
		[UsedImplicitly]
		public esriGeometryType ShapeType { get; set; }

		[XmlElement("HasZ")]
		[UsedImplicitly]
		public bool HasZ { get; set; }

		[XmlElement("HasM")]
		[UsedImplicitly]
		public bool HasM { get; set; }

		[XmlElement("SpatialReference")]
		[UsedImplicitly]
		public XmlSpatialReferenceDescriptor SpatialReference { get; set; }

		[XmlArray("Fields")]
		[XmlArrayItem("Field")]
		[NotNull]
		[UsedImplicitly]
		public List<FieldDescriptor> Fields => _fields;

		#region Overrides of TableReportBase

		public override void AddField(FieldDescriptor fieldDescriptor)
		{
			Assert.ArgumentNotNull(fieldDescriptor, nameof(fieldDescriptor));

			_fields.Add(fieldDescriptor);
		}

		protected override void AddRowCore(IRow row)
		{
			var feature = row as IFeature;
			if (feature == null)
			{
				return;
			}

			IGeometry geometry = feature.Shape;

			int featureVertexCount = GeometryUtils.GetPointCount(geometry);

			// update total vertex count
			VertexCount += featureVertexCount;

			// update maximum vertex count per feature
			MaximumVertexCount = Math.Max(featureVertexCount, MaximumVertexCount);

			_averageVertexCount = null;

			if (geometry == null)
			{
				NullGeometryFeatureCount += 1;
			}
			else if (geometry.IsEmpty)
			{
				EmptyGeometryFeatureCount += 1;
			}
			else if (GeometryProperties.IsMultipart(geometry))
			{
				MultipartFeatureCount += 1;
			}

			var segmentCollection = geometry as ISegmentCollection;
			if (segmentCollection != null)
			{
				if (GeometryUtils.HasNonLinearSegments(segmentCollection))
				{
					NonLinearSegmentFeatureCount += 1;

					SegmentCounts segmentCounts = GeometryProperties.GetSegmentCounts(
						segmentCollection);

					LinearSegmentCount = LinearSegmentCount + segmentCounts.LinearSegmentCount;
					CircularArcCount = CircularArcCount + segmentCounts.CircularArcCount;
					BezierCount = BezierCount + segmentCounts.BezierCount;
					EllipticArcCount = EllipticArcCount + segmentCounts.EllipticArcCount;

					NonLinearSegmentCount = NonLinearSegmentCount +
					                        segmentCounts.CircularArcCount +
					                        segmentCounts.BezierCount +
					                        segmentCounts.EllipticArcCount;
				}
				else
				{
					LinearSegmentCount = LinearSegmentCount + segmentCollection.SegmentCount;
				}
			}
		}

		#endregion
	}
}
