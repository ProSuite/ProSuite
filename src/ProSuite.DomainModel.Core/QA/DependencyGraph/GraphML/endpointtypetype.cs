using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph.GraphML
{
	/// <remarks/>
	[GeneratedCode("xsd", "4.0.30319.33440")]
	[Serializable]
	[XmlType(TypeName = "endpoint.type.type",
	         Namespace = "http://graphml.graphdrawing.org/xmlns")]
	public enum endpointtypetype
	{
		/// <remarks/>
		@in,

		/// <remarks/>
		@out,

		/// <remarks/>
		undir
	}
}