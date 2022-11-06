using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core.Reports
{
	public abstract class PythonClassBuilderBase : ReportBuilderBase
	{
		private readonly TextWriter _textWriter;

		protected PythonClassBuilderBase([NotNull] TextWriter textWriter)
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

			var sb = new StringBuilder();

			WriteHeader(sb);

			CreatePythonClass(includedTests, sb);

			_textWriter.Write(sb.ToString());
		}

		public void WriteTransformerClassFile()
		{
			List<IncludedInstanceBase> includedTransformers =
				GetSortedTransformerClasses().Cast<IncludedInstanceBase>().ToList();

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

		private void CreatePythonClass(IEnumerable<IncludedInstanceBase> includedTests,
		                               StringBuilder sb)
		{
			sb.AppendLine("class ConditionFactory:");

			foreach (IncludedInstanceBase includedTest in includedTests)
			{
				if (includedTest is IncludedInstanceClass includedTestClass)
				{
					if (includedTestClass.InstanceConstructors.Count <= 0)
					{
						continue;
					}

					foreach (IncludedInstanceConstructor constructor in includedTestClass
						         .InstanceConstructors)
					{
						if (HasUnsupportedParameters(constructor))
						{
							continue;
						}

						AppendTestClassMethod(includedTestClass, constructor, sb);
					}
				}
				else if (includedTest is IncludedTestFactory includedTestFactory)
				{
					if (HasUnsupportedParameters(includedTest))
					{
						continue;
					}

					AppendTestFactoryMethod(includedTestFactory, sb);
				}
			}
		}

		private void CreatePythonTransformerClass(
			IEnumerable<IncludedInstanceBase> includedTransformers,
			StringBuilder sb)
		{
			sb.AppendLine("class Transformer:");

			foreach (IncludedInstanceBase includedTest in includedTransformers)
			{
				if (includedTest is IncludedInstanceClass includedTransformer)
				{
					if (includedTransformer.InstanceConstructors.Count <= 0)
					{
						continue;
					}

					foreach (IncludedInstanceConstructor constructor in includedTransformer
						         .InstanceConstructors)
					{
						AppendTransformerClassMethod(includedTransformer, constructor, sb);
					}
				}
			}
		}

		private void AppendTransformerClassMethod(
			IncludedInstanceBase includedTransformerClass,
			IncludedInstanceConstructor constructor,
			StringBuilder sb)
		{
			IInstanceInfo testFactory = constructor.InstanceInfo;

			string methodName =
				$"{ToUnderscoreCase(includedTransformerClass.InstanceType.Name)}_{constructor.ConstructorIndex}";

			string methodSignature = GetConstructorSignature(testFactory);
			string conditionConstructorSignature =
				$"\"{includedTransformerClass.InstanceType.Name}({constructor.ConstructorIndex})\"";

			AppendTransformerMethod(methodName, methodSignature, testFactory,
			                        conditionConstructorSignature,
			                        sb);
		}

		private void AppendTestClassMethod(IncludedInstanceBase includedTestClass,
		                                   IncludedInstanceConstructor constructor,
		                                   StringBuilder sb)
		{
			IInstanceInfo factory = constructor.InstanceInfo;

			string methodName =
				$"{ToUnderscoreCase(includedTestClass.InstanceType.Name)}_{constructor.ConstructorIndex}";

			string methodSignature = GetConstructorSignature(factory);
			string conditionConstructorSignature =
				$"\"{includedTestClass.InstanceType.Name}({constructor.ConstructorIndex})\"";

			AppendMethod(methodName, methodSignature, factory, conditionConstructorSignature,
			             sb);
		}

		private static void AppendTransformerMethod(string methodName, string methodSignature,
		                                            IInstanceInfo factory,
		                                            string conditionConstructorSignature,
		                                            StringBuilder sb)
		{
			sb.AppendLine();
			sb.AppendLine($"    @classmethod");
			sb.AppendLine($"    def {methodName}({methodSignature}):");
			sb.AppendLine($"        pass");
		}

		private static void AppendMethod(string methodName, string methodSignature,
		                                 IInstanceInfo factory,
		                                 string conditionConstructorSignature, StringBuilder sb)
		{
			sb.AppendLine();
			sb.AppendLine($"    @classmethod");
			sb.AppendLine($"    def {methodName}({methodSignature}) -> Condition:");
			sb.AppendLine($"        \"\"\"");
			sb.AppendLine($"        {factory.TestDescription ?? string.Empty}        \"\"\"");
			sb.AppendLine($"        result = Condition({conditionConstructorSignature})");

			foreach (TestParameter testParameter in factory.Parameters)
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

		private void AppendTestFactoryMethod(IncludedTestFactory includedTestFactory,
		                                     StringBuilder sb)
		{
			IInstanceInfo factory = includedTestFactory.InstanceInfo;

			string methodName = ToUnderscoreCase(includedTestFactory.InstanceType.Name);

			string methodSignature = GetConstructorSignature(factory);
			string conditionConstructorSignature = $"\"{includedTestFactory.InstanceType.Name}\"";

			AppendMethod(methodName, methodSignature, factory, conditionConstructorSignature,
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
		private string GetConstructorSignature([NotNull] IInstanceInfo instanceInfo)
		{
			Assert.ArgumentNotNull(instanceInfo, nameof(instanceInfo));

			var sb = new StringBuilder();

			// Because it is a @classmethod
			sb.Append("cls");

			foreach (TestParameter testParameter in instanceInfo.Parameters.Where(
				         p => p.IsConstructorParameter))
			{
				AppendTestParameter(sb, testParameter);
			}

			// Optional parameters:
			foreach (TestParameter testParameter in instanceInfo.Parameters.Where(
				         p => ! p.IsConstructorParameter))
			{
				AppendTestParameter(sb, testParameter);

				// Set a default value:
				object defaultValue = testParameter.DefaultValue ??
				                      GetDefaultParameterValue(testParameter.Type);

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

		private void AppendTestParameter([NotNull] StringBuilder stringBuilder,
		                                 [NotNull] TestParameter testParameter)
		{
			if (stringBuilder.Length > 1)
			{
				stringBuilder.Append(", ");
			}

			string snakeCasePythonName = ToUnderscoreCase(testParameter.Name);

			stringBuilder.AppendFormat("{0}", snakeCasePythonName);

			string pythonType = TranslateToPythonType(testParameter);

			stringBuilder.AppendFormat(": {0}", pythonType);
		}

		protected virtual bool HasUnsupportedParameters(IncludedInstanceBase includedTest)
		{
			return false;
		}

		protected abstract object GetDefaultParameterValue(Type parameterType);

		[NotNull]
		protected abstract string TranslateToPythonType([NotNull] TestParameter testParameter);
	}
}
