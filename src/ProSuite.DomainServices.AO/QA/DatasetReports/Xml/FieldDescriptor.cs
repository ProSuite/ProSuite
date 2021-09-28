using System.ComponentModel;
using System.Xml.Serialization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	public class FieldDescriptor
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("aliasName")]
		public string AliasName { get; set; }

		[XmlAttribute("type")]
		public esriFieldType Type { get; set; }

		[XmlAttribute("length")]
		[DefaultValue(0)]
		public int Length { get; set; }

		[XmlAttribute("domainName")]
		public string DomainName { get; set; }

		[XmlAttribute("scale")]
		[DefaultValue(0)]
		public int Scale { get; set; }

		[XmlAttribute("precision")]
		[DefaultValue(0)]
		public int Precision { get; set; }

		[XmlAttribute("isNullable")]
		public bool IsNullable { get; set; }

		[XmlAttribute("editable")]
		public bool Editable { get; set; }

		[CanBeNull]
		[XmlElement("Statistics")]
		public FieldStatistics Statistics { get; set; }
	}
}
