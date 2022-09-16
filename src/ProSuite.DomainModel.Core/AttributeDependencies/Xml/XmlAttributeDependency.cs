using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;

namespace ProSuite.DomainModel.Core.AttributeDependencies.Xml
{
	public class XmlAttributeDependency
	{
		private readonly List<XmlAttribute> _sourceAttributes = new List<XmlAttribute>();

		private readonly List<XmlAttribute> _targetAttributes = new List<XmlAttribute>();

		private readonly List<XmlAttributeValueMapping> _attributeValueMappings =
			new List<XmlAttributeValueMapping>();

		public XmlNamedEntity ModelReference { get; set; }

		[XmlAttribute("dataset")]
		public string Dataset { get; set; }

		[XmlArrayItem("SourceAttribute")]
		[NotNull]
		public List<XmlAttribute> SourceAttributes
		{
			get { return _sourceAttributes; }
		}

		[XmlArrayItem("TargetAttribute")]
		[NotNull]
		public List<XmlAttribute> TargetAttributes
		{
			get { return _targetAttributes; }
		}

		[XmlArrayItem("AttributeValueMapping")]
		[NotNull]
		public List<XmlAttributeValueMapping> AttributeValueMappings
		{
			get { return _attributeValueMappings; }
		}
	}
}
