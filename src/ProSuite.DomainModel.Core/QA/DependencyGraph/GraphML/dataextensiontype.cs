using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph.GraphML
{
	/// <remarks/>
	[XmlInclude(typeof(defaulttype))]
	[XmlInclude(typeof(datatype))]
	[GeneratedCode("xsd", "4.0.30319.33440")]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(TypeName = "data-extension.type",
	         Namespace = "http://graphml.graphdrawing.org/xmlns")]
	public class dataextensiontype
	{
		private string[] textField;

		/// <remarks/>
		[XmlText]
		public string[] Text
		{
			get { return textField; }
			set { textField = value; }
		}
	}
}
