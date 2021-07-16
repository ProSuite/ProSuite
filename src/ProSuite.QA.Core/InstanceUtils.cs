using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;

namespace ProSuite.QA.Core
{
	public static class InstanceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

			if (result.GetConstructors().Length <= constructorId)
			{
				throw new TypeLoadException(
					$"invalid constructorId {constructorId}, {typeName} has " +
					$"{result.GetConstructors().Length} constructors");
			}

			return result;
		}

		public static T CreateInstance<T>([NotNull] Type type,
		                                  int constructorId,
		                                  object[] constructorArgs)
		{
			ConstructorInfo constructor = type.GetConstructors()[constructorId];

			return (T) constructor.Invoke(constructorArgs);
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
			ConstructorInfo constr = type.GetConstructors()[constructorId];

			ParameterInfo[] constructorParameters = constr.GetParameters();
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
