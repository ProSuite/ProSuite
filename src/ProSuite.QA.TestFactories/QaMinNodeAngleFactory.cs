using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[IntersectionParameterTest]
	public class QaMinNodeAngleFactory : QaAngleFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaMinAngle.Codes;

		public override string GetTestTypeDescription()
		{
			return typeof(QaMinAngle).Name;
		}

		protected override ContainerTest CreateAngleTest(object[] args)
		{
			var test = new QaMinAngle((IList<IReadOnlyFeatureClass>) args[0], (double) args[1],
			                          (bool) args[2]);
			return test;
		}

		public override string TestDescription => DocStrings.QaMinNodeAngleFactory;
	}
}
