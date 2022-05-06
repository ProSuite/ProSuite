using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public class IncludedInstanceConstructor : IncludedInstance,
	                                           IComparable<IncludedInstanceConstructor>
	{
		private readonly Type _instanceType;
		private readonly int _constructorIndex;

		private IncludedInstanceConstructor([NotNull] Type instanceType, int constructorIndex)
			: base(GetTitle(instanceType, constructorIndex),
			       instanceType.Assembly,
			       GetInstanceInfo(instanceType, constructorIndex),
			       InstanceFactoryUtils.IsObsolete(instanceType, constructorIndex),
			       InstanceFactoryUtils.IsInternallyUsed(instanceType, constructorIndex))
		{
			_instanceType = instanceType;
			_constructorIndex = constructorIndex;
		}

		public static IncludedInstanceConstructor CreateInstance(
			[NotNull] Type testType, int constructorIndex)
		{
			InstanceUtils.AssertConstructorExists(testType, constructorIndex);

			return new IncludedInstanceConstructor(testType, constructorIndex);
		}

		[NotNull]
		public override Type InstanceType => _instanceType;

		public int ConstructorIndex => _constructorIndex;

		private static string GetTitle([NotNull] Type testType, int constructorIndex)
		{
			return string.Format("{0} - constructor index: {1}", testType.Name, constructorIndex);
		}

		private static IInstanceInfo GetInstanceInfo(Type instanceType, int constructorIndex)
		{
			return new InstanceInfo(instanceType, constructorIndex);
		}

		public override string Key =>
			string.Format("{0}:{1}", _instanceType.FullName, ConstructorIndex);

		#region IComparable<IncludedTestConstructor> Members

		public int CompareTo(IncludedInstanceConstructor other)
		{
			return base.CompareTo(other);
		}

		#endregion
	}
}
