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
	[XmlType(TypeName = "edge.type", Namespace = "http://graphml.graphdrawing.org/xmlns")
	]
	[XmlRoot("edge", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class edgetype
	{
		private string descField;

		private datatype[] dataField;

		private graphtype graphField;

		private string idField;

		private bool directedField;

		private bool directedFieldSpecified;

		private string sourceField;

		private string targetField;

		private string sourceportField;

		private string targetportField;

		/// <remarks/>
		public string desc
		{
			get { return descField; }
			set { descField = value; }
		}

		/// <remarks/>
		[XmlElement("data")]
		public datatype[] data
		{
			get { return dataField; }
			set { dataField = value; }
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

		/// <remarks/>
		[XmlAttribute]
		public bool directed
		{
			get { return directedField; }
			set { directedField = value; }
		}

		/// <remarks/>
		[XmlIgnore]
		public bool directedSpecified
		{
			get { return directedFieldSpecified; }
			set { directedFieldSpecified = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string source
		{
			get { return sourceField; }
			set { sourceField = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string target
		{
			get { return targetField; }
			set { targetField = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string sourceport
		{
			get { return sourceportField; }
			set { sourceportField = value; }
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string targetport
		{
			get { return targetportField; }
			set { targetportField = value; }
		}
	}
}