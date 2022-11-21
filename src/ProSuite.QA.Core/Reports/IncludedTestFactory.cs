using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Core.Reports
{
	public class IncludedTestFactory : IncludedInstance, IComparable<IncludedTestFactory>
	{
		private readonly Type _testFactoryType;

		public IncludedTestFactory([NotNull] Type testFactoryType)
			: base(GetTitle(testFactoryType),
			       testFactoryType.Assembly,
			       GetTestFactory(testFactoryType),
			       InstanceUtils.IsObsolete(testFactoryType),
			       InstanceUtils.IsInternallyUsed(testFactoryType))
		{
			_testFactoryType = testFactoryType;
		}

		private static string GetTitle(Type testFactoryType)
		{
			return testFactoryType.Name;
		}

		private static IInstanceInfo GetTestFactory(Type testFactoryType)
		{
			ConstructorInfo ctor = testFactoryType.GetConstructors()[0];

			return (IInstanceInfo) ctor.Invoke(new object[] { });
		}

		#region Overrides of IncludedInstanceBase

		public override string Key => Assert.NotNull(_testFactoryType.FullName, "FullName");

		public override Type InstanceType => _testFactoryType;

		public override IList<IssueCode> IssueCodes =>
			IssueCodeUtils.GetIssueCodes(_testFactoryType);

		#endregion

		#region IComparable<IncludedTestFactory> Members

		public int CompareTo(IncludedTestFactory other)
		{
			return base.CompareTo(other);
		}

		#endregion
	}
}
