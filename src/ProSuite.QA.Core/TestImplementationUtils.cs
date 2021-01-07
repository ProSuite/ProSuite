using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.QA.Core
{
	public static class TestImplementationUtils
	{
		[NotNull]
		public static string GetParameterTypeString([NotNull] TestParameter testParameter)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			string typeString = testParameter.Type.Name;

			if (testParameter.ArrayDimension == 0)
			{
				return typeString;
			}

			var sb = new StringBuilder();

			sb.Append(typeString);

			for (var i = 0; i < testParameter.ArrayDimension; i++)
			{
				sb.Append("[]");
			}

			return sb.ToString();
		}

		[NotNull]
		public static string GetTestSignature([NotNull] ITestImplementationInfo testInfo)
		{
			Assert.ArgumentNotNull(testInfo, nameof(testInfo));

			var sb = new StringBuilder();

			foreach (TestParameter testParameter in testInfo.Parameters)
			{
				if (sb.Length > 1)
				{
					sb.Append(", ");
				}

				if (! testParameter.IsConstructorParameter)
				{
					sb.Append("[");
				}

				sb.Append(GetParameterTypeString(testParameter));
				sb.AppendFormat(" {0}", testParameter.Name);
				if (! testParameter.IsConstructorParameter)
				{
					sb.Append("]");
				}
			}

			return sb.ToString();
		}

		[CanBeNull]
		public static string GetDescription([NotNull] ConstructorInfo constructorInfo)
		{
			return ReflectionUtils.GetDescription(constructorInfo);
		}

		[CanBeNull]
		public static string GetDescription([NotNull] ParameterInfo parameterInfo)
		{
			return ReflectionUtils.GetDescription(parameterInfo, inherit: false);
		}

		[CanBeNull]
		public static string GetDescription([NotNull] PropertyInfo propertyInfo)
		{
			return ReflectionUtils.GetDescription(propertyInfo, inherit: false);
		}
	}
}
