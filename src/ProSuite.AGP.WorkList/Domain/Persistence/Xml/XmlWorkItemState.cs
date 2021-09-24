using System.Xml.Serialization;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class XmlWorkItemState : IWorkItemState
	{
		[UsedImplicitly]
		public XmlWorkItemState() { }

		[UsedImplicitly]
		public XmlWorkItemState(long oid, bool visited, WorkItemStatus status, XmlGdbRowIdentity row)
		{
			OID = oid;
			Visited = visited;
			Status = status;
			Row = row;
		}

		[XmlAttribute("id")]
		public long OID { get; set; }

		[XmlAttribute("visited")]
		public bool Visited { get; set; }

		[XmlAttribute("status")]
		public WorkItemStatus Status { get; set; }

		[XmlElement("Row")]
		public XmlGdbRowIdentity Row { get; set; }

		// todo daro: replace with UUID as workspace id?
		[XmlAttribute("connectionString")]
		public string ConnectionString { get; set; }
	}
}
