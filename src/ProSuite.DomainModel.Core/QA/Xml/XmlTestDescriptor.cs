using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlTestDescriptor : XmlInstanceDescriptor
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
