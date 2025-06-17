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
		public XmlWorkItemState(long oid, bool visited, WorkItemStatus status,
		                        XmlGdbRowIdentity row)
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

		[XmlElement("XMin")]
		public double XMin { get; set; }
		[XmlElement("XMax")]
		public double XMax { get; set; }
		[XmlElement("YMin")]
		public double YMin { get; set; }
		[XmlElement("YMax")]
		public double YMax { get; set; }

		// todo daro: replace with UUID as workspace id?
		[XmlAttribute("connectionString")]
		public string ConnectionString { get; set; }

		public override string ToString()
		{
			return $"item id={OID}, row oid={Row.OID}, {Row.TableName}, {Status}, {Visited}";
		}
	}
}
