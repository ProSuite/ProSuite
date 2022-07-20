using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public static class TestReportUtils
	{
		public static void WriteTestReport([NotNull] IList<Assembly> assemblies,
		                                   [NotNull] string htmlFileName,
		                                   bool overwrite)
		{
			Assert.ArgumentNotNull(assemblies, nameof(assemblies));
			Assert.ArgumentNotNullOrEmpty(htmlFileName, nameof(htmlFileName));

			if (overwrite && File.Exists(htmlFileName))
			{
				File.Delete(htmlFileName);
			}

			using (TextWriter writer = new StreamWriter(htmlFileName))
			{
				WriteTestReport(assemblies, writer);
			}
		}

		public static void WriteTestReport([NotNull] IList<Assembly> assemblies,
		                                   [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(assemblies, nameof(assemblies));

			var builder = new HtmlReportBuilder(writer, "ProSuite QA Test Documentation");

			builder.AddHeaderItem("ProSuite Version",
			                      ReflectionUtils.GetAssemblyVersionString(
				                      Assembly.GetExecutingAssembly()));

			builder.IncludeObsolete = false;
			builder.IncludeAssemblyInfo = true;

			IncludeTestClasses(builder, assemblies);
			IncludeTransformerClasses(builder, assemblies);
			IncludeTestFactories(builder, assemblies);

			builder.WriteReport();
		}

		public static void WritePythonTransformerClass([NotNull] IList<Assembly> assemblies,
		                                               [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(assemblies, nameof(assemblies));

			var builder = new PythonClassBuilder(writer);

			builder.AddHeaderItem("ProSuite Version",
			                      ReflectionUtils.GetAssemblyVersionString(
				                      Assembly.GetExecutingAssembly()));

			builder.IncludeObsolete = false;
			builder.IncludeAssemblyInfo = true;

			IncludeTransformerClasses(builder, assemblies);

			builder.WriteTransformerClassFile();
		}

		public static void WritePythonTestClasses([NotNull] IList<Assembly> assemblies,
		                                          [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(assemblies, nameof(assemblies));

			var builder = new PythonClassBuilder(writer);

			builder.AddHeaderItem("ProSuite Version",
			                      ReflectionUtils.GetAssemblyVersionString(
				                      Assembly.GetExecutingAssembly()));

			builder.IncludeObsolete = false;
			builder.IncludeAssemblyInfo = true;

			IncludeTestClasses(builder, assemblies);
			IncludeTestFactories(builder, assemblies);

			builder.WriteReport();
		}

		public static void WriteXmlTestDescriptors([NotNull] IList<Assembly> assemblies,
		                                           [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(assemblies, nameof(assemblies));

			var builder = new XmlTestDescriptorsBuilder(writer);

			builder.AddHeaderItem("ProSuite Version",
			                      ReflectionUtils.GetAssemblyVersionString(
				                      Assembly.GetExecutingAssembly()));

			builder.IncludeObsolete = false;
			builder.IncludeAssemblyInfo = true;

			IncludeTestClasses(builder, assemblies);
			IncludeTestFactories(builder, assemblies);

			builder.WriteReport();
		}

		private static void IncludeTestClasses([NotNull] IReportBuilder reportBuilder,
		                                       [NotNull] IEnumerable<Assembly> assemblies)
		{
			const bool includeObsolete = true;
			const bool includeInternallyUsed = true;

			foreach (Assembly assembly in assemblies)
			{
				foreach (Type testType in TestFactoryUtils.GetTestClasses(
					         assembly, includeObsolete, includeInternallyUsed))
				{
					foreach (int ctorIndex in InstanceFactoryUtils.GetConstructorIndexes(
						         testType, includeObsolete, includeInternallyUsed))
					{
						reportBuilder.IncludeTest(testType, ctorIndex);
					}
				}
			}
		}

		private static void IncludeTransformerClasses([NotNull] IReportBuilder reportBuilder,
		                                              [NotNull] IEnumerable<Assembly> assemblies)
		{
			const bool includeObsolete = true;
			const bool includeInternallyUsed = true;

			foreach (Assembly assembly in assemblies)
			{
				foreach (Type transformerType in InstanceFactoryUtils.GetTransformerClasses(
					         assembly, includeObsolete, includeInternallyUsed))
				{
					foreach (int ctorIndex in InstanceFactoryUtils.GetConstructorIndexes(
						         transformerType, includeObsolete, includeInternallyUsed))
					{
						reportBuilder.IncludeTransformer(transformerType, ctorIndex);
					}
				}
			}
		}

		private static void IncludeTestFactories([NotNull] IReportBuilder reportBuilder,
		                                         [NotNull] IEnumerable<Assembly> assemblies)
		{
			const bool includeObsolete = true;
			const bool includeInternallyUsed = true;
			foreach (Assembly assembly in assemblies)
			{
				foreach (Type testFactoryType in TestFactoryUtils.GetTestFactoryClasses(
					         assembly, includeObsolete, includeInternallyUsed))
				{
					reportBuilder.IncludeTestFactory(testFactoryType);
				}
			}
		}
	}
}
