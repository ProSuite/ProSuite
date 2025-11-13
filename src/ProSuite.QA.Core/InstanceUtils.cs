using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.Core
{
	public static class InstanceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static string AssemblyDefinitionsSuffix => "Definitions";
		private static string AlgorithmDefinitionSuffix => "Definition";

		[NotNull]
		public static Type LoadType([NotNull] string assemblyName,
		                            [NotNull] string typeName,
		                            int constructorId)
		{
			Type result = PrivateAssemblyUtils.LoadType(assemblyName, typeName);

			if (result == null)
			{
				throw new TypeLoadException(
					$"{typeName} does not exist in {assemblyName}");
			}

			AssertConstructorExists(result, constructorId);

			return result;
		}

		/// <summary>
		/// Returns the name of the assembly containing the test/instance implementations for the
		/// specified assembly name containing the test/instance definitions.
		/// </summary>
		/// <param name="definitionAssembly"></param>
		/// <returns></returns>
		public static string GetImplementationAssemblyName(string definitionAssembly)
		{
			string assemblyDefinitionSuffix = $".{AssemblyDefinitionsSuffix}";

			if (definitionAssembly.EndsWith(assemblyDefinitionSuffix))
			{
				definitionAssembly =
					definitionAssembly.Remove(definitionAssembly.Length -
					                          assemblyDefinitionSuffix.Length);
			}

			return definitionAssembly;
		}

		/// <summary>
		/// Returns the name of the assembly containing the test/instance definitions for the
		/// specified assembly name containing the test/instance implementations, e.g.
		/// "ProSuite.QA.Tests" --> "ProSuite.QA.Tests.Definitions"
		/// </summary>
		public static string GetDefinitionsAssemblyName(string implementationAssemblyName)
		{
			// Substitute first. Definition based tests can never come from the legacy assemblies
			if (PrivateAssemblyUtils.KnownSubstitutes.TryGetValue(
				    implementationAssemblyName, out string substituteAssembly))
			{
				implementationAssemblyName = substituteAssembly;
			}

			if (implementationAssemblyName.EndsWith(AssemblyDefinitionsSuffix))
			{
				return implementationAssemblyName;
			}

			return $"{implementationAssemblyName}.{AssemblyDefinitionsSuffix}";
		}

		[CanBeNull]
		public static string TryGetAlgorithmName(string algorithmDefinitionName)
		{
			if (algorithmDefinitionName.EndsWith(AlgorithmDefinitionSuffix))
			{
				return algorithmDefinitionName.Substring(
					0, algorithmDefinitionName.Length - AlgorithmDefinitionSuffix.Length);
			}

			return null;
		}

		/// <summary>
		/// Returns test/instance definition name for the specified
		/// test/instance implementation name (both have the same namespace), e.g.
		/// "ProSuite.QA.Tests.QaConstraint" --> "ProSuite.QA.Tests.QaConstraintDefinition"
		/// (both share the same namespace)
		/// </summary>
		public static string GetAlgorithmDefinitionName(string algorithmName)
		{
			if (algorithmName.EndsWith(AlgorithmDefinitionSuffix))
			{
				return algorithmName;
			}

			return $"{algorithmName}{AlgorithmDefinitionSuffix}";
		}

		public static void AssertConstructorExists([NotNull] Type type, int constructorId)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (type.GetConstructors().Length <= constructorId)
			{
				throw new TypeLoadException(
					$"invalid constructorId {constructorId}, {type} has " +
					$"{type.GetConstructors().Length} constructors");
			}
		}

		public static IEnumerable<Tuple<Type, List<int>>> GetConstructorsByType(
			IEnumerable<Type> types,
			bool includeObsolete = false,
			bool includeInternallyUsed = false)
		{
			foreach (Type testType in types)
			{
				if (! IncludeType(testType))
				{
					continue;
				}

				var constructors = new List<int>();
				foreach (int ctorIndex in GetConstructorIndexes(
					         testType, includeObsolete, includeInternallyUsed))
				{
					constructors.Add(ctorIndex);
				}

				yield return new Tuple<Type, List<int>>(testType, constructors);
			}
		}

		public static IEnumerable<int> GetConstructorIndexes(Type instanceType,
		                                                     bool includeObsolete = false,
		                                                     bool includeInternallyUsed = false)
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

		public static T CreateInstance<T>([NotNull] Type type,
		                                  int constructorId,
		                                  object[] constructorArgs)
		{
			AssertConstructorExists(type, constructorId);

			ConstructorInfo constructor = type.GetConstructors()[constructorId];

			if (typeof(AlgorithmDefinition).IsAssignableFrom(type))
			{
				// Detour via definition;
				AlgorithmDefinition definition =
					(AlgorithmDefinition) constructor.Invoke(constructorArgs);

				return (T) definition.CreateInstance(definition);
			}

			return (T) constructor.Invoke(constructorArgs);
		}

		[NotNull]
		public static string GetParameterNameString([NotNull] TestParameter testParameter)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			return ! testParameter.IsConstructorParameter
				       ? string.Format("[{0}]", testParameter.Name)
				       : testParameter.Name;
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
		public static IList<TestParameter> CreateParameters([NotNull] Type type,
		                                                    int constructorId)
		{
			AssertConstructorExists(type, constructorId);

			ConstructorInfo constructor = type.GetConstructors()[constructorId];

			ParameterInfo[] constructorParameters = constructor.GetParameters();
			PropertyInfo[] properties = type.GetProperties();

			var testParameterProperties =
				new Dictionary<PropertyInfo, TestParameterAttribute>();

			foreach (PropertyInfo propertyInfo in properties)
			{
				if (! propertyInfo.CanRead || ! propertyInfo.CanWrite)
				{
					continue;
				}

				var testParameterAttribute =
					ReflectionUtils.GetAttribute<TestParameterAttribute>(propertyInfo);

				if (testParameterAttribute == null)
				{
					continue;
				}

				var isValid = true;
				foreach (ParameterInfo constructorParameter in constructorParameters)
				{
					if (string.Equals(constructorParameter.Name, propertyInfo.Name,
					                  StringComparison.InvariantCultureIgnoreCase))
					{
						isValid = false;

						_msg.Warn(GetMessageConstructorParameterExistsAlsoAsProperty(
							          type, constructorId, constructorParameter));
					}
				}

				if (isValid)
				{
					testParameterProperties.Add(propertyInfo, testParameterAttribute);
				}
			}

			var testParameters =
				new List<TestParameter>(constructorParameters.Length +
				                        testParameterProperties.Count);

			foreach (ParameterInfo parameter in constructorParameters)
			{
				var testParameter = new TestParameter(
					parameter.Name, parameter.ParameterType,
					GetDescription(parameter),
					isConstructorParameter: true);

				object defaultValue;
				if (ReflectionUtils.TryGetDefaultValue(parameter, out defaultValue))
				{
					testParameter.DefaultValue = defaultValue;
				}

				testParameters.Add(testParameter);
			}

			foreach (KeyValuePair<PropertyInfo, TestParameterAttribute> pair in
			         testParameterProperties)
			{
				PropertyInfo property = pair.Key;
				TestParameterAttribute attribute = pair.Value;

				var testParameter = new TestParameter(
					property.Name, property.PropertyType,
					GetDescription(property),
					isConstructorParameter: false);

				testParameter.DefaultValue = attribute.DefaultValue;

				testParameters.Add(testParameter);
			}

			return new ReadOnlyList<TestParameter>(testParameters);
		}

		[NotNull]
		public static string GetTestSignature([NotNull] IInstanceInfo instanceInfo)
		{
			Assert.ArgumentNotNull(instanceInfo, nameof(instanceInfo));

			var sb = new StringBuilder();

			foreach (TestParameter testParameter in instanceInfo.Parameters)
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
		public static string GetDescription([NotNull] Type type, int constructorId)
		{
			AssertConstructorExists(type, constructorId);

			ConstructorInfo constructor = type.GetConstructors()[constructorId];

			return ReflectionUtils.GetDescription(constructor);
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

		public static string[] GetCategories([NotNull] Type type)
		{
			return ReflectionUtils.GetCategories(type);
		}

		public static bool IsInstanceType([NotNull] Type candidateType, [NotNull] Type instanceType)
		{
			Assert.ArgumentNotNull(candidateType, nameof(candidateType));
			Assert.ArgumentNotNull(instanceType, nameof(instanceType));

			return instanceType.IsAssignableFrom(candidateType) &&
			       ! candidateType.IsAbstract &&
			       candidateType.IsPublic;
		}

		public static bool IncludeType(Type candidateType,
		                               bool includeObsolete = false,
		                               bool includeInternallyUsed = false)
		{
			if (candidateType.IsAbstract)
			{
				return false;
			}

			if (! candidateType.IsPublic)
			{
				return false;
			}

			if (! includeObsolete && IsObsolete(candidateType))
			{
				return false;
			}

			if (! includeInternallyUsed && IsInternallyUsed(candidateType))
			{
				return false;
			}

			return true;
		}

		private static bool IncludeConstructor(ConstructorInfo ctorInfo,
		                                       bool includeObsolete,
		                                       bool includeInternallyUsed)
		{
			if (! includeObsolete && ReflectionUtils.IsObsolete(ctorInfo, out _))
			{
				return false;
			}

			return includeInternallyUsed || ! HasInternallyUsedAttribute(ctorInfo);
		}

		public static bool IsObsolete([NotNull] Type type)
		{
			return IsObsolete(type, out _);
		}

		public static bool IsObsolete([NotNull] Type type,
		                              [CanBeNull] out string message)
		{
			return ReflectionUtils.IsObsolete(type, out message);
		}

		public static bool IsObsolete([NotNull] Type type, int constructorId)
		{
			return IsObsolete(type, constructorId, out _);
		}

		public static bool IsObsolete([NotNull] Type type,
		                              int constructorId,
		                              [CanBeNull] out string message)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (ReflectionUtils.IsObsolete(type, out message))
			{
				return true;
			}

			AssertConstructorExists(type, constructorId);

			ConstructorInfo ctorInfo = type.GetConstructors()[constructorId];

			return ReflectionUtils.IsObsolete(ctorInfo, out message);
		}

		public static bool IsInternallyUsed([NotNull] Type type)
		{
			return HasInternallyUsedAttribute(type);
		}

		public static bool IsInternallyUsed([NotNull] Type type, int constructorId)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (HasInternallyUsedAttribute(type))
			{
				return true;
			}

			AssertConstructorExists(type, constructorId);

			ConstructorInfo constructor = type.GetConstructors()[constructorId];

			return HasInternallyUsedAttribute(constructor);
		}

		public static bool HasInternallyUsedAttribute(
			[NotNull] ICustomAttributeProvider attributeProvider)
		{
			return ReflectionUtils.HasAttribute<InternallyUsedTestAttribute>(attributeProvider);
		}

		[NotNull]
		private static string GetMessageConstructorParameterExistsAlsoAsProperty(
			[NotNull] Type type,
			int constructorId,
			[NotNull] ParameterInfo constructorParameter)
		{
			var sb = new StringBuilder();

			sb.AppendFormat(
				"{0}({1}) has '{2}' as constructor parameter and as TestParameter property.",
				type.Name, constructorId, constructorParameter.Name);
			sb.AppendLine();

			sb.AppendFormat(
				"Parameter values for '{0}' will be assigned to constructor parameter.",
				constructorParameter.Name);
			sb.AppendLine();

			sb.AppendLine("Consider to use a different constructor for this test.");

			return sb.ToString();
		}
	}
}
