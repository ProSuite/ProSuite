using System;
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

	/// <summary>
	/// Not set and not read anymore. The element is kept so that existing work list files
	/// can still be read.
	/// </summary>
	[Obsolete("The index depends on the order of the loaded items and cannot be restored. " +
	          "Use CurrentItem instead.")]
	[XmlElement("CurrentItemIndex")]
	public int CurrentIndex { get; set; }

	/// <summary>
	/// The current item. In contrast to the deprecated CurrentIndex it identifies the item
	/// independently of the order in which the items are loaded.
	/// </summary>
	[XmlElement("CurrentItem")]
	public XmlGdbRowIdentity CurrentItem { get; set; }

	[XmlArray("Items")]
	[XmlArrayItem(typeof(XmlWorkItemState), ElementName = "Item")]
	public List<XmlWorkItemState> Items { get; set; }

	[XmlElement("Extent")]
	public string Extent { get; set; }

	// TODO: AreaOfInterest?: Polygon (and move to JSON)
}
