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
	[XmlType(TypeName = "hyperedge.type",
	         Namespace = "http://graphml.graphdrawing.org/xmlns")]
	[XmlRoot("hyperedge", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class hyperedgetype
	{
		private string descField;

		private object[] itemsField;

		private graphtype graphField;

		private string idField;

		/// <remarks/>
		public string desc
		{
			get { return descField; }
			set { descField = value; }
		}

		/// <remarks/>
		[XmlElement("data", typeof(datatype))]
		[XmlElement("endpoint", typeof(endpointtype))]
		public object[] Items
		{
			get { return itemsField; }
			set { itemsField = value; }
		}

		/// <remarks/>
		public graphtype graph
		{
			get { return graphField; }
			set { graphField = value; }
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
