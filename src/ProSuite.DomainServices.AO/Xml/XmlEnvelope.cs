using System;
using System.ComponentModel;
using System.Xml.Serialization;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.Xml
{
	public class XmlEnvelope
	{
		private const double _undefinedZ = -99999999;

		[UsedImplicitly]
		public XmlEnvelope() { }

		[CLSCompliant(false)]
		public XmlEnvelope([NotNull] IEnvelope envelope)
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

			if (((IZAware) envelope).ZAware)
			{
				ZMin = envelope.ZMin;
				ZMax = envelope.ZMax;
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

		[XmlAttribute("zmin")]
		[DefaultValue(_undefinedZ)]
		public double ZMin { get; set; } = _undefinedZ;

		[XmlAttribute("zmax")]
		[DefaultValue(_undefinedZ)]
		public double ZMax { get; set; } = _undefinedZ;
	}
}
