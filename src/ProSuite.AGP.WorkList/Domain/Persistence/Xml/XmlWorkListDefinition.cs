using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	[XmlRoot("workListDefinition")]
	public class XmlWorkListDefinition : IWorkListDefinition<XmlWorkItemState>
	{
		[XmlElement("xmlFile")]
		public string Path { get; set; }

		// todo daro: has to be a list
		[XmlElement("issueGeodatabase")]
		public string GeodatabasePath { get; set; }

		[XmlArray("workItems")]
		[XmlArrayItem(typeof(XmlWorkItemState), ElementName = "workItem")]
		public List<XmlWorkItemState> Items { get; set; }
	}
}
