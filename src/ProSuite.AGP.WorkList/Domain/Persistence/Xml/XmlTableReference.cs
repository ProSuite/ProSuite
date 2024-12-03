using System.Xml.Serialization;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class XmlTableReference
	{
		public XmlTableReference() { }

		public XmlTableReference(long id, string name)
		{
			Id = id;
			Name = name;
		}

		[XmlAttribute("tableId")]
		public long Id { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("definitionQuery")]
		public string DefinitionQuery { get; set; }

		[XmlAttribute("statusFieldName")]
		public string StatusFieldName { get; set; }

		[XmlAttribute("statusValueTodo")]
		public int StatusValueTodo { get; set; }

		[XmlAttribute("statusValueDone")]
		public int StatusValueDone { get; set; }
	}
}
