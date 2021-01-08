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
	[XmlType(TypeName = "endpoint.type",
	         Namespace = "http://graphml.graphdrawing.org/xmlns")]
	[XmlRoot("endpoint", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class endpointtype
	{
		private string descField;

		private string idField;

		private string portField;

		private string nodeField;

		private endpointtypetype typeField;

		public endpointtype()
		{
			typeField = endpointtypetype.undir;
		}

		/// <remarks/>
		public string desc
		{
			get { return descField; }
			set { descField = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string id
		{
			get { return idField; }
			set { idField = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string port
		{
			get { return portField; }
			set { portField = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string node
		{
			get { return nodeField; }
			set { nodeField = value; }
		}

		/// <remarks/>
		[XmlAttribute]
		[DefaultValue(endpointtypetype.undir)]
		public endpointtypetype type
		{
			get { return typeField; }
			set { typeField = value; }
		}
	}
}