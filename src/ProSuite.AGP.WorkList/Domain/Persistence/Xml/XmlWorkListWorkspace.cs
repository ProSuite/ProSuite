using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml;

public class XmlWorkListWorkspace
{
	[XmlAttribute("workspaceFactory")]
	public string WorkspaceFactory { get; set; }

	[XmlAttribute("connectionString")]
	public string ConnectionString { get; set; }

	[XmlArrayItem(typeof(XmlTableReference), ElementName = "Table")]
	public List<XmlTableReference> Tables { get; set; }
}
