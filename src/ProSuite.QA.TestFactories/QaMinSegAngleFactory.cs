using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Core;
using ProSuite.QA.Tests;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinSegAngleFactory : QaAngleFactory
	{
		private IList<TestParameter> _parameters;

		#region issue codes

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
		{
			get { return QaMinSegAngle.Codes; }
		}

		#endregion

		public override string GetTestTypeDescription()
		{
			return typeof(QaMinSegAngle).Name;
		}

		protected override ContainerTest CreateAngleTest(object[] args)
		{
			var test = new QaMinSegAngle((IReadOnlyFeatureClass) args[0], (double) args[1],
			                             (bool) args[2]);
			return test;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			if (_parameters == null)
			{
				var list = new List<TestParameter>
				           {
					           new TestParameter("featureClass", typeof(IReadOnlyFeatureClass),
					                             DocStrings.QaMinSegAngleFactory_featureClass),
					           new TestParameter("limit", typeof(double),
					                             DocStrings.QaMinSegAngleFactory_limit),
					           new TestParameter("is3D", typeof(bool),
					                             DocStrings.QaMinSegAngleFactory_is3D)
				           };

				_parameters = list.AsReadOnly();
			}

			SetParameters(_parameters);
			return _parameters;
		}

		public override string GetTestDescription()
		{
			return DocStrings.QaMinSegAngleFactory;
		}
	}
}
