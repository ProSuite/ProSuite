using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlWorkspace
	{
		[XmlAttribute("id")]
		public string ID { get; set; }

		[XmlAttribute("modelName")]
		public string ModelName { get; set; }

		[XmlAttribute("factoryProgId")]
		public string FactoryProgId { get; set; }

		[XmlAttribute("connectionString")]
		public string ConnectionString { get; set; }

		[XmlAttribute("catalogPath")]
		public string CatalogPath { get; set; }

		[XmlAttribute("database")]
		public string Database { get; set; }

		[XmlAttribute("schemaOwner")]
		public string SchemaOwner { get; set; }
	}
}
