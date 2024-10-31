using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaLineConnection : QaFactoryBase
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaConnections.Codes;

		public override string GetTestTypeDescription()
		{
			return typeof(QaConnections).Name;
		}

		public override string TestDescription => DocStrings.QaLineConnection;

		//protected override IList<TestParameter> CreateParameters()
		//{
		//	var list =
		//		new List<TestParameter>
		//		{
		//			new TestParameter("featureClasses", typeof(IReadOnlyFeatureClass[]),
		//							  DocStrings.QaLineConnection_featureClasses),
		//			new TestParameter("rules", typeof(string[]),
		//							  DocStrings.QaLineConnection_rules)
		//		};

		//	return list.AsReadOnly();
		//}

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);
			if (objParams.Length != 2)
			{
				throw new ArgumentException(string.Format("expected 2 parameter, got {0}",
				                                          objParams.Length));
			}

			if (objParams[0] is IReadOnlyFeatureClass[] == false)
			{
				throw new ArgumentException(string.Format(
					                            "expected IReadOnlyFeatureClass[], got {0}",
					                            objParams[0].GetType()));
			}

			if (objParams[1] is string[] == false)
			{
				throw new ArgumentException(string.Format("expected string[], got {0}",
				                                          objParams[1].GetType()));
			}

			var objects = new object[2];
			objects[0] = objParams[0];

			var featureClasses = (IReadOnlyFeatureClass[]) objParams[0];
			var ruleParts = (string[]) objParams[1];

			objects[0] = featureClasses;
			objects[1] = GetRuleArrays(featureClasses, ruleParts);

			return objects;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			var featureClasses = (IReadOnlyFeatureClass[]) args[0];
			var rules = (List<string[]>) args[1];

			return new QaConnections(featureClasses, rules);
		}

		public override string Export(QualityCondition qualityCondition)
		{
			var configurator = new LineConnectionConfigurator();
			LineConnectionConfigurator.Matrix matrix = configurator.Convert(qualityCondition);

			return matrix.ToCsv();
		}

		public override QualityCondition CreateQualityCondition(
			StreamReader file,
			IList<Dataset> datasets,
			IEnumerable<TestParameterValue> parameterValues)
		{
			Assert.ArgumentNotNull(file, nameof(file));
			Assert.ArgumentNotNull(datasets, nameof(datasets));
			Assert.ArgumentNotNull(parameterValues, nameof(parameterValues));

			var datasetFilter = new Dictionary<Dataset, string>();

			foreach (TestParameterValue oldValue in parameterValues)
			{
				if (oldValue.TestParameterName !=
				    LineConnectionConfigurator.FeatureClassesParamName)
				{
					continue;
				}

				var dsValue = (DatasetTestParameterValue) oldValue;
				if (string.IsNullOrEmpty(dsValue.FilterExpression))
				{
					continue;
				}

				Dataset dataset = dsValue.DatasetValue;
				Assert.NotNull(dataset, "Dataset parameter {0} does not refer to a dataset",
				               dsValue.TestParameterName);

				datasetFilter.Add(dataset, dsValue.FilterExpression);
			}

			LineConnectionConfigurator.Matrix mat =
				LineConnectionConfigurator.Matrix.Create(file);

			var config = new LineConnectionConfigurator();

			QualityCondition qualityCondition = config.Convert(mat, datasets);

			foreach (TestParameterValue newValue in qualityCondition.ParameterValues)
			{
				if (newValue.TestParameterName !=
				    LineConnectionConfigurator.FeatureClassesParamName)
				{
					continue;
				}

				var datasetTestParameterValue = (DatasetTestParameterValue) newValue;
				Dataset dataset = datasetTestParameterValue.DatasetValue;

				Assert.NotNull(dataset,
				               "Dataset parameter '{0}' in quality condition '{1}' does not refer to a dataset",
				               datasetTestParameterValue.TestParameterName,
				               qualityCondition.Name);

				string filter;
				if (datasetFilter.TryGetValue(dataset, out filter))
				{
					datasetTestParameterValue.FilterExpression = filter;
				}
			}

			return qualityCondition;
		}

		[NotNull]
		private static List<string[]> GetRuleArrays(
			[NotNull] ICollection<IReadOnlyFeatureClass> featureClasses,
			[NotNull] IList<string> ruleParts)
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));
			Assert.ArgumentNotNull(ruleParts, nameof(ruleParts));

			int classCount = featureClasses.Count;
			int rulePartCount = ruleParts.Count;

			if (rulePartCount % classCount != 0)
			{
				throw new ArgumentException(
					string.Format(
						"Expected # of rules : n x {0} (=# of feature classes), actual # of rules {1}",
						classCount, rulePartCount));
			}

			int rulesPerClassCount = rulePartCount / classCount;

			var result = new List<string[]>();

			int rulePartIndex = 0;

			for (int ruleIndex = 0; ruleIndex < rulesPerClassCount; ruleIndex++)
			{
				var ruleArray = new string[classCount];

				for (int classIndex = 0; classIndex < classCount; classIndex++)
				{
					ruleArray[classIndex] = ruleParts[rulePartIndex];
					rulePartIndex++;
				}

				result.Add(ruleArray);
			}

			return result;
		}
	}
}
