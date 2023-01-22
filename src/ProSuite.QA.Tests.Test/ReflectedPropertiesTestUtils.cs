using System;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.Tests.Test
{
	public static class ReflectedPropertiesTestUtils
	{
		public static void AreTestDescriptionsDefined([NotNull] Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes().Where(IsValidTestType))
			{
				int n = type.GetConstructors().Length;
				for (var i = 0; i < n; i++)
				{
					TestFactory testFactory = new DefaultTestFactory(type, i);
					ReportMissingTestDescription(testFactory, i);
				}
			}
		}

		public static void AreTestFactoryDescriptionsDefined([NotNull] Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes().Where(IsValidTestFactoryType))
			{
				TestFactory testFactory = (TestFactory) Activator.CreateInstance(type);

				ReportMissingTestDescription(testFactory, -1);
			}
		}

		public static void AreTestCategoriesDefined([NotNull] Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes().Where(IsValidTestType))
			{
				ReportMissingTestCategories(type);
			}
		}

		public static void AreTestFactoryCategoriesDefined([NotNull] Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes().Where(IsValidTestFactoryType))
			{
				ReportMissingTestCategories(type);
			}
		}

		public static void AreTestIssueCodesDefined([NotNull] Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes().Where(IsValidTestType))
			{
				ReportMissingTestIssueCodes(type);
			}
		}

		public static void AreTestFactoryIssueCodesDefined([NotNull] Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes().Where(IsValidTestFactoryType))
			{
				ReportMissingTestIssueCodes(type);
			}
		}

		private static bool IsValidTestType([NotNull] Type type)
		{
			return typeof(ITest).IsAssignableFrom(type) &&
			       ! type.IsAbstract &&
			       ! ReflectionUtils.IsObsolete(type) &&
			       ! ReflectionUtils.HasAttribute<InternallyUsedTestAttribute>(type);
		}

		private static bool IsValidTestFactoryType([NotNull] Type type)
		{
			return typeof(TestFactory).IsAssignableFrom(type) &&
			       ! type.IsAbstract &&
			       ! ReflectionUtils.IsObsolete(type) &&
			       ! ReflectionUtils.HasAttribute<InternallyUsedTestAttribute>(type);
		}

		private static void ReportMissingTestDescription([NotNull] TestFactory testFactory,
		                                                 int constrId)
		{
			string typeName = testFactory is DefaultTestFactory
				                  ? testFactory.GetTestTypeDescription()
				                  : testFactory.GetType().Name;

			if (string.IsNullOrEmpty(testFactory.TestDescription))
			{
				Console.WriteLine($@"Missing Test Description: {typeName}_{constrId}");
			}

			foreach (TestParameter parameter in testFactory.Parameters)
			{
				string parameterDescription = testFactory.GetParameterDescription(parameter.Name);
				if (string.IsNullOrEmpty(parameterDescription))
				{
					Console.WriteLine(
						$@"Missing Parameter Description: {typeName}_{constrId}: {parameter.Name}");
				}
			}
		}

		private static void ReportMissingTestCategories([NotNull] Type testType)
		{
			if (ReflectionUtils.GetCategories(testType).Length == 0)
			{
				Console.WriteLine($@"No Test Category: {testType.Name}");
			}
		}

		private static void ReportMissingTestIssueCodes([NotNull] Type testType)
		{
			var issueCodes = IssueCodeUtils.GetIssueCodes(testType);
			if (issueCodes.Count == 0)
			{
				Console.WriteLine($@"No Issue Codes: {testType.Name}");
			}
			else
			{
				foreach (IssueCode issueCode in issueCodes)
				{
					string codeDescription = issueCode.Description;
					if (string.IsNullOrEmpty(codeDescription))
					{
						Console.WriteLine(
							$@"Missing Issue Code Description: {testType.Name}: {issueCode.ID}");
					}
				}
			}
		}
	}
}
