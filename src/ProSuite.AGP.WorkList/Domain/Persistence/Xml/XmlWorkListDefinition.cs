using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	[XmlRoot("WorkListDefinition")]
	public class XmlWorkListDefinition : IWorkListDefinition<XmlWorkItemState>
	{
		public string Name { get; set; }

		[XmlElement("Type")]
		public string TypeName { get; set; }

		[XmlElement("Assembly")]
		public string AssemblyName { get; set; }

		[XmlElement("XmlFile")]
		public string Path { get; set; }

		// TODO: AreaOfInterest: Polygon (and move to JSON)

		[XmlArray("Workspaces")]
		[XmlArrayItem(typeof(XmlWorkListWorkspace), ElementName = "Workspace")]
		public List<XmlWorkListWorkspace> Workspaces { get; set; }

		[XmlElement("CurrentItemIndex")]
		public int CurrentIndex { get; set; }

		[XmlArray("Items")]
		[XmlArrayItem(typeof(XmlWorkItemState), ElementName = "Item")]
		public List<XmlWorkItemState> Items { get; set; }
	}
}
