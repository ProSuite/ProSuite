using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Core;
using ProSuite.QA.Tests;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[IntersectionParameterTest]
	public class QaLineIntersectAngleFactory : QaAngleFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaLineIntersectAngle.Codes;

		public override string GetTestTypeDescription()
		{
			return typeof(QaLineIntersectAngle).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			IList<TestParameter> list = base.CreateParameters();
			list[0].Description = DocStrings.QaLineIntersectAngleFactory_featureClasses;
			return list;
		}

		public override string GetTestDescription()
		{
			return DocStrings.QaLineIntersectAngleFactory;
		}

		protected override ContainerTest CreateAngleTest(object[] args)
		{
			ContainerTest test = new QaLineIntersectAngle(
				(IList<IFeatureClass>) args[0], (double) args[1], (bool) args[2]);

			return test;
		}
	}
}
