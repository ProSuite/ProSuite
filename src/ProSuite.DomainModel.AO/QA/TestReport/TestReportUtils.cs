using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.QA.Core.Reports;

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

			var builder = new HtmlReportBuilder(writer, "ProSuite QA Documentation");

			builder.AddHeaderItem("ProSuite Version",
			                      ReflectionUtils.GetAssemblyVersionString(
				                      Assembly.GetExecutingAssembly()));

			builder.IncludeObsolete = false;
			builder.IncludeAssemblyInfo = true;

			IncludeTestClasses(builder, assemblies);
			IncludeTransformerClasses(builder, assemblies);
			IncludeIssueFilterClasses(builder, assemblies);
			IncludeTestFactories(builder, assemblies);

			builder.WriteReport();
		}

		public static void WriteDescriptorsReport(
			[NotNull] IList<InstanceDescriptor> descriptors,
			[NotNull] string htmlFileName,
			bool overwrite,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));
			Assert.ArgumentNotNullOrEmpty(htmlFileName, nameof(htmlFileName));

			if (overwrite && File.Exists(htmlFileName))
			{
				File.Delete(htmlFileName);
			}

			using (TextWriter writer = new StreamWriter(htmlFileName))
			{
				WriteDescriptorsReport(descriptors, writer, notifications);
			}
		}

		public static void WriteDescriptorsReport(
			[NotNull] IList<InstanceDescriptor> descriptors,
			TextWriter writer,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));

			var builder =
				new HtmlReportBuilder(
					writer, "Registered test, transformer and filter implementations");

			builder.IncludeObsolete = false;
			builder.IncludeAssemblyInfo = true;

			foreach (InstanceDescriptor descriptor in descriptors)
			{
				try
				{
					IncludeInstanceDescriptor(builder, descriptor);
				}
				catch (Exception ex)
				{
					NotificationUtils.Add(notifications,
					                      $"Unable to include {descriptor}: {ExceptionUtils.FormatMessage(ex)}");
				}
			}

			builder.WriteReport();
		}

		[NotNull]
		public static string WriteDescriptorDoc([NotNull] InstanceDescriptor descriptor)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			var stringWriter = new StringWriter();

			WriteDescriptorDoc(descriptor, stringWriter);

			return stringWriter.ToString();
		}

		public static void WriteDescriptorDoc([NotNull] InstanceDescriptor descriptor,
		                                      [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			var builder = new HtmlReportBuilder(writer, "ProSuite QA Documentation")
			              {
				              ExcludeHeadersAndIndex = true
			              };

			builder.AddHeaderItem("ProSuite Version",
			                      ReflectionUtils.GetAssemblyVersionString(
				                      Assembly.GetExecutingAssembly()));

			builder.IncludeObsolete = false;
			builder.IncludeAssemblyInfo = true;

			IncludeInstanceDescriptor(builder, descriptor);

			builder.WriteReport();
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

		public static void IncludeInstanceDescriptor([NotNull] IReportBuilder reportBuilder,
		                                             [NotNull] InstanceDescriptor descriptor)
		{
			if (descriptor is TransformerDescriptor)
			{
				reportBuilder.IncludeTransformer(
					Assert.NotNull(descriptor.Class).GetInstanceType(),
					descriptor.ConstructorId);
			}
			else if (descriptor is IssueFilterDescriptor)
			{
				reportBuilder.IncludeIssueFilter(
					Assert.NotNull(descriptor.Class).GetInstanceType(),
					descriptor.ConstructorId);
			}
			else if (descriptor is TestDescriptor testDescriptor)
			{
				if (testDescriptor.TestClass != null)
				{
					reportBuilder.IncludeTest(
						testDescriptor.TestClass.GetInstanceType(),
						testDescriptor.TestConstructorId);
				}
				else if (testDescriptor.TestFactoryDescriptor != null)
				{
					reportBuilder.IncludeTestFactory(
						testDescriptor.TestFactoryDescriptor.GetInstanceType());
				}
				else
				{
					throw new InvalidOperationException(
						$"Neither test class nor factory defined for {testDescriptor}");
				}
			}
			else
			{
				throw new InvalidOperationException(
					$"Unknown descriptor type {descriptor.GetType()}");
			}
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
					foreach (int ctorIndex in InstanceUtils.GetConstructorIndexes(
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
					foreach (int ctorIndex in InstanceUtils.GetConstructorIndexes(
						         transformerType, includeObsolete, includeInternallyUsed))
					{
						reportBuilder.IncludeTransformer(transformerType, ctorIndex);
					}
				}
			}
		}

		private static void IncludeIssueFilterClasses([NotNull] IReportBuilder reportBuilder,
		                                              [NotNull] IEnumerable<Assembly> assemblies)
		{
			const bool includeObsolete = true;
			const bool includeInternallyUsed = true;

			foreach (Assembly assembly in assemblies)
			{
				foreach (Type transformerType in InstanceFactoryUtils.GetIssueFilterClasses(
					         assembly, includeObsolete, includeInternallyUsed))
				{
					foreach (int ctorIndex in InstanceUtils.GetConstructorIndexes(
						         transformerType, includeObsolete, includeInternallyUsed))
					{
						reportBuilder.IncludeIssueFilter(transformerType, ctorIndex);
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
