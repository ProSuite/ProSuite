using System.Xml.Serialization;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml;

public class XmlGdbRowIdentity
{
	public XmlGdbRowIdentity() { }

	public XmlGdbRowIdentity(GdbRowIdentity row,
	                         long uniqueTableId)
	{
		OID = row.ObjectId;

		TableName = row.Table.Name;

		TableId = uniqueTableId;
	}

	[XmlAttribute("oid")]
	public long OID { get; set; }

	[XmlAttribute("tableId")]
	public long TableId { get; set; }

	[XmlAttribute("tableName")]
	public string TableName { get; set; }
}
