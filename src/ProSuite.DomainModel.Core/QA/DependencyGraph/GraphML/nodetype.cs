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
	[XmlType(TypeName = "node.type", Namespace = "http://graphml.graphdrawing.org/xmlns")
	]
	[XmlRoot("node", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class nodetype
	{
		private string descField;

		private object[] itemsField;

		private string idField;

		/// <remarks/>
		public string desc
		{
			get { return descField; }
			set { descField = value; }
		}

		/// <remarks/>
		[XmlElement("data", typeof(datatype))]
		[XmlElement("graph", typeof(graphtype))]
		[XmlElement("locator", typeof(locatortype))]
		[XmlElement("port", typeof(porttype))]
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
	}
}
