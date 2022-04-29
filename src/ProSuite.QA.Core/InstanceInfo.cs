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
		private readonly Type _instanceType;
		private readonly int _constructorId;

		public InstanceInfo([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));
			InstanceUtils.AssertConstructorExists(type, constructorId);

			_instanceType = type;
			_constructorId = constructorId;
		}

		public InstanceInfo([NotNull] string assemblyName,
		                    [NotNull] string typeName,
		                    int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			_instanceType = InstanceUtils.LoadType(assemblyName, typeName, constructorId);
			_constructorId = constructorId;
		}

		public override string GetTestTypeDescription()
		{
			return InstanceType.Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return InstanceUtils.CreateParameters(InstanceType, _constructorId);
		}

		[NotNull]
		private Type InstanceType => _instanceType;

		public override string[] TestCategories => ReflectionUtils.GetCategories(InstanceType);

		public override string GetTestDescription()
		{
			ConstructorInfo ctor = InstanceType.GetConstructors()[_constructorId];

			return InstanceUtils.GetDescription(ctor);
		}

		public override string ToString()
		{
			return
				$"Instance {InstanceType.Name} with parameters: {InstanceUtils.GetTestSignature(this)}";
		}
	}
}
