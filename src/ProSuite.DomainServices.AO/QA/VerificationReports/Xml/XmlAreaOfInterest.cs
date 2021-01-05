using System.ComponentModel;
using System.Xml.Serialization;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.Xml;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlAreaOfInterest
	{
		public XmlAreaOfInterest() { }

		public XmlAreaOfInterest([NotNull] AreaOfInterest aoi)
		{
			Assert.ArgumentNotNull(aoi, nameof(aoi));

			Type = aoi.IsEmpty
				       ? AreaOfInterestType.Empty
				       : aoi.Geometry is IPolygon
					       ? AreaOfInterestType.Polygon
					       : AreaOfInterestType.Box;

			Description = GetValue(aoi.Description);

			FeatureSource = GetValue(aoi.FeatureSource);
			WhereClause = GetValue(aoi.WhereClause);

			BufferDistance = aoi.BufferDistance;
			GeneralizationTolerance = aoi.GeneralizationTolerance;

			if (aoi.ClipExtent != null && ! aoi.ClipExtent.IsEmpty)
			{
				ClipExtent = new Xml2DEnvelope(aoi.ClipExtent);
			}

			Extent = aoi.IsEmpty
				         ? null
				         : new Xml2DEnvelope(aoi.Extent);
		}

		[XmlAttribute("type")]
		public AreaOfInterestType Type { get; set; }

		[XmlAttribute("description")]
		[CanBeNull]
		public string Description { get; set; }

		[XmlAttribute("featureSource")]
		[CanBeNull]
		public string FeatureSource { get; set; }

		[XmlAttribute("whereClause")]
		[CanBeNull]
		public string WhereClause { get; set; }

		[XmlAttribute("bufferDistance")]
		[DefaultValue(0)]
		public double BufferDistance { get; set; }

		[XmlAttribute("generalizationTolerance")]
		[DefaultValue(0)]
		public double GeneralizationTolerance { get; set; }

		[XmlElement("Extent")]
		[CanBeNull]
		public Xml2DEnvelope Extent { get; set; }

		[XmlElement("ClipExtent")]
		[CanBeNull]
		public Xml2DEnvelope ClipExtent { get; set; }

		[CanBeNull]
		private static string GetValue([CanBeNull] string text)
		{
			return StringUtils.IsNullOrEmptyOrBlank(text) ? null : text.Trim();
		}
	}
}
