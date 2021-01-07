using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	[CLSCompliant(false)]
	public static class TestDescriptorUtils
	{
		/// <summary>
		/// Gets the test implementation info. Requires the test class or the test factory descriptor to be defined.
		/// </summary>
		/// <param name="testDescriptor"></param>
		/// <returns>TestImplementationInfo or null if neither the test class nor the test factory descriptor are defined.</returns>
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
	}
}
