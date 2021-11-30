using System.Xml.Serialization;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Xml
{
	/// <summary>
	/// Xml-serialization for <see cref="INamed"/> implementation.
	/// Usage:
	/// <code>SerializableClass.MyXmlTag = new XmlNamedEntity(INamed)</code>
	/// resulting in:
	/// <![CDATA[ <MyXmlTag name="INamed.Name"/> ]]>
	/// </summary>
	public class XmlNamedEntity
	{
		// Need a parameter-less constructor for de-serialization
		public XmlNamedEntity() { }

		public XmlNamedEntity([NotNull] INamed named)
		{
			Assert.ArgumentNotNull(named, nameof(named));

			Name = named.Name;
		}

		[XmlAttribute("name")]
		public string Name { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}
