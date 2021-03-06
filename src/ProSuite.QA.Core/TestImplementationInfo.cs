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
	public class TestImplementationInfo : ITestImplementationInfo
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Type _testType;
		private readonly int _constructorId;

		private IList<TestParameter> _parameters;

		public TestImplementationInfo([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			_testType = type;
			_constructorId = constructorId;
		}

		public TestImplementationInfo([NotNull] string assemblyName,
		                              [NotNull] string typeName,
		                              int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			_testType = PrivateAssemblyUtils.LoadType(assemblyName, typeName);

			if (_testType == null)
			{
				throw new TypeLoadException(
					string.Format("{0} does not exist in {1}", typeName, assemblyName));
			}

			if (TestType.GetConstructors().Length <= constructorId)
			{
				throw new TypeLoadException(
					string.Format("invalid constructorId {0}, {1} has {2} constructors",
					              constructorId, typeName, TestType.GetConstructors().Length));
			}

			_constructorId = constructorId;
		}

		public string GetTestTypeDescription()
		{
			return TestType.Name;
		}

		[NotNull]
		protected Type TestType => _testType;

		private IList<TestParameter> CreateParameters()
		{
			return CreateParameters(TestType, _constructorId);
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
					ReflectionUtils.GetAttribute<TestParameterAttribute>(
						propertyInfo);

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
					TestImplementationUtils.GetDescription(parameter),
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
					TestImplementationUtils.GetDescription(property),
					isConstructorParameter: false);

				testParameter.DefaultValue = attribute.DefaultValue;

				testParameters.Add(testParameter);
			}

			return new ReadOnlyList<TestParameter>(testParameters);
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

		public TestParameter GetParameter(string parameterName)
		{
			throw new NotImplementedException();
		}

		[NotNull]
		public IList<TestParameter> Parameters
		{
			get
			{
				if (_parameters == null)
				{
					_parameters = CreateParameters();
				}

				return new ReadOnlyList<TestParameter>(_parameters);
			}
		}

		public string[] TestCategories => ReflectionUtils.GetCategories(TestType);

		public string GetTestDescription()
		{
			ConstructorInfo ctor = TestType.GetConstructors()[_constructorId];

			return TestImplementationUtils.GetDescription(ctor);
		}

		public string GetParameterDescription(string parameterName)
		{
			ConstructorInfo ctor = TestType.GetConstructors()[_constructorId];

			// TODO: revise, case-insensitive match is ok? (parameter name search is insensitive elsewhere)
			const StringComparison stringComparison = StringComparison.OrdinalIgnoreCase;

			foreach (ParameterInfo parameterInfo in ctor.GetParameters())
			{
				if (string.Equals(parameterInfo.Name, parameterName, stringComparison))
				{
					return TestImplementationUtils.GetDescription(parameterInfo);
				}
			}

			foreach (PropertyInfo propertyInfo in TestType.GetProperties())
			{
				if (string.Equals(propertyInfo.Name, parameterName, stringComparison))
				{
					return TestImplementationUtils.GetDescription(propertyInfo);
				}
			}

			return null;
		}
	}
}
