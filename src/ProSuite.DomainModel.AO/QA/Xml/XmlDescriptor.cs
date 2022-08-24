using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlDescriptor : IXmlEntityMetadata
	{
		private string _description;

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("Description")]
		[DefaultValue(null)]
		public string Description
		{
			get => string.IsNullOrEmpty(_description)
				       ? null
				       : _description;
			set => _description = value;
		}

		[XmlAttribute("createdDate")]
		public string CreatedDate { get; set; }

		[XmlAttribute("createdByUser")]
		public string CreatedByUser { get; set; }

		[XmlAttribute("lastChangedDate")]
		public string LastChangedDate { get; set; }

		[XmlAttribute("lastChangedByUser")]
		public string LastChangedByUser { get; set; }

		[XmlIgnore]
		public XmlClassDescriptor ClassDescriptor { get; protected set; }
	}
}
