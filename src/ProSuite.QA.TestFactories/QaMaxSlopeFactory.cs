using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Tests;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaMaxSlopeFactory : TestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaMaxSlope.Codes;

		private IList<TestParameter> _parameters;

		public override string GetTestTypeDescription()
		{
			return typeof(QaMaxSlope).Name;
		}

		public override string TestDescription => DocStrings.QaMaxSlopeFactory;

		protected override IList<TestParameter> CreateParameters()
		{
			if (_parameters == null)
			{
				var list = new List<TestParameter>
				           {
					           new TestParameter("featureClass", typeof(IReadOnlyFeatureClass),
					                             DocStrings.QaMaxSlopeFactory_featureClass),
					           new TestParameter("limit", typeof(double),
					                             DocStrings.QaMaxSlopeFactory_limit)
				           };

				_parameters = new ReadOnlyCollection<TestParameter>(list);
			}

			return _parameters;
		}

		protected void SetParameters(IList<TestParameter> parameters)
		{
			_parameters = new ReadOnlyCollection<TestParameter>(parameters);
		}

		protected override object[] Args(
			IOpenDataset datasetContext,
			IList<TestParameter> testParameters,
			out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);

			var objects = new object[2];
			objects[0] = objParams[0];
			objects[1] = (double) objParams[1] * Math.PI / 180.0;

			return objects;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			ContainerTest containerTest = new QaMaxSlope((IReadOnlyFeatureClass) args[0],
			                                             (double) args[1]);
			containerTest.AngleUnit = AngleUnit.Degree;
			return containerTest;
		}
	}
}
