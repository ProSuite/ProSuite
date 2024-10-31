using System;
using System.Collections.Generic;
using System.Text;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaDangleFactory : QaFactoryBase
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaConnections.Codes;

		protected override object[] Args(
			[NotNull] IOpenDataset datasetContext,
			[NotNull] IList<TestParameter> testParameters,
			[NotNull] out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);
			if (objParams.Length != 1)
			{
				throw new ArgumentException(string.Format("expected 1 parameter, got {0}",
				                                          objParams.Length));
			}

			if (objParams[0] is IReadOnlyFeatureClass[] == false)
			{
				throw new ArgumentException(string.Format(
					                            "expected IReadOnlyFeatureClass[], got {0}",
					                            objParams[0].GetType()));
			}

			var objects = new object[2];
			objects[0] = objParams[0];

			var featureClasses = (IReadOnlyFeatureClass[]) objParams[0];

			int featureClassCount = featureClasses.Length;
			var rules = new string[featureClassCount];
			var constraint = new StringBuilder();

			for (int featureClassIndex = 0;
			     featureClassIndex < featureClassCount;
			     featureClassIndex++)
			{
				string var = string.Format("m{0}", featureClassIndex);

				rules[featureClassIndex] = string.Format("true; {0}: true", var);

				if (constraint.Length > 0)
				{
					constraint.Append(" + ");
				}

				constraint.AppendFormat("{0}", var);
			}

			rules[0] = string.Format("{0}; {1} > 1", rules[0], constraint);

			objects[0] = featureClasses;
			objects[1] = new[] {rules};

			return objects;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			var featureClasses = (IReadOnlyFeatureClass[]) args[0];
			var rules = (IList<string[]>) args[1];

			return new QaConnections(featureClasses, rules);
		}
	}
}
