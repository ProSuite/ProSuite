using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.DomainModel.AO.QA
{
	public static class InstanceFactoryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Gets the row filter factory, sets the row filter configuration for it and initializes
		/// its  parameter values.
		/// </summary>
		/// <returns>RowFilterFactory or null.</returns>
		[CanBeNull]
		public static RowFilterFactory CreateRowFilterFactory(
			[NotNull] RowFilterConfiguration rowFilterConfiguration)
		{
			Assert.ArgumentNotNull(rowFilterConfiguration, nameof(rowFilterConfiguration));

			if (rowFilterConfiguration.RowFilterDescriptor == null)
			{
				return null;
			}

			RowFilterFactory factory =
				CreateRowFilterFactory(rowFilterConfiguration.RowFilterDescriptor);

			if (factory != null)
			{
				InitializeParameterValues(factory, rowFilterConfiguration.ParameterValues);
			}

			return factory;
		}

		/// <summary>
		/// Gets the transformer factory, sets the transformer configuration and initializes its 
		/// parameter values.
		/// </summary>
		/// <returns>TransformerFactory or null.</returns>
		[CanBeNull]
		public static TransformerFactory CreateTransformerFactory(
			[NotNull] TransformerConfiguration transformerConfiguration)
		{
			Assert.ArgumentNotNull(transformerConfiguration, nameof(transformerConfiguration));

			if (transformerConfiguration.TransformerDescriptor == null)
			{
				return null;
			}

			TransformerFactory factory =
				CreateTransformerFactory(transformerConfiguration.TransformerDescriptor);

			if (factory != null)
			{
				InitializeParameterValues(factory, transformerConfiguration.ParameterValues);
			}

			return factory;
		}

		public static void InitializeParameterValues(
			[NotNull] InstanceFactory factory,
			[NotNull] IEnumerable<TestParameterValue> parameterValues)
		{
			Dictionary<string, TestParameter> parametersByName =
				factory.Parameters.ToDictionary(testParameter => testParameter.Name);

			foreach (TestParameterValue parameterValue in parameterValues)
			{
				if (parametersByName.TryGetValue(parameterValue.TestParameterName,
				                                 out TestParameter testParameter))
				{
					parameterValue.DataType = testParameter.Type;
				}
				else
				{
					_msg.WarnFormat(
						"Test parameter value {0}: No parameter found in {1}. The constructor Id might be incorrect.",
						parameterValue.TestParameterName, factory);
				}
			}
		}

		[NotNull]
		public static IEnumerable<Type> GetTransformerClasses([NotNull] Assembly assembly,
		                                                      bool includeObsolete,
		                                                      bool includeInternallyUsed)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			Type transformerType = typeof(ITableTransformer);

			foreach (Type candidateType in assembly.GetTypes())
			{
				if (! IsInstanceType(candidateType, transformerType))
				{
					continue;
				}

				if (! includeObsolete && IsObsolete(candidateType))
				{
					continue;
				}

				if (! includeInternallyUsed && IsInternallyUsed(candidateType))
				{
					continue;
				}

				yield return candidateType;
			}
		}

		[NotNull]
		public static IEnumerable<int> GetConstructorIndexes([NotNull] Type instanceType,
		                                                     bool includeObsolete,
		                                                     bool includeInternallyUsed)
		{
			Assert.ArgumentNotNull(instanceType, nameof(instanceType));

			var constructorIndex = 0;
			foreach (ConstructorInfo ctorInfo in instanceType.GetConstructors())
			{
				if (IncludeConstructor(ctorInfo, includeObsolete, includeInternallyUsed))
				{
					yield return constructorIndex;
				}

				constructorIndex++;
			}
		}

		private static bool IncludeConstructor([NotNull] ConstructorInfo ctorInfo,
		                                       bool includeObsolete,
		                                       bool includeInternallyUsed)
		{
			if (! includeObsolete && ReflectionUtils.IsObsolete(ctorInfo, out _))
			{
				return false;
			}

			return includeInternallyUsed || ! HasInternallyUsedAttribute(ctorInfo);
		}

		public static bool IsInstanceType([NotNull] Type candidateType, [NotNull] Type instanceType)
		{
			Assert.ArgumentNotNull(candidateType, nameof(candidateType));
			Assert.ArgumentNotNull(instanceType, nameof(instanceType));

			return instanceType.IsAssignableFrom(candidateType) &&
			       ! candidateType.IsAbstract &&
			       candidateType.IsPublic;
		}

		public static bool IsObsolete([NotNull] Type instanceType)
		{
			return ReflectionUtils.IsObsolete(instanceType, out _);
		}

		public static bool IsObsolete([NotNull] Type instanceType, int constructorId)
		{
			return IsObsolete(instanceType, constructorId, out _);
		}

		public static bool IsObsolete([NotNull] Type instanceType,
		                              int constructorId,
		                              [CanBeNull] out string message)
		{
			Assert.ArgumentNotNull(instanceType, nameof(instanceType));

			if (ReflectionUtils.IsObsolete(instanceType, out message))
			{
				return true;
			}

			InstanceUtils.AssertConstructorExists(instanceType, constructorId);

			ConstructorInfo ctorInfo = instanceType.GetConstructors()[constructorId];

			return ReflectionUtils.IsObsolete(ctorInfo, out message);
		}

		public static bool IsInternallyUsed([NotNull] Type instanceType)
		{
			return HasInternallyUsedAttribute(instanceType);
		}

		public static bool IsInternallyUsed([NotNull] Type instanceType, int constructorId)
		{
			Assert.ArgumentNotNull(instanceType, nameof(instanceType));

			if (HasInternallyUsedAttribute(instanceType))
			{
				return true;
			}

			InstanceUtils.AssertConstructorExists(instanceType, constructorId);

			ConstructorInfo ctorInfo = instanceType.GetConstructors()[constructorId];

			return HasInternallyUsedAttribute(ctorInfo);
		}

		private static bool HasInternallyUsedAttribute(
			[NotNull] ICustomAttributeProvider attributeProvider)
		{
			return ReflectionUtils.HasAttribute<InternallyUsedTestAttribute>(attributeProvider);
		}

		private static RowFilterFactory CreateRowFilterFactory(
			[NotNull] RowFilterDescriptor rowFilterDescriptor)
		{
			ClassDescriptor classDescriptor = rowFilterDescriptor.Class;

			return classDescriptor != null
				       ? new RowFilterFactory(classDescriptor.AssemblyName,
				                              classDescriptor.TypeName,
				                              rowFilterDescriptor.ConstructorId)
				       : null;
		}

		private static TransformerFactory CreateTransformerFactory(
			[NotNull] TransformerDescriptor transformerDescriptor)
		{
			ClassDescriptor classDescriptor = transformerDescriptor.Class;

			return classDescriptor != null
				       ? new TransformerFactory(classDescriptor.AssemblyName,
				                                classDescriptor.TypeName,
				                                transformerDescriptor.ConstructorId)
				       : null;
		}
	}
}
