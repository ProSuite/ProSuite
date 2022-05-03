using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;

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

		public override void AddHeaderItem(string name, string value) { }

		public override void WriteReport()
		{
			IncludedTestFactories.Sort();

			List<IncludedInstanceBase> includedTests =
				GetSortedTestClasses().Cast<IncludedInstanceBase>().ToList();

			includedTests.AddRange(IncludedTestFactories);

			if (includedTests.Count <= 0)
			{
				return;
			}

			var document = new XmlDataQualityDocument30();

			foreach (IncludedInstanceBase includedTest in includedTests)
			{
				if (includedTest is IncludedInstanceClass includedTestClass)
				{
					Type testType = includedTestClass.TestType;

					foreach (IncludedInstanceConstructor constructor in includedTestClass
						         .TestConstructors)
					{
						string testName = $"{testType.Name}({constructor.ConstructorIndex})";

						TestDescriptor testDescriptor = new TestDescriptor(
							testName, new ClassDescriptor(testType),
							constructor.ConstructorIndex, false, false, null);

						Add(testDescriptor, document);
					}
				}
				else if (includedTest is IncludedTestFactory includedFactory)
				{
					Type factoryType = includedFactory.TestType;

					string testName = $"{factoryType.Name}";

					TestDescriptor testDescriptor = new TestDescriptor(
						testName, new ClassDescriptor(factoryType),
						false, false, null);

					Add(testDescriptor, document);
				}
			}

			_textWriter.Write(XmlUtils.Serialize(document));
		}

		private static void Add(TestDescriptor testDescriptor,
		                        XmlDataQualityDocument toDocument)
		{
			XmlTestDescriptor xmlTestDescriptor =
				XmlDataQualityUtils.CreateXmlTestDescriptor(testDescriptor, false);

			toDocument.AddTestDescriptor(xmlTestDescriptor);
		}
	}
}
