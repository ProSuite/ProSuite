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
	[XmlType(TypeName = "key.type", Namespace = "http://graphml.graphdrawing.org/xmlns")]
	[XmlRoot("key", Namespace = "http://graphml.graphdrawing.org/xmlns",
	         IsNullable = false)]
	public class keytype
	{
		private string descField;

		private defaulttype defaultField;

		private string idField;

		private bool dynamicField;

		private keyfortype forField;

		public keytype()
		{
			dynamicField = false;
			forField = keyfortype.all;
		}

		[XmlAttribute("attr.name")]
		public string Name { get; set; }

		[XmlAttribute("attr.type")]
		public string Type { get; set; }

		/// <remarks/>
		public string desc
		{
			get { return descField; }
			set { descField = value; }
		}

		/// <remarks/>
		public defaulttype @default
		{
			get { return defaultField; }
			set { defaultField = value; }
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
		[DefaultValue(false)]
		public bool dynamic
		{
			get { return dynamicField; }
			set { dynamicField = value; }
		}

		/// <remarks/>
		[XmlAttribute]
		[DefaultValue(keyfortype.all)]
		public keyfortype @for
		{
			get { return forField; }
			set { forField = value; }
		}
	}
}
