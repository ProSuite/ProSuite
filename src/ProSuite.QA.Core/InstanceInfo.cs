using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Platform-independent information provider for implementations of instance descriptor.
	/// </summary>
	public class InstanceInfo : InstanceInfoBase
	{
		private readonly Type _testType;
		private readonly int _constructorId;

		public InstanceInfo([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			_testType = type;
			_constructorId = constructorId;
		}

		public InstanceInfo([NotNull] string assemblyName,
		                    [NotNull] string typeName,
		                    int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			_testType = InstanceUtils.LoadType(assemblyName, typeName, constructorId);
			_constructorId = constructorId;
		}

		public override string GetTestTypeDescription()
		{
			return TestType.Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return InstanceUtils.CreateParameters(TestType, _constructorId);
		}

		[NotNull]
		private Type TestType => _testType;

		public override string[] TestCategories => ReflectionUtils.GetCategories(TestType);

		public override string GetTestDescription()
		{
			ConstructorInfo ctor = TestType.GetConstructors()[_constructorId];

			return InstanceUtils.GetDescription(ctor);
		}
	}
}
