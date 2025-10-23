using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
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
using ProSuite.QA.Tests.Coincidence;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaTopoNotNearPolyFactory : QaFactoryBase
	{
		private static ITestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes =>
			_codes ?? (_codes = QaTopoNotNear.Codes); // TODO, QaIntersectsOther.Codes);

		public override string TestDescription => DocStrings.QaTopoNotNearPolyFactory;

		public const string FeatureClassParamName = "featureClass";
		public const string ReferenceParamName = "reference";
		public const string ReferenceSubtypesParamName = "referenceSubtypes";
		public const string FeaturesubtypeRulesParamName = "featuresubtypeRules";

		public static List<TestParameter> CreateParameterList()
		{
			var list =
				new List<TestParameter>
				{
					new TestParameter(FeatureClassParamName, typeof(IReadOnlyFeatureClass),
					                  DocStrings.QaTopoNotNearPolyFactory_featureClass),
					new TestParameter(ReferenceParamName, typeof(IReadOnlyFeatureClass),
					                  DocStrings.QaTopoNotNearPolyFactory_reference),
					new TestParameter(ReferenceSubtypesParamName, typeof(int[]),
					                  DocStrings.QaTopoNotNearPolyFactory_referenceSubtypes),
					new TestParameter(FeaturesubtypeRulesParamName, typeof(string[]),
					                  DocStrings.QaTopoNotNearPolyFactory_featuresubtypeRules)
				};

			return list;
		}

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);
			if (objParams.Length != 4)
			{
				throw new ArgumentException(string.Format("expected 4 parameter, got {0}",
				                                          objParams.Length));
			}

			if (objParams[0] is IReadOnlyFeatureClass == false)
			{
				throw new ArgumentException(string.Format("expected IReadOnlyFeatureClass, got {0}",
				                                          objParams[0].GetType()));
			}

			if (objParams[1] is IReadOnlyFeatureClass == false)
			{
				throw new ArgumentException(string.Format("expected IReadOnlyFeatureClass, got {0}",
				                                          objParams[1].GetType()));
			}

			if (objParams[2] is int[] == false)
			{
				throw new ArgumentException(string.Format("expected int[], got {0}",
				                                          objParams[2].GetType()));
			}

			if (objParams[3] is string[] == false)
			{
				throw new ArgumentException(string.Format("expected string[], got {0}",
				                                          objParams[3].GetType()));
			}

			var objects = new object[4];

			var featureClass = (IReadOnlyFeatureClass) objParams[0];
			var referenceClass = (IReadOnlyFeatureClass) objParams[1];
			var referenceSubtypes = (int[]) objParams[2];
			var rules = (string[]) objParams[3];

			objects[0] = featureClass;
			objects[1] = referenceClass;
			objects[2] = referenceSubtypes;
			objects[3] = rules;

			return objects;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			throw new InvalidOperationException(
				"The tests are created in the method CreateTestInstances");
		}

		protected class ConstrParams
		{
			public double MaxNear { get; private set; }
			public string FeatureClassNear { get; private set; }
			public List<string> RightSideNears { get; private set; }
			public string IgnoreNeighborCondition { get; private set; }

			public static ConstrParams Create(IReadOnlyFeatureClass featureClass,
			                                  IList<int> referenceSubtypes,
			                                  IList<string> featureClassRules)
			{
				string featureClassField = ((ISubtypes) featureClass).SubtypeFieldName;
				string referenceClassField = ((ISubtypes) featureClass).SubtypeFieldName;

				double maxNear = 0;

				StringBuilder sbNear = new StringBuilder();
				StringBuilder sbNearEnd = new StringBuilder();
				StringBuilder sbRightSideNear = new StringBuilder();
				StringBuilder sbRightSideNearEnd = new StringBuilder();
				StringBuilder sbIgnore = new StringBuilder();
				List<int> featureClassSubtypes = new List<int>();
				bool anyRightSide = false;
				foreach (string featureClassRule in featureClassRules)
				{
					string[] ruleParts = featureClassRule.Split(';');
					if (! int.TryParse(ruleParts[0], out int subtype))
					{
						throw new InvalidOperationException(
							$"invalid subtype '{ruleParts[0]}' in featureClassRule '{featureClassRule}' ");
					}

					featureClassSubtypes.Add(subtype);

					// handle default near
					if (! double.TryParse(ruleParts[1], out double near))
					{
						throw new InvalidOperationException(
							$"invalid near distance '{ruleParts[1]}' in featureClassRule '{featureClassRule}' ");
					}

					maxNear = Math.Max(maxNear, near);
					sbNear.Append($"IIF({featureClassField}={subtype},{near},");
					sbNearEnd.Append(")");

					// handle right side near
					double rightSideNear;
					if (! string.IsNullOrWhiteSpace(ruleParts[2]))
					{
						if (! double.TryParse(ruleParts[2], out rightSideNear))
						{
							throw new InvalidOperationException(
								$"invalid right side near distance '{ruleParts[2]}' in featureClassRule '{featureClassRule}' ");
						}

						anyRightSide = true;
						maxNear = Math.Max(maxNear, rightSideNear);
					}
					else
					{
						rightSideNear = near;
					}

					sbRightSideNear.Append($"IIF({featureClassField}={subtype},{rightSideNear},");
					sbRightSideNearEnd.Append(")");

					StringBuilder sbRefIgnore = new StringBuilder();
					foreach (int overlapIndex in
					         LineNotNearPolyOverlapConfigurator.EnumOverlapIndices(
						         ruleParts, featureClassRule, referenceSubtypes))
					{
						if (! int.TryParse(ruleParts[overlapIndex], out int overlapKey))
						{
							throw new InvalidOperationException(
								$"invalid rule '{ruleParts[overlapIndex]} in featureClassRule '{featureClassRule}'");
						}

						if (overlapKey == 0)
						{
							if (sbRefIgnore.Length > 0)
							{
								sbRefIgnore.Append(",");
							}

							sbRefIgnore.Append(
								$"{referenceSubtypes[overlapIndex - ReferenceSubtypesStart]}");
						}
					}

					if (sbRefIgnore.Length > 0)
					{
						if (sbIgnore.Length > 0)
						{
							sbIgnore.Append(" OR ");
						}

						sbIgnore.Append(
							$"(G1.{featureClassField} = {subtype} AND G2.{referenceClassField} IN ({sbRefIgnore}))");
					}
				}

				// prepare and create test parameters
				if (sbIgnore.Length > 0)
				{
					sbIgnore.Append(" OR ");
				}

				string fcSubtypesList =
					string.Concat(featureClassSubtypes.Select(x => $"{x},")).Trim(',');
				string refSubtypesList =
					string.Concat(referenceSubtypes.Select(x => $"{x},")).Trim(',');

				sbIgnore.Append($"(G1.{featureClassField} NOT IN ({fcSubtypesList})");
				sbIgnore.Append($" OR G2.{referenceClassField} NOT IN ({refSubtypesList}))");

				ConstrParams pars = new ConstrParams
				                    {
					                    MaxNear = maxNear,
					                    FeatureClassNear = $"{sbNear} 0 {sbNearEnd}",
					                    RightSideNears = anyRightSide
						                                     ? new List<string>
						                                       {
							                                       $"{sbRightSideNear} null {sbRightSideNearEnd}",
							                                       "0"
						                                       }
						                                     : null,
					                    IgnoreNeighborCondition = $"{sbIgnore}"
				                    };
				return pars;
			}
		}

		private static int ReferenceSubtypesStart =>
			LineNotNearPolyOverlapConfigurator.ReferenceSubtypesStart;

		protected override IList<ITest> CreateTestInstances(object[] args)
		{
			var featureClass = (IReadOnlyFeatureClass) args[0];
			var referenceClass = (IReadOnlyFeatureClass) args[1];
			var referenceSubtypes = (int[]) args[2];
			var featureClassRules = (string[]) args[3];

			ConstrParams pars =
				ConstrParams.Create(featureClass, referenceSubtypes, featureClassRules);
			var notNearTest = new QaTopoNotNear(featureClass, referenceClass, pars.MaxNear,
			                                    pars.FeatureClassNear,
			                                    "0", 0, 0, is3D: false);
			if (pars.RightSideNears != null)
			{
				notNearTest.RightSideNears = pars.RightSideNears;
			}

			notNearTest.IgnoreNeighborCondition = pars.IgnoreNeighborCondition;
			notNearTest.UnconnectedLineCapStyle = LineCapStyle.Butt;

			var intersectTest =
				new QaIntersectsOther(featureClass, referenceClass, pars.IgnoreNeighborCondition);

			return new List<ITest> { notNearTest, intersectTest };
		}

		public override string Export(QualityCondition qualityCondition)
		{
			LineNotNearPolyOverlapConfigurator.Matrix matrix =
				LineNotNearPolyOverlapConfigurator.Convert(qualityCondition);

			return matrix.ToCsv();
		}

		public override QualityCondition CreateQualityCondition(
			StreamReader file, IList<Dataset> datasets,
			IEnumerable<TestParameterValue> parameterValues)
		{
			Assert.ArgumentNotNull(file, nameof(file));
			Assert.ArgumentNotNull(datasets, nameof(datasets));
			Assert.ArgumentNotNull(parameterValues, nameof(parameterValues));

			var datasetFilter = new Dictionary<Dataset, string>();

			foreach (TestParameterValue oldValue in parameterValues)
			{
				if (oldValue.TestParameterName !=
				    FeatureClassParamName)
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

			LineNotNearPolyOverlapConfigurator.Matrix mat =
				LineNotNearPolyOverlapConfigurator.Matrix.Create(file);

			var config = new LineNotNearPolyOverlapConfigurator();

			QualityCondition qualityCondition = config.Convert(mat, datasets);

			foreach (TestParameterValue newValue in qualityCondition.ParameterValues)
			{
				if (newValue.TestParameterName !=
				    FeatureClassParamName)
				{
					continue;
				}

				var datasetTestParameterValue = (DatasetTestParameterValue) newValue;
				Dataset dataset = datasetTestParameterValue.DatasetValue;

				Assert.NotNull(dataset,
				               "Dataset parameter '{0}' in quality condition '{1}' does not refer to a dataset",
				               datasetTestParameterValue.TestParameterName,
				               qualityCondition.Name);

				if (datasetFilter.TryGetValue(dataset, out string filter))
				{
					datasetTestParameterValue.FilterExpression = filter;
				}
			}

			return qualityCondition;
		}
	}
}
