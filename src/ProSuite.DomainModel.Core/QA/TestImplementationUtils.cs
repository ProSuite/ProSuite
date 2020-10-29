using System;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	[CLSCompliant(false)]
	public static class TestImplementationUtils
	{
		/// <summary>
		/// Gets the test factory. Requires the test class or the test factory descriptor to be defined.
		/// </summary>
		/// <param name="testDescriptor"></param>
		/// <returns>TestFactory or null if neither the test class nor the test factory descriptor are defined.</returns>
		[CanBeNull]
		public static ITestImplementationInfo GetTestImplementationInfo(
			[NotNull] TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			if (testDescriptor.TestClass != null)
			{
				return new TestImplementationInfo(testDescriptor.TestClass.AssemblyName,
				                                  testDescriptor.TestClass.TypeName,
				                                  testDescriptor.TestConstructorId);
			}

			if (testDescriptor.TestFactoryDescriptor != null)
			{
				return testDescriptor.TestFactoryDescriptor
				                     .CreateInstance<ITestImplementationInfo>();
			}

			return null;
		}

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
	}
}
