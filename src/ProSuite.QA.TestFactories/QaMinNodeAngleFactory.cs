using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[IntersectionParameterTest]
	public class QaMinNodeAngleFactory : QaAngleFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
		{
			get { return QaMinAngle.Codes; }
		}

		public override string GetTestTypeDescription()
		{
			return typeof(QaMinAngle).Name;
		}

		protected override ContainerTest CreateAngleTest(object[] args)
		{
			var test = new QaMinAngle((IList<IFeatureClass>) args[0], (double) args[1],
			                          (bool) args[2]);
			return test;
		}

		public override string GetTestDescription()
		{
			return DocStrings.QaMinNodeAngleFactory;
		}
	}
}
