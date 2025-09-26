using System;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.IO;
using ProSuite.DomainModel.AO.QA.TestReport;

namespace ProSuite.DomainModel.AO.Test.QA.TestReport
{
	[TestFixture]
	public class TestReportUtilsTest
	{
		[Test]
		public void CanWriteReport()
		{
			var writer = new StringWriter();

			TestReportUtils.WriteTestReport(new[] { Assembly.GetExecutingAssembly() }, writer);

			string html = writer.ToString();

			Console.WriteLine(html);
		}

		[Test]
		public void CanWriteTestAndTransformerReport()
		{
			var writer = new StringWriter();

			string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Assert.NotNull(binDir);

			Assembly testAssembly =
				AssemblyResolveUtils.TryLoadAssembly("ProSuite.QA.Tests", binDir);

			Assembly testFactoryAssembly =
				AssemblyResolveUtils.TryLoadAssembly("ProSuite.QA.TestFactories", binDir);

			TestReportUtils.WriteTestReport(new[] { testAssembly, testFactoryAssembly }, writer);

			string html = writer.ToString();

			const string reportName = "Qa_TestDescriptors.html";
			Console.WriteLine(Path.GetFullPath(reportName));

			Console.WriteLine(html);

			FileSystemUtils.WriteTextFile(html, reportName);
		}

		[Test]
		public void CanWritePythonTestClasses()
		{
			var writer = new StringWriter();

			TestReportUtils.WritePythonTestClasses(new[] { Assembly.GetExecutingAssembly() },
			                                       writer);

			string html = writer.ToString();

			Console.WriteLine(html);
		}

		[Test]
		public void CanWriteAllPythonTestClasses()
		{
			var writer = new StringWriter();

			string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Assert.NotNull(binDir);

			Assembly testAssembly =
				AssemblyResolveUtils.TryLoadAssembly("ProSuite.QA.Tests", binDir);
			Assembly testFactoryAssembly =
				AssemblyResolveUtils.TryLoadAssembly("ProSuite.QA.TestFactories", binDir);

			TestReportUtils.WritePythonTestClasses(new[] { testAssembly, testFactoryAssembly },
			                                       writer);

			string pythonClass = writer.ToString();

			Console.WriteLine(pythonClass);

			FileSystemUtils.WriteTextFile(pythonClass, "condition_factory.py", Encoding.UTF8);
		}

		[Test]
		public void CanWriteAllXmlTestDescriptors()
		{
			var writer = new StringWriter();

			string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Assert.NotNull(binDir);

			Assembly testAssembly =
				AssemblyResolveUtils.TryLoadAssembly("ProSuite.QA.Tests", binDir);
			Assembly testFactoryAssembly =
				AssemblyResolveUtils.TryLoadAssembly("ProSuite.QA.TestFactories", binDir);

			TestReportUtils.WriteXmlTestDescriptors(new[] { testAssembly, testFactoryAssembly },
			                                        writer);

			string xml = writer.ToString();

			Console.WriteLine(xml);

			FileSystemUtils.WriteTextFile(xml, "Qa_TestDescriptors.qa.xml", Encoding.UTF8);
		}
	}
}
