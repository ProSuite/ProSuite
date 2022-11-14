using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core.Reports;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public class XmlTestDescriptorsBuilder : ReportBuilderBase
	{
		private readonly TextWriter _textWriter;

		public XmlTestDescriptorsBuilder([NotNull] TextWriter textWriter)
		{
			Assert.ArgumentNotNull(textWriter, nameof(textWriter));

			_textWriter = textWriter;
		}

		public bool StopOnError { get; set; } = false;

		public bool AllowErrors { get; set; } = false;

		public bool UseDefaultTestDescriptorName { get; set; } = false;

		public override void AddHeaderItem(string name, string value) { }

		public override void WriteReport()
		{
			var includedTests = new List<IncludedInstanceBase>(IncludedTestClasses.Values);
			includedTests.AddRange(IncludedTestFactories);
			includedTests.Sort();

			if (includedTests.Count <= 0)
			{
				return;
			}

			var document = new XmlDataQualityDocument30();

			foreach (IncludedInstanceBase includedTest in includedTests)
			{
				if (includedTest is IncludedInstanceClass includedTestClass)
				{
					Type testType = includedTestClass.InstanceType;

					foreach (IncludedInstanceConstructor constructor in includedTestClass
						         .InstanceConstructors)
					{
						string testName = UseDefaultTestDescriptorName
							                  ? TestFactoryUtils.GetDefaultTestDescriptorName(
								                  testType, constructor.ConstructorIndex)
							                  : $"{testType.Name}({constructor.ConstructorIndex})";

						TestDescriptor testDescriptor = new TestDescriptor(
							testName, new ClassDescriptor(testType), constructor.ConstructorIndex,
							StopOnError, AllowErrors);

						Add(testDescriptor, document);
					}
				}
				else if (includedTest is IncludedTestFactory includedFactory)
				{
					Type testFactoryType = includedFactory.InstanceType;

					string testName = UseDefaultTestDescriptorName
						                  ? TestFactoryUtils.GetDefaultTestDescriptorName(
							                  testFactoryType)
						                  : $"{testFactoryType.Name}";

					TestDescriptor testDescriptor = new TestDescriptor(
						testName, new ClassDescriptor(testFactoryType),
						StopOnError, AllowErrors);

					Add(testDescriptor, document);
				}
			}

			_textWriter.Write(XmlUtils.Serialize(document));
		}

		private static void Add(TestDescriptor testDescriptor, XmlDataQualityDocument toDocument)
		{
			XmlTestDescriptor xmlTestDescriptor =
				XmlDataQualityUtils.CreateXmlTestDescriptor(testDescriptor, false);

			toDocument.AddTestDescriptor(xmlTestDescriptor);
		}
	}
}
