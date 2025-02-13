using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public class DefaultTestFactory : TestFactory
	{
		[UsedImplicitly] [NotNull] private readonly Type _testType;
		[UsedImplicitly] private readonly int _constructorId;

		public DefaultTestFactory([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));
			InstanceUtils.AssertConstructorExists(type, constructorId);

			_testType = type;
			_constructorId = constructorId;
		}

		public DefaultTestFactory([NotNull] string assemblyName,
		                          [NotNull] string typeName,
		                          int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			_testType = InstanceUtils.LoadType(assemblyName, typeName, constructorId);
			_constructorId = constructorId;
		}

		[NotNull]
		protected Type TestType => _testType;

		public override string TestDescription =>
			InstanceUtils.GetDescription(TestType, _constructorId);

		public override string[] TestCategories => InstanceUtils.GetCategories(TestType);

		public override string GetTestTypeDescription()
		{
			return TestType.Name;
		}

		public T CreateInstance<T>(IOpenDataset context)
			where T : IInvolvesTables
		{
			IList<T> created = Create(context, Parameters,
			                          args => new[] {CreateInstance<T>(args)});
			return created[0];
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			return CreateInstance<ITest>(args);
		}

		private T CreateInstance<T>(object[] args)
		{
			return InstanceUtils.CreateInstance<T>(TestType, _constructorId, args);
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return InstanceUtils.CreateParameters(TestType, _constructorId);
		}

		public override string GetParameterDescription(string parameterName)
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
