using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml;

[XmlRoot("WorkListDefinition")]
public class XmlWorkListDefinition : IWorkListDefinition<XmlWorkItemState>
{
	public string Name { get; set; }

	[XmlElement("DisplayName")]
	public string DisplayName { get; set; }

	[XmlElement("Type")]
	public string TypeName { get; set; }

	// TODO: (daro) not needed anymore. drop!
	[XmlElement("Assembly")]
	public string AssemblyName { get; set; }

	[XmlElement("XmlFile")]
	public string Path { get; set; }

	[XmlArray("Workspaces")]
	[XmlArrayItem(typeof(XmlWorkListWorkspace), ElementName = "Workspace")]
	public List<XmlWorkListWorkspace> Workspaces { get; set; }

	[XmlElement("CurrentItemIndex")]
	public int CurrentIndex { get; set; }

	[XmlArray("Items")]
	[XmlArrayItem(typeof(XmlWorkItemState), ElementName = "Item")]
	public List<XmlWorkItemState> Items { get; set; }

	[XmlElement("Extent")]
	public string Extent { get; set; }

	// TODO: AreaOfInterest?: Polygon (and move to JSON)
}
