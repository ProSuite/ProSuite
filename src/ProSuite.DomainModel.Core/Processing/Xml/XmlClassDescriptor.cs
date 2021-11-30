using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public class XmlClassDescriptor
	{
		private string _description;

		[XmlAttribute("type")]
		public string TypeName { get; set; }

		[XmlAttribute("assembly")]
		public string AssemblyName { get; set; }

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
	}
}
