using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;

namespace EsriDE.ProSuite.DomainModel.QA
{
	[CLSCompliant(false)]
	public static class TestDescriptorUtils
	{
		/// <summary>
		/// Gets the test factory. Requires the test class or the test factory descriptor to be defined.
		/// </summary>
		/// <param name="testDescriptor"></param>
		/// <returns>TestFactory or null if neither the test class nor the test factory descriptor are defined.</returns>
		[CanBeNull]
		public static TestFactory GetTestFactory([NotNull] TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			if (testDescriptor.TestClass != null)
			{
				return new DefaultTestFactory(testDescriptor.TestClass.AssemblyName,
				                              testDescriptor.TestClass.TypeName,
				                              testDescriptor.TestConstructorId);
			}

			if (testDescriptor.TestFactoryDescriptor != null)
			{
				return testDescriptor.TestFactoryDescriptor.CreateInstance<TestFactory>();
			}

			return null;
		}

		[NotNull]
		public static DefaultTestFactory GetTestFactory([NotNull] Type testType,
		                                                int constructorIndex)
		{
			Assert.ArgumentNotNull(testType, nameof(testType));

			return new DefaultTestFactory(testType, constructorIndex);
		}

		public static bool IsTestType([NotNull] Type candidateType)
		{
			Type testType = typeof(ITest);

			return IsTestType(candidateType, testType);
		}

		public static bool IsTestType([NotNull] Type candidateType,
		                              [NotNull] Type testType)
		{
			Assert.ArgumentNotNull(candidateType, nameof(candidateType));

			return testType.IsAssignableFrom(candidateType) &&
			       ! candidateType.IsAbstract &&
			       candidateType.IsPublic;
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

				if (! includeObsolete && ReflectionUtils.IsObsolete(candidateType))
				{
					continue;
				}

				if (! includeInternallyUsed && HasInternallyUsedAttribute(candidateType))
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

			foreach (Type candidateType in assembly.GetTypes())
			{
				if (! IsTestType(candidateType, testType))
				{
					continue;
				}

				if (! includeObsolete && ReflectionUtils.IsObsolete(candidateType))
				{
					continue;
				}

				if (! includeInternallyUsed && HasInternallyUsedAttribute(candidateType))
				{
					continue;
				}

				yield return candidateType;
			}
		}

		[NotNull]
		public static IEnumerable<int> GetTestConstructorIndexes([NotNull] Type testType,
		                                                         bool includeObsolete,
		                                                         bool includeInternallyUsed)
		{
			Assert.ArgumentNotNull(testType, nameof(testType));

			var constructorIndex = 0;
			foreach (ConstructorInfo ctorInfo in testType.GetConstructors())
			{
				if (IncludeTestConstructor(ctorInfo, includeObsolete, includeInternallyUsed))
				{
					yield return constructorIndex;
				}

				constructorIndex++;
			}
		}

		public static bool IsObsolete([NotNull] Type testType, int constructorIndex)
		{
			string message;
			return IsObsolete(testType, constructorIndex, out message);
		}

		public static bool IsObsolete([NotNull] Type testType,
		                              int constructorIndex,
		                              [CanBeNull] out string message)
		{
			Assert.ArgumentNotNull(testType, nameof(testType));

			if (ReflectionUtils.IsObsolete(testType, out message))
			{
				return true;
			}

			ConstructorInfo ctorInfo = testType.GetConstructors()[constructorIndex];

			return ReflectionUtils.IsObsolete(ctorInfo, out message);
		}

		public static bool IsInternallyUsed([NotNull] Type testType, int constructorIndex)
		{
			Assert.ArgumentNotNull(testType, nameof(testType));

			if (IsInternallyUsed(testType))
			{
				return true;
			}

			ConstructorInfo ctorInfo = testType.GetConstructors()[constructorIndex];

			return HasInternallyUsedAttribute(ctorInfo);
		}

		public static bool IsInternallyUsed([NotNull] Type testType)
		{
			return HasInternallyUsedAttribute(testType);
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

		private static bool IncludeTestConstructor([NotNull] ConstructorInfo ctorInfo,
		                                           bool includeObsolete,
		                                           bool includeInternallyUsed)
		{
			if (! includeObsolete && ReflectionUtils.IsObsolete(ctorInfo))
			{
				return false;
			}

			return includeInternallyUsed || ! HasInternallyUsedAttribute(ctorInfo);
		}

		private static bool HasInternallyUsedAttribute(
			[NotNull] ICustomAttributeProvider attributeProvider)
		{
			return ReflectionUtils.HasAttribute<InternallyUsedTestAttribute>(attributeProvider);
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