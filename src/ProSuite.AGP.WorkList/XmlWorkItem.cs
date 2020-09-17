using System.Xml.Serialization;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public interface IWorkItemState
	{
		int OID { get; set; }
		bool Visited { get; set; }
		WorkItemStatus Status { get; set; }
	}

	[XmlRoot("xmlWorkItem")]
	public class XmlWorkItem : IWorkItemState
	{
		[UsedImplicitly]
		public XmlWorkItem() { }

		[UsedImplicitly]
		public XmlWorkItem(int oid, bool visited, WorkItemStatus status, XmlGdbRowIdentity row)
		{
			OID = oid;
			Visited = visited;
			Status = status;
			Row = row;
		}

		[XmlAttribute("id")]
		public int OID { get; set; }

		[XmlAttribute("visited")]
		public bool Visited { get; set; }

		[XmlAttribute("status")]
		public WorkItemStatus Status { get; set; }

		[XmlElement("row")]
		public XmlGdbRowIdentity Row { get; set; }
	}
}
