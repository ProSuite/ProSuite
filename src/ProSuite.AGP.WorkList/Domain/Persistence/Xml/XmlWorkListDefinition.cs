using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	[XmlRoot("workListDefinition")]
	public class XmlWorkListDefinition : IWorkListDefinition<XmlWorkItemState>
	{
		public string Name { get; set; }

		[XmlElement("XmlFile")]
		public string Path { get; set; }

		[XmlArray("Workspaces")]
		[XmlArrayItem(typeof(XmlWorkListWorkspace), ElementName = "Workspace")]
		public List<XmlWorkListWorkspace> Workspaces { get; set; }

		[XmlArray("Items")]
		[XmlArrayItem(typeof(XmlWorkItemState), ElementName = "Item")]
		public List<XmlWorkItemState> Items { get; set; }
	}
}