using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class XmlWorkListWorkspace
	{
		[XmlAttribute("path")]
		public string Path { get; set; }

		[XmlArrayItem(typeof(XmlTableReference), ElementName = "Table")]
		public List<XmlTableReference> Tables { get; set; }
	}
}
