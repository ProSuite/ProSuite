using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Platform-independent information provider for implementations of instance descriptor.
	/// </summary>
	public class InstanceInfo : InstanceInfoBase
	{
		private readonly int _constructorId;

		public InstanceInfo([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));
			InstanceUtils.AssertConstructorExists(type, constructorId);

			InstanceType = type;
			_constructorId = constructorId;
		}

		public InstanceInfo([NotNull] string assemblyName,
		                    [NotNull] string typeName,
		                    int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			InstanceType = InstanceUtils.LoadType(assemblyName, typeName, constructorId);
			_constructorId = constructorId;
		}

		[NotNull]
		public override Type InstanceType { get; }

		public override string TestDescription =>
			InstanceUtils.GetDescription(InstanceType, _constructorId);

		public override string[] TestCategories => InstanceUtils.GetCategories(InstanceType);

		public override string GetTestTypeDescription()
		{
			return InstanceType.Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return InstanceUtils.CreateParameters(InstanceType, _constructorId);
		}

		public override string ToString()
		{
			return
				$"Instance {InstanceType.Name} with parameters: {InstanceUtils.GetTestSignature(this)}";
		}
	}
}
