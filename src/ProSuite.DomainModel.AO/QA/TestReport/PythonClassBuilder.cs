using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public class PythonClassBuilder : ReportBuilderBase
	{
		private readonly TextWriter _textWriter;

		public PythonClassBuilder(TextWriter textWriter)
		{
			Assert.ArgumentNotNull(textWriter, nameof(textWriter));

			_textWriter = textWriter;
		}

		public override void AddHeaderItem(string name, string value) { }

		public override void WriteReport()
		{
			IncludedTestFactories.Sort();

			List<IncludedTestBase> includedTests =
				GetSortedTestClasses().Cast<IncludedTestBase>().ToList();

			
			includedTests.AddRange(IncludedTestFactories);

			if (includedTests.Count <= 0)
			{
				return;
			}

			var sb = new StringBuilder();

			WriteHeader(sb);

			CreatePythonClass(includedTests, sb);

			_textWriter.Write(sb.ToString());
		}

		public void WriteTransformerClassFile()
		{
			List<IncludedTestBase> includedTransformers =
				GetSortedTransformerClasses().Cast<IncludedTestBase>().ToList();

			if (includedTransformers.Count <= 0)
			{
				return;
			}

			var sb = new StringBuilder();

			WriteTransformerHeader(sb);

			CreatePythonTransformerClass(includedTransformers, sb);

			_textWriter.Write(sb.ToString());
		}

		private static void WriteTransformerHeader(StringBuilder sb)
		{
			// add import statements
		}

		private static void WriteHeader(StringBuilder sb)
		{
			sb.AppendLine("from datetime import datetime");
			sb.AppendLine("from typing import List");
			sb.AppendLine("from prosuite.condition import Condition");
			sb.AppendLine("from prosuite.parameter import Parameter");
			sb.AppendLine("from prosuite.dataset import Dataset");

			sb.AppendLine();
			sb.AppendLine();
		}

		private static void CreatePythonClass(IEnumerable<IncludedTestBase> includedTests,
		                                      StringBuilder sb)
		{
			sb.AppendLine("class ConditionFactory:");

			foreach (IncludedTestBase includedTest in includedTests)
			{
				if (includedTest is IncludedTestClass includedTestClass)
				{
					if (includedTestClass.TestConstructors.Count <= 0)
					{
						continue;
					}

					foreach (IncludedTestConstructor constructor in includedTestClass
						.TestConstructors)
					{
						AppendTestClassMethod(includedTestClass, constructor, sb);
					}
				}
				else if (includedTest is IncludedTestFactory includedTestFactory)
				{
					AppendTestFactoryMethod(includedTestFactory, sb);
				}
			}
		}

		private static void CreatePythonTransformerClass(IEnumerable<IncludedTestBase> includedTransformers,
		                                      StringBuilder sb)
		{
			sb.AppendLine("class Transformer:");

			foreach (IncludedTestBase includedTest in includedTransformers)
			{
				if (includedTest is IncludedTransformer includedTransformer)
				{
					if (includedTransformer.TestConstructors.Count <= 0)
					{
						continue;
					}

					foreach (IncludedTestConstructor constructor in includedTransformer
						.TestConstructors)
					{
						AppendTransformerClassMethod(includedTransformer, constructor, sb);
					}
				}
				
			}
		}

		private static void AppendTransformerClassMethod(IncludedTestBase includedTransformerClass,
		                                          IncludedTestConstructor constructor,
		                                          StringBuilder sb)
		{
			TestFactory testFactory = constructor.TestFactory;

			string methodName =
				$"{ToUnderscoreCase(includedTransformerClass.TestType.Name)}_{constructor.ConstructorIndex}";

			string methodSignature = GetConstructorSignature(testFactory);
			string conditionConstructorSignature =
				$"\"{includedTransformerClass.TestType.Name}({constructor.ConstructorIndex})\"";

			AppendTransformerMethod(methodName, methodSignature, testFactory, conditionConstructorSignature,
			             sb);
		}


		private static void AppendTestClassMethod(IncludedTestBase includedTestClass,
		                                          IncludedTestConstructor constructor,
		                                          StringBuilder sb)
		{
			TestFactory testFactory = constructor.TestFactory;

			string methodName =
				$"{ToUnderscoreCase(includedTestClass.TestType.Name)}_{constructor.ConstructorIndex}";

			string methodSignature = GetConstructorSignature(testFactory);
			string conditionConstructorSignature =
				$"\"{includedTestClass.TestType.Name}({constructor.ConstructorIndex})\"";

			AppendMethod(methodName, methodSignature, testFactory, conditionConstructorSignature,
			             sb);
		}
		
		private static void AppendTransformerMethod(string methodName, string methodSignature,
		                                            TestFactory testFactory,
		                                            string conditionConstructorSignature,
		                                            StringBuilder sb)
		{
			sb.AppendLine();
			sb.AppendLine($"    @classmethod");
			sb.AppendLine($"    def {methodName}({methodSignature}):");
			sb.AppendLine($"        pass");
		}

		private static void AppendMethod(string methodName, string methodSignature,
		                                 TestFactory testFactory,
		                                 string conditionConstructorSignature, StringBuilder sb)
		{
			string testDescription = testFactory.GetTestDescription();

			sb.AppendLine();
			sb.AppendLine($"    @classmethod");
			sb.AppendLine($"    def {methodName}({methodSignature}) -> Condition:");
			sb.AppendLine($"        \"\"\"");
			sb.AppendLine($"        {testFactory.GetTestDescription()}        \"\"\"");
			sb.AppendLine($"        result = Condition({conditionConstructorSignature})");

			foreach (TestParameter testParameter in testFactory.Parameters)
			{
				string snakeCasePythonName = ToUnderscoreCase(testParameter.Name);

				if (testParameter.ArrayDimension > 0)
				{
					string parameterConstructor =
						$"Parameter(\"{testParameter.Name}\", element)";
					sb.AppendLine($"        if type({snakeCasePythonName}) == list:");
					sb.AppendLine($"            for element in {snakeCasePythonName}:");
					sb.AppendLine(
						$"                result.parameters.append({parameterConstructor})");
					sb.AppendLine($"        else:");
					sb.AppendLine(
						$"            result.parameters.append(Parameter(\"{testParameter.Name}\", {snakeCasePythonName}))");
				}
				else
				{
					string parameterConstructor =
						$"Parameter(\"{testParameter.Name}\", {snakeCasePythonName})";
					sb.AppendLine($"        result.parameters.append({parameterConstructor})");
				}
			}

			sb.AppendLine($"        result.generate_name()");
			sb.AppendLine($"        return result");
		}

		private static void AppendTestFactoryMethod(IncludedTestFactory includedTestFactory,
		                                            StringBuilder sb)
		{
			TestFactory testFactory = includedTestFactory.TestFactory;

			string methodName = ToUnderscoreCase(includedTestFactory.TestType.Name);

			string methodSignature = GetConstructorSignature(testFactory);
			string conditionConstructorSignature = $"\"{includedTestFactory.TestType.Name}\"";

			AppendMethod(methodName, methodSignature, testFactory, conditionConstructorSignature,
			             sb);
		}

		private static string ToUnderscoreCase(string str)
		{
			return string
			       .Concat(str.Select(
				               (x, i) => i > 0 && char.IsUpper(x)
					                         ? "_" + x.ToString()
					                         : x.ToString())).ToLower();
		}

		[NotNull]
		private static string GetConstructorSignature([NotNull] IInstanceInfo testInfo)
		{
			Assert.ArgumentNotNull(testInfo, nameof(testInfo));

			var sb = new StringBuilder();

			// Because it is a @classmethod
			sb.Append("cls");

			foreach (TestParameter testParameter in testInfo.Parameters.Where(
				p => p.IsConstructorParameter))
			{
				AppendTestParameter(sb, testParameter);
			}

			// Optional parameters:
			foreach (TestParameter testParameter in testInfo.Parameters.Where(
				p => ! p.IsConstructorParameter))
			{
				AppendTestParameter(sb, testParameter);

				// Set a default value:
				object defaultValue = testParameter.DefaultValue ?? GetDefault(testParameter.Type);

				if (defaultValue == null)
				{
					defaultValue = "None";
				}
				else if (defaultValue.GetType().IsEnum)
				{
					defaultValue = (int) defaultValue;
				}

				sb.AppendFormat(" = {0}", defaultValue);
			}

			return sb.ToString();
		}

		private static void AppendTestParameter(StringBuilder stringBuilder,
		                                        TestParameter testParameter)
		{
			if (stringBuilder.Length > 1)
			{
				stringBuilder.Append(", ");
			}

			string snakeCasePythonName = ToUnderscoreCase(testParameter.Name);

			stringBuilder.AppendFormat("{0}", snakeCasePythonName);

			string pythonType = TranslateType(testParameter);

			if (! string.IsNullOrEmpty(pythonType))
			{
				stringBuilder.AppendFormat(": {0}", pythonType);
			}
		}

		private static object GetDefault(Type type)
		{
			if (type.IsValueType)
			{
				if (type.IsEnum)
				{
					// Until all enums are also pythonified
					return default(int);
				}

				return Activator.CreateInstance(type);
			}

			return null;
		}

		private static string TranslateType(TestParameter testParameter)
		{
			TestParameterType parameterType =
				TestParameterTypeUtils.GetParameterType(testParameter.Type);

			string result;
			switch (parameterType)
			{
				case TestParameterType.VectorDataset:
				case TestParameterType.ObjectDataset:
				case TestParameterType.TableDataset:
					result = "Dataset";
					break;
				case TestParameterType.CustomScalar:
					result = null;
					break;
				case TestParameterType.String:
					result = "str";
					break;
				case TestParameterType.Integer:
					result = "int";
					break;
				case TestParameterType.Double:
					result = "float";
					break;
				case TestParameterType.DateTime:
					result = "datetime";
					break;
				case TestParameterType.Boolean:
					result = "bool";
					break;
				default:
					result = "None";
					break;
			}

			for (var i = 0; i < testParameter.ArrayDimension; i++)
			{
				result = $"List[{result}]";
			}

			return result;
		}
	}
}
