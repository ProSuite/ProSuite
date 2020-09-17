using System.Collections.Generic;
using System.Xml.Serialization;

namespace ProSuite.AGP.WorkList
{
	// todo daro: abstract class WorkListDefinition?
	public interface IWorkListDefinition<T> where T : IWorkItemState
	{
		string Path { get; set; }

		List<T> Items { get; set; }
	}

	[XmlRoot("workListDefinition")]
	public class XmlBasedWorkListDefinition : IWorkListDefinition<XmlWorkItem>
	{
		[XmlElement("xmlFile")]
		public string Path { get; set; }

		[XmlArray("workItems")]
		[XmlArrayItem(typeof(XmlWorkItem), ElementName = "workItem")]
		public List<XmlWorkItem> Items { get; set; }
	}

	public class IssueWorkListDefinition : XmlBasedWorkListDefinition
	{
		[XmlElement("geodatabase")]
		public string IssueGeodatabasePath { get; set; }

	}
}
