using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal class IncludedTestConstructor : IncludedTest,
	                                         IComparable<IncludedTestConstructor>
	{
		private readonly Type _testType;
		private readonly int _constructorIndex;

		public IncludedTestConstructor([NotNull] Type testType, int constructorIndex)
			: base(GetTitle(testType, constructorIndex),
			       TestDescriptorUtils.GetTestFactory(testType, constructorIndex),
			       testType.Assembly,
			       TestDescriptorUtils.IsObsolete(testType, constructorIndex),
			       TestDescriptorUtils.IsInternallyUsed(testType, constructorIndex))
		{
			_testType = testType;
			_constructorIndex = constructorIndex;
		}

		[NotNull]
		public override Type TestType
		{
			get { return _testType; }
		}

		#region IComparable<IncludedTestConstructor> Members

		public int CompareTo(IncludedTestConstructor other)
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

		public override string Key
		{
			get { return string.Format("{0}:{1}", _testType.FullName, _constructorIndex); }
		}

		public override string IndexTooltip
		{
			get { return TestFactory.GetTestDescription(); }
		}

		#endregion
	}
}