using System.ComponentModel;
using System.Xml.Serialization;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.Xml
{
	public class Xml2DEnvelope
	{
		[UsedImplicitly]
		public Xml2DEnvelope() { }

		public Xml2DEnvelope([NotNull] IEnvelope envelope)
		{
			Assert.ArgumentNotNull(envelope, nameof(envelope));
			Assert.ArgumentCondition(! envelope.IsEmpty, "envelope is empty");

			double xMin;
			double yMin;
			double xMax;
			double yMax;
			envelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;

			if (envelope.SpatialReference != null)
			{
				CoordinateSystem = envelope.SpatialReference.Name;
				CoordinateSystemId = envelope.SpatialReference.FactoryCode;
			}
		}

		[XmlAttribute("xmin")]
		public double XMin { get; set; }

		[XmlAttribute("ymin")]
		public double YMin { get; set; }

		[XmlAttribute("xmax")]
		public double XMax { get; set; }

		[XmlAttribute("ymax")]
		public double YMax { get; set; }

		[CanBeNull]
		[XmlAttribute("coordinateSystem")]
		public string CoordinateSystem { get; set; }

		[DefaultValue(0)]
		[XmlAttribute("coordinateSystemId")]
		public int CoordinateSystemId { get; set; }

		[NotNull]
		public IEnvelope CreateEnvelope()
		{
			ISpatialReference spatialReference =
				CoordinateSystemId > 0
					? SpatialReferenceUtils.CreateSpatialReference(
						CoordinateSystemId,
						setDefaultXyDomain: true)
					: null;

			return GeometryFactory.CreateEnvelope(XMin, YMin, XMax, YMax,
			                                      spatialReference);
		}
	}
}
