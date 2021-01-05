using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlGdbObjectReference
	{
		private const int _noOID = -1;

		public XmlGdbObjectReference() : this(string.Empty, _noOID) { }

		public XmlGdbObjectReference([NotNull] string tableName) :
			this(tableName, _noOID) { }

		public XmlGdbObjectReference([NotNull] string tableName, int oid)
		{
			TableName = tableName;
			OID = oid;
		}

		[XmlAttribute("table")]
		[NotNull]
		public string TableName { get; set; }

		[XmlAttribute("objectId")]
		[DefaultValue(_noOID)]
		public int OID { get; set; }
	}
}
