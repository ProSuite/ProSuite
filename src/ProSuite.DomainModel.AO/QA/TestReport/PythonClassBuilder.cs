using System;
using System.IO;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.QA.Core.Reports;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public class PythonClassBuilder : PythonClassBuilderBase
	{
		public PythonClassBuilder([NotNull] TextWriter textWriter)
			: base(textWriter) { }

		protected override object GetDefaultParameterValue(Type parameterType)
		{
			if (parameterType.IsValueType)
			{
				if (parameterType.IsEnum)
				{
					// Until all enums are also pythonified
					return default(int);
				}

				return Activator.CreateInstance(parameterType);
			}

			return null;
		}

		protected override string TranslateToPythonType(TestParameter testParameter)
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
					result = "any";
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
