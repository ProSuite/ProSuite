using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlHtmlReportDataQualityCategoryOptions
	{
		[XmlAttribute("categoryUuid")]
		public string CategoryUuid { get; set; }

		[XmlAttribute("ignoreCategoryLevel")]
		[DefaultValue(false)]
		public bool IgnoreCategoryLevel { get; set; }

		[CanBeNull]
		[XmlAttribute("aliasName")]
		[DefaultValue(null)]
		public string AliasName { get; set; }
	}
}
