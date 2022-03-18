using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlDescriptor : IXmlEntityMetadata
	{
		private string _description;

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

		[XmlAttribute("createdDate")]
		public string CreatedDate { get; set; }

		[XmlAttribute("createdByUser")]
		public string CreatedByUser { get; set; }

		[XmlAttribute("lastChangedDate")]
		public string LastChangedDate { get; set; }

		[XmlAttribute("lastChangedByUser")]
		public string LastChangedByUser { get; set; }

		[XmlIgnore]
		public XmlClassDescriptor ClassDescriptor { get; protected set; }
	}

	public class XmlRowFilterDescriptor : XmlDescriptor
	{
		[XmlElement("RowFilterClass")]
		[CanBeNull]
		public XmlClassDescriptor RowFilterClass
		{
			get => ClassDescriptor;
			set => ClassDescriptor = value;
		}
	}

	public class XmlIssueFilterDescriptor : XmlDescriptor
	{
		[XmlElement("IssueFilterClass")]
		[CanBeNull]
		public XmlClassDescriptor IssueFilterClass
		{
			get => ClassDescriptor;
			set => ClassDescriptor = value;
		}
	}

	public class XmlTransformerDescriptor : XmlDescriptor
	{
		[XmlElement("TransformerClass")]
		[CanBeNull]
		public XmlClassDescriptor TransformerClass
		{
			get => ClassDescriptor;
			set => ClassDescriptor = value;
		}
	}

	public class XmlTestDescriptor : XmlDescriptor
	{
		private const int _nullExecutionPriority = -1;

		[XmlElement("TestClass")]
		[CanBeNull]
		public XmlClassDescriptor TestClass
		{
			get => ClassDescriptor;
			set => ClassDescriptor = value;
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

		[XmlElement("TestFactory")]
		[CanBeNull]
		public XmlClassDescriptor TestFactoryDescriptor { get; set; }

		[XmlElement("TestConfigurator")]
		[CanBeNull]
		public XmlClassDescriptor TestConfigurator { get; set; }

		public int? GetExecutionPriority()
		{
			return ExecutionPriority == _nullExecutionPriority
				       ? (int?) null
				       : ExecutionPriority;
		}

		public void SetExecutionPriority(int? executionPriority)
		{
			ExecutionPriority = executionPriority ?? _nullExecutionPriority;
		}
	}
}
