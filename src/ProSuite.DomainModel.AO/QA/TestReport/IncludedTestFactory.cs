using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.QA.Container;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal class IncludedTestFactory : IncludedTest, IComparable<IncludedTestFactory>
	{
		private readonly Type _testFactoryType;

		public IncludedTestFactory([NotNull] Type testFactoryType)
			: base(GetTitle(testFactoryType),
			       GetTestFactory(testFactoryType),
			       testFactoryType.Assembly,
			       ReflectionUtils.IsObsolete(testFactoryType),
			       TestDescriptorUtils.IsInternallyUsed(testFactoryType))
		{
			_testFactoryType = testFactoryType;
		}

		#region IComparable<IncludedTestFactory> Members

		public int CompareTo(IncludedTestFactory other)
		{
			return base.CompareTo(other);
		}

		#endregion

		private static string GetTitle(Type testFactoryType)
		{
			return testFactoryType.Name;
		}

		private static TestFactory GetTestFactory(Type testFactoryType)
		{
			ConstructorInfo ctor = testFactoryType.GetConstructors()[0];

			return (TestFactory) ctor.Invoke(new object[] { });
		}

		#region Overrides of IncludedTestBase

		public override string Key
		{
			get { return Assert.NotNull(_testFactoryType.FullName, "FullName"); }
		}

		public override string IndexTooltip
		{
			get { return TestFactory.GetTestDescription(); }
		}

		public override Type TestType
		{
			get { return _testFactoryType; }
		}

		public override IList<IssueCode> IssueCodes
		{
			get { return IssueCodeUtils.GetIssueCodes(_testFactoryType); }
		}

		#endregion
	}
}
