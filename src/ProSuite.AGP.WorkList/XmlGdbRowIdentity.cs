using System.Xml.Serialization;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList
{
	public class XmlGdbRowIdentity
	{
		public XmlGdbRowIdentity() { }

		public XmlGdbRowIdentity(GdbRowIdentity row)
		{
			OID = row.ObjectId;
			TableId = row.Table.Id;
			TableName = row.Table.Name;
		}

		[XmlAttribute("oid")]
		public long OID { get; set; }

		[XmlAttribute("tableId")]
		public long TableId { get; set; }

		[XmlAttribute("tableName")]
		public string TableName { get; set; }
	}
}
