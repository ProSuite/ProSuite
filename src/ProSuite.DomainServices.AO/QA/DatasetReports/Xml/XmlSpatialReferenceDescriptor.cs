using System.Globalization;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.Xml;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	public class XmlSpatialReferenceDescriptor
	{
		[XmlElement("WKT")]
		[UsedImplicitly]
		public string WellKnownText { get; set; }

		[XmlElement("XYCoordinateSystem")]
		[UsedImplicitly]
		public string XyCoordinateSystem { get; set; }

		[XmlElement("IsHighPrecision")]
		[UsedImplicitly]
		public bool IsHighPrecision { get; set; }

		[XmlElement("XYTolerance")]
		[UsedImplicitly]
		public string XyToleranceFormatted { get; set; }

		[XmlElement("XYResolution")]
		[UsedImplicitly]
		public string XyResolutionFormatted { get; set; }

		[XmlElement("XYDomain")]
		[UsedImplicitly]
		public Xml2DEnvelope XyDomain { get; set; }

		[XmlElement("ZTolerance")]
		[UsedImplicitly]
		public string ZToleranceFormatted { get; set; }

		[XmlElement("ZResolution")]
		[UsedImplicitly]
		public string ZResolutionFormatted { get; set; }

		[XmlElement("ZDomain")]
		[UsedImplicitly]
		public XmlRange ZDomain { get; set; }

		[XmlElement("MTolerance")]
		[UsedImplicitly]
		public string MToleranceFormatted { get; set; }

		[XmlElement("MResolution")]
		[UsedImplicitly]
		public string MResolutionFormatted { get; set; }

		[XmlElement("MDomain")]
		[UsedImplicitly]
		public XmlRange MDomain { get; set; }

		[XmlIgnore]
		public double XyTolerance
		{
			get { return Parse(XyToleranceFormatted); }
			set { XyToleranceFormatted = Format(value); }
		}

		[XmlIgnore]
		public double XyResolution
		{
			get { return Parse(XyResolutionFormatted); }
			set { XyResolutionFormatted = Format(value); }
		}

		[XmlIgnore]
		public double ZTolerance
		{
			get { return Parse(ZToleranceFormatted); }
			set { ZToleranceFormatted = Format(value); }
		}

		[XmlIgnore]
		public double ZResolution
		{
			get { return Parse(ZResolutionFormatted); }
			set { ZResolutionFormatted = Format(value); }
		}

		[XmlIgnore]
		public double MTolerance
		{
			get { return Parse(MToleranceFormatted); }
			set { MToleranceFormatted = Format(value); }
		}

		[XmlIgnore]
		public double MResolution
		{
			get { return Parse(MResolutionFormatted); }
			set { MResolutionFormatted = Format(value); }
		}

		[NotNull]
		private static string Format(double value)
		{
			return StringUtils.FormatPreservingDecimalPlaces(value,
			                                                 CultureInfo.InvariantCulture);
		}

		private static double Parse([CanBeNull] string formatted)
		{
			return string.IsNullOrEmpty(formatted)
				       ? 0
				       : double.Parse(formatted, NumberStyles.Any, CultureInfo.InvariantCulture);
		}
	}
}
