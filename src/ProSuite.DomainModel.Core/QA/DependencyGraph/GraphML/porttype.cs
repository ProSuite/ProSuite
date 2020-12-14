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
	[XmlType(TypeName = "port.type", Namespace = "http://graphml.graphdrawing.org/xmlns")
	]
	[XmlRoot("port", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class porttype
	{
		private string descField;

		private object[] itemsField;

		private string nameField;

		/// <remarks/>
		public string desc
		{
			get { return descField; }
			set { descField = value; }
		}

		/// <remarks/>
		[XmlElement("data", typeof(datatype))]
		[XmlElement("port", typeof(porttype))]
		public object[] Items
		{
			get { return itemsField; }
			set { itemsField = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string name
		{
			get { return nameField; }
			set { nameField = value; }
		}
	}
}