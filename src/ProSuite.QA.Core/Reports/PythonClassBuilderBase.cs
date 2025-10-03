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
		protected TextWriter TextWriter { get; }

		protected PythonClassBuilderBase([NotNull] TextWriter textWriter)
		{
			Assert.ArgumentNotNull(textWriter, nameof(textWriter));

			TextWriter = textWriter;
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

			WriteConditionImports(sb);

			CreatePythonConditionClass(includedTests, sb);

			TextWriter.Write(sb.ToString());
		}

		public virtual void WriteHeader(StringBuilder sb) { }

		protected virtual void WriteConditionImports(StringBuilder sb) { }

		private void CreatePythonConditionClass(
			[NotNull] IEnumerable<IncludedInstanceBase> includedTests,
			[NotNull] StringBuilder sb)
		{
			sb.AppendLine("class Conditions:");

			foreach (IncludedInstanceBase includedTest in includedTests)
			{
				if (includedTest is IncludedInstanceClass includedTestClass)
				{
					if (includedTestClass.InstanceConstructors.Count == 0)
					{
						continue;
					}

					foreach (IncludedInstanceConstructor constructor in
					         includedTestClass.InstanceConstructors)
					{
						if (HasUnsupportedParameters(constructor))
						{
							continue;
						}

						AppendClassMethod("Condition", includedTestClass, constructor, sb);
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

		protected void AppendClassMethod(string resultClassName,
		                                 IncludedInstanceBase includedInstance,
		                                 IncludedInstanceConstructor constructor, StringBuilder sb)
		{
			IInstanceInfo factory = constructor.InstanceInfo;

			string methodName =
				$"{ToUnderscoreCase(includedInstance.InstanceType.Name)}_{constructor.ConstructorIndex}";

			string methodSignature = GetConstructorSignature(factory);
			string conditionConstructorSignature =
				$"\"{includedInstance.InstanceType.Name}({constructor.ConstructorIndex})\"";

			AppendMethod(methodName, resultClassName, methodSignature, factory,
			             conditionConstructorSignature, sb);
		}

		private static void AppendConditionMethod(string methodName, string methodSignature,
		                                          IInstanceInfo factory,
		                                          string conditionConstructorSignature,
		                                          StringBuilder sb)
		{
			var resultClassName = "Condition";

			AppendMethod(methodName, resultClassName, methodSignature, factory,
			             conditionConstructorSignature, sb);
		}

		private static void AppendMethod(string methodName, string resultClassName,
		                                 string methodSignature,
		                                 IInstanceInfo factory,
		                                 string conditionConstructorSignature,
		                                 StringBuilder sb)
		{
			string description = factory.TestDescription ?? string.Empty;
			description = description.Replace(Environment.NewLine, $"{Environment.NewLine}        ")
			                         .Replace("/", @"\/");

			sb.AppendLine();
			sb.AppendLine($"    @classmethod");
			sb.AppendLine($"    def {methodName}({methodSignature}) -> {resultClassName}:");
			sb.AppendLine($"        \"\"\"");
			sb.AppendLine($"        {description}");
			sb.AppendLine($"        \"\"\"");
			sb.AppendLine($"        ");
			sb.AppendLine($"        result = {resultClassName}({conditionConstructorSignature})");

			foreach (TestParameter testParameter in factory.Parameters)
			{
				string snakeCasePythonName = ToUnderscoreCase(testParameter.Name);

				if (testParameter.ArrayDimension > 0)
				{
					string parameterConstructor =
						$"Parameter(\"{testParameter.Name}\", element)";
					sb.AppendLine(
						$"        if type({snakeCasePythonName}) == list:");
					sb.AppendLine(
						$"            for element in {snakeCasePythonName}:");
					sb.AppendLine(
						$"                result.parameters.append({parameterConstructor})");
					sb.AppendLine(
						$"        elif {snakeCasePythonName} is not None:");
					sb.AppendLine(
						$"            result.parameters.append(Parameter(\"{testParameter.Name}\", {snakeCasePythonName}))");
					// else: No value specified, do not add a parameter (the server will have to translate this to null)
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

			AppendConditionMethod(methodName, methodSignature, factory,
			                      conditionConstructorSignature, sb);
		}

		private static string ToUnderscoreCase(string str)
		{
			return string.Concat(str.Select(
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
