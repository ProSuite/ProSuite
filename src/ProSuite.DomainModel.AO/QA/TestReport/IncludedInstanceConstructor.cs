using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public class IncludedInstanceConstructor : IncludedInstance,
	                                       IComparable<IncludedInstanceConstructor>
	{
		private readonly Type _testType;
		private readonly int _constructorIndex;

		private IncludedInstanceConstructor([NotNull] Type testType, int constructorIndex)
			: base(GetTitle(testType, constructorIndex),
			       testType.Assembly,
			       TestFactoryUtils.GetTestFactory(testType, constructorIndex),
			       TestFactoryUtils.IsObsolete(testType, constructorIndex),
			       TestFactoryUtils.IsInternallyUsed(testType, constructorIndex))
		{
			_testType = testType;
			_constructorIndex = constructorIndex;
		}

		public static IncludedInstanceConstructor CreateInstance(
			[NotNull] Type testType, int constructorIndex)
		{
			AssertConstructorExists(testType, constructorIndex);

			return new IncludedInstanceConstructor(testType, constructorIndex);
		}

		//TODO: after push/pull subtree use InstanceUtils
		private static void AssertConstructorExists([NotNull] Type type, int constructorId)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (type.GetConstructors().Length <= constructorId)
			{
				throw new TypeLoadException(
					$"invalid constructorId {constructorId}, {type} has " +
					$"{type.GetConstructors().Length} constructors");
			}
		}

		[NotNull]
		public override Type TestType => _testType;

		#region IComparable<IncludedTestConstructor> Members

		public int CompareTo(IncludedInstanceConstructor other)
		{
			return base.CompareTo(other);
		}

		#endregion

		private static string GetTitle([NotNull] Type testType, int constructorIndex)
		{
			return string.Format("{0} - constructor index: {1}", testType.Name,
			                     constructorIndex);
		}

		#region Overrides of IncludedTestBase

		public override string Key => string.Format("{0}:{1}", _testType.FullName, ConstructorIndex);

		public override string IndexTooltip => InstanceFactory.GetTestDescription();

		public int ConstructorIndex => _constructorIndex;

		#endregion
	}
}
