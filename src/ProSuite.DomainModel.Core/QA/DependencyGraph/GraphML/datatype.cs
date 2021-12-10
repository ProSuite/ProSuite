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
	[XmlType(TypeName = "data.type", Namespace = "http://graphml.graphdrawing.org/xmlns")
	]
	[XmlRoot("data", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class datatype : dataextensiontype
	{
		private string keyField;

		private long timeField;

		private string idField;

		public datatype()
		{
			timeField = 0;
		}

		/// <remarks/>
		[XmlAttribute(DataType = "NMTOKEN")]
		public string key
		{
			get { return keyField; }
			set { keyField = value; }
		}

		/// <remarks/>
		[XmlAttribute]
		[DefaultValue(typeof(long), "0")]
		public long time
		{
			get { return timeField; }
			set { timeField = value; }
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
