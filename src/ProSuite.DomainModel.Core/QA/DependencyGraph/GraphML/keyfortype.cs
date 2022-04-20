using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph.GraphML
{
	/// <remarks/>
	[GeneratedCode("xsd", "4.0.30319.33440")]
	[Serializable]
	[XmlType(TypeName = "key.for.type",
	         Namespace = "http://graphml.graphdrawing.org/xmlns")]
	public enum keyfortype
	{
		/// <remarks/>
		all,

		/// <remarks/>
		graphml,

		/// <remarks/>
		graph,

		/// <remarks/>
		node,

		/// <remarks/>
		edge,

		/// <remarks/>
		hyperedge,

		/// <remarks/>
		port,

		/// <remarks/>
		endpoint
	}
}
