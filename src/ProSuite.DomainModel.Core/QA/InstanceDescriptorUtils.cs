using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	public static class InstanceDescriptorUtils
	{
		/// <summary>
		/// Gets the test implementation info. Requires the test class or the test factory descriptor to be defined.
		/// </summary>
		/// <param name="testDescriptor"></param>
		/// <returns>InstanceInfo or null if neither the test class nor the test factory descriptor are defined.</returns>
		[CanBeNull]
		public static IInstanceInfo GetInstanceInfo([NotNull] TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			if (testDescriptor.TestClass != null)
			{
				return new InstanceInfo(testDescriptor.TestClass.AssemblyName,
				                        testDescriptor.TestClass.TypeName,
				                        testDescriptor.TestConstructorId);
			}

			if (testDescriptor.TestFactoryDescriptor != null)
			{
				return testDescriptor.TestFactoryDescriptor
				                     .CreateInstance<IInstanceInfo>();
			}

			return null;
		}

		[CanBeNull]
		public static IInstanceInfo GetInstanceInfo([NotNull] InstanceDescriptor descriptor)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			if (descriptor is TestDescriptor testDescriptor)
			{
				return GetInstanceInfo(testDescriptor);
			}

			if (descriptor.Class != null)
			{
				return new InstanceInfo(descriptor.Class.AssemblyName,
				                        descriptor.Class.TypeName,
				                        descriptor.ConstructorId);
			}

			return null;
		}
	}
}
