using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.AttributeDependencies.Xml
{
	[XmlRoot(Namespace = "urn:EsriDE.ProSuite.AttributeDependencies-1.0")]
	public class XmlAttributeDependenciesDocument
	{
		private readonly List<XmlAttributeDependency> _attributeDependencies =
			new List<XmlAttributeDependency>();

		[XmlArrayItem("AttributeDependency")]
		[NotNull]
		public List<XmlAttributeDependency> AttributeDependencies
		{
			get { return _attributeDependencies; }
		}
	}
}
