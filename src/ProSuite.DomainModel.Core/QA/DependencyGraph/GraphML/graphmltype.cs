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
	[XmlType(TypeName = "graphml.type",
	         Namespace = "http://graphml.graphdrawing.org/xmlns")]
	[XmlRoot("graphml", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class graphmltype
	{
		private string descField;

		private keytype[] keyField;

		private object[] itemsField;

		/// <remarks/>
		public string desc
		{
			get { return descField; }
			set { descField = value; }
		}

		/// <remarks/>
		[XmlElement("key")]
		public keytype[] key
		{
			get { return keyField; }
			set { keyField = value; }
		}

		/// <remarks/>
		[XmlElement("data", typeof(datatype))]
		[XmlElement("graph", typeof(graphtype))]
		public object[] Items
		{
			get { return itemsField; }
			set { itemsField = value; }
		}
	}
}