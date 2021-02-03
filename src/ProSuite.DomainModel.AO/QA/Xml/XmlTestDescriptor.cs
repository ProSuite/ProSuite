using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlTestDescriptor : IXmlEntityMetadata
	{
		private string _description;
		private const int _nullExecutionPriority = -1;

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("Description")]
		[DefaultValue(null)]
		public string Description
		{
			get
			{
				return string.IsNullOrEmpty(_description)
					       ? null
					       : _description;
			}
			set { _description = value; }
		}

		[XmlAttribute("executionPriority")]
		[DefaultValue(_nullExecutionPriority)]
		public int ExecutionPriority { get; set; }

		[XmlAttribute("stopOnError")]
		[DefaultValue(false)]
		public bool StopOnError { get; set; }

		[XmlAttribute("allowErrors")]
		[DefaultValue(false)]
		public bool AllowErrors { get; set; }

		[XmlElement("TestClass")]
		[CanBeNull]
		public XmlClassDescriptor TestClass { get; set; }

		[XmlElement("TestFactory")]
		[CanBeNull]
		public XmlClassDescriptor TestFactoryDescriptor { get; set; }

		[XmlElement("TestConfigurator")]
		[CanBeNull]
		public XmlClassDescriptor TestConfigurator { get; set; }

		[XmlAttribute("createdDate")]
		public string CreatedDate { get; set; }

		[XmlAttribute("createdByUser")]
		public string CreatedByUser { get; set; }

		[XmlAttribute("lastChangedDate")]
		public string LastChangedDate { get; set; }

		[XmlAttribute("lastChangedByUser")]
		public string LastChangedByUser { get; set; }

		public int? GetExecutionPriority()
		{
			return ExecutionPriority == _nullExecutionPriority
				       ? (int?) null
				       : ExecutionPriority;
		}

		public void SetExecutionPriority(int? executionPriority)
		{
			ExecutionPriority = executionPriority == null
				                    ? _nullExecutionPriority
				                    : executionPriority.Value;
		}
	}
}