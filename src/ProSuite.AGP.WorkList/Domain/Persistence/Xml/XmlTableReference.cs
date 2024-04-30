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

		// todo daro: could be non distinct, we need smth more robust
		[XmlAttribute("tableId")]
		public long Id { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("definitionQuery")]
		public string DefinitionQuery { get; set; }
	}
}
