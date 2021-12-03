using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Xml;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public class XmlCartoProcessGroup
	{
		private string _description;

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("associatedCommandId")]
		public string AssociatedCommandId { get; set; }

		[XmlAttribute("associatedCommandSubType")]
		[DefaultValue(0)]
		public int AssociatedCommandSubType { get; set; }

		[XmlAttribute("associatedCommandIcon")]
		public string AssociatedCommandIcon { get; set; }

		[XmlElement("Description")]
		[DefaultValue(null)]
		public string Description
		{
			get
			{
				return string.IsNullOrEmpty(_description)
					       ? null
					       : _description;
			}
			set { _description = value; }
		}

		public XmlNamedEntity AssociatedGroupProcessTypeReference { get; set; }

		[XmlArrayItem("Process")]
		public List<XmlNamedEntity> Processes { get; } = new List<XmlNamedEntity>();
	}
}
