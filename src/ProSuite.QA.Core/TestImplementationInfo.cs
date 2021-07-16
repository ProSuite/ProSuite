using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.QA.Core
{
	public class TestImplementationInfo : ITestImplementationInfo
	{
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

			_testType = InstanceUtils.LoadType(assemblyName, typeName, constructorId);
			_constructorId = constructorId;
		}

		public string GetTestTypeDescription()
		{
			return TestType.Name;
		}

		[NotNull]
		protected Type TestType => _testType;

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
					_parameters =
						InstanceUtils.CreateParameters(TestType, _constructorId);
				}

				return new ReadOnlyList<TestParameter>(_parameters);
			}
		}

		public string[] TestCategories => ReflectionUtils.GetCategories(TestType);

		public string GetTestDescription()
		{
			ConstructorInfo ctor = TestType.GetConstructors()[_constructorId];

			return InstanceUtils.GetDescription(ctor);
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
					return InstanceUtils.GetDescription(parameterInfo);
				}
			}

			foreach (PropertyInfo propertyInfo in TestType.GetProperties())
			{
				if (string.Equals(propertyInfo.Name, parameterName, stringComparison))
				{
					return InstanceUtils.GetDescription(propertyInfo);
				}
			}

			return null;
		}
	}
}
