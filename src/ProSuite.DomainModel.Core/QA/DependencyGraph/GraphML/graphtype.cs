using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph.GraphML
{
	/// <remarks/>
	[GeneratedCode("xsd", "4.0.30319.33440")]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(TypeName = "graph.type", Namespace = "http://graphml.graphdrawing.org/xmlns"
		)
	]
	[XmlRoot("graph", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class graphtype
	{
		private string descField;

		private object[] itemsField;

		private string idField;

		private graphedgedefaulttype edgedefaultField;

		/// <remarks/>
		public string desc
		{
			get { return descField; }
			set { descField = value; }
		}

		/// <remarks/>
		[XmlElement("data", typeof(datatype))]
		[XmlElement("edge", typeof(edgetype))]
		[XmlElement("hyperedge", typeof(hyperedgetype))]
		[XmlElement("locator", typeof(locatortype))]
		[XmlElement("node", typeof(nodetype))]
		public object[] Items
		{
			get { return itemsField; }
			set { itemsField = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string id
		{
			get { return idField; }
			set { idField = value; }
		}

		/// <remarks/>
		[XmlAttribute]
		public graphedgedefaulttype edgedefault
		{
			get { return edgedefaultField; }
			set { edgedefaultField = value; }
		}
	}
}