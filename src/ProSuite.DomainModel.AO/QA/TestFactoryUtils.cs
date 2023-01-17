using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public static class TestFactoryUtils
	{
		/// <summary>
		/// Gets the test factory, sets the quality condition for it and initializes parameter values for it.
		/// </summary>
		/// <returns>TestFactory or null.</returns>
		[CanBeNull]
		public static TestFactory CreateTestFactory([NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			if (qualityCondition.TestDescriptor == null)
			{
				return null;
			}

			TestFactory factory = GetTestFactory(qualityCondition.TestDescriptor);

			if (factory != null)
			{
				factory.Condition = qualityCondition;

				InstanceConfigurationUtils.InitializeParameterValues(
					factory, qualityCondition);
			}

			return factory;
		}

		/// <summary>
		/// Gets the test factory. Requires the test class or the test factory descriptor to be defined.
		/// </summary>
		/// <param name="descriptor"></param>
		/// <returns>TestFactory or null if neither the test class nor the test factory descriptor are defined.</returns>
		[CanBeNull]
		//TODO Make private, use e.g. InstanceDescriptorUtils.GetInstanceInfo
		public static TestFactory GetTestFactory([NotNull] TestDescriptor descriptor)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			ClassDescriptor classDescriptor = descriptor.Class;

			if (classDescriptor != null)
			{
				return new DefaultTestFactory(classDescriptor.AssemblyName,
				                              classDescriptor.TypeName,
				                              descriptor.ConstructorId);
			}

			if (descriptor.TestFactoryDescriptor != null)
			{
				return descriptor.TestFactoryDescriptor.CreateInstance<TestFactory>();
			}

			return null;
		}

		public static bool IsTestFactoryType([NotNull] Type candidateType,
		                                     [NotNull] Type testFactoryType)
		{
			Assert.ArgumentNotNull(candidateType, nameof(candidateType));
			Assert.ArgumentNotNull(testFactoryType, nameof(testFactoryType));

			return testFactoryType.IsAssignableFrom(candidateType) &&
			       ! candidateType.IsAbstract &&
			       candidateType.IsPublic &&
			       candidateType.GetConstructors().Length == 1;
		}

		[NotNull]
		public static IEnumerable<Type> GetTestFactoryClasses([NotNull] Assembly assembly,
		                                                      bool includeObsolete,
		                                                      bool includeInternallyUsed)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			Type testFactoryType = typeof(TestFactory);

			foreach (Type candidateType in assembly.GetTypes())
			{
				if (! IsTestFactoryType(candidateType, testFactoryType))
				{
					continue;
				}

				if (! includeObsolete && InstanceUtils.IsObsolete(candidateType))
				{
					continue;
				}

				if (! includeInternallyUsed && InstanceUtils.IsInternallyUsed(candidateType))
				{
					continue;
				}

				yield return candidateType;
			}
		}

		[NotNull]
		public static IEnumerable<Type> GetTestClasses([NotNull] Assembly assembly,
		                                               bool includeObsolete,
		                                               bool includeInternallyUsed)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			Type testType = typeof(ITest);

			return InstanceFactoryUtils.GetClasses(assembly, testType, includeObsolete,
			                                       includeInternallyUsed);
		}

		[NotNull]
		public static string GetDefaultTestDescriptorName([NotNull] Type testType,
		                                                  int constructorIndex)
		{
			Assert.ArgumentNotNull(testType, nameof(testType));

			return string.Format("{0}({1})",
			                     GetTestDescriptorBaseName(testType),
			                     constructorIndex);
		}

		[NotNull]
		public static string GetDefaultTestDescriptorName([NotNull] Type testFactoryType)
		{
			Assert.ArgumentNotNull(testFactoryType, nameof(testFactoryType));

			return GetTestDescriptorBaseName(testFactoryType);
		}

		[NotNull]
		private static string GetTestDescriptorBaseName([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			string result = type.Name.Trim();

			if (result.Length > 2 &&
			    result.StartsWith("qa", StringComparison.InvariantCultureIgnoreCase))
			{
				result = result.Substring(2);
			}

			return result;
		}
	}
}
