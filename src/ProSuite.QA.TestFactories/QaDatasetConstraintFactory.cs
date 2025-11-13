using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.Constraints;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaDatasetConstraintFactory : QaFactoryBase
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaConstraint.Codes;

		protected override object[] Args(
			IOpenDataset datasetContext,
			IList<TestParameter> testParameters,
			out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);
			if (objParams.Length != 2)
			{
				throw new ArgumentException(string.Format("expected 2 parameter, got {0}",
				                                          objParams.Length));
			}

			if (objParams[0] is IReadOnlyTable == false)
			{
				throw new ArgumentException(string.Format("expected IReadOnlyTable, got {0}",
				                                          objParams[0].GetType()));
			}

			if (objParams[1] is IList<string> == false)
			{
				throw new ArgumentException(string.Format("expected IList<string>, got {0}",
				                                          objParams[1].GetType()));
			}

			var objects = new object[2];

			IList<ConstraintNode> nodes =
				HierarchicalConstraintUtils.GetConstraintHierarchy((IList<string>) objParams[1]);

			objects[0] = objParams[0];
			objects[1] = nodes;

			return objects;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			var test = new QaConstraint((IReadOnlyTable) args[0],
			                            (IList<ConstraintNode>) args[1]);
			return test;
		}

		public override string Export(QualityCondition qualityCondition)
		{
			var config = new DatasetConstraintConfigurator(
				qualityCondition.ParameterValues);

			return config.ToCsv();
		}

		public override QualityCondition CreateQualityCondition(
			StreamReader file, IList<Dataset> datasets,
			IEnumerable<TestParameterValue> parameterValues)
		{
			DatasetConstraintConfigurator config =
				DatasetConstraintConfigurator.Create(file, datasets);

			QualityCondition qc = config.ToQualityCondition();
			return qc;
		}
	}
}
