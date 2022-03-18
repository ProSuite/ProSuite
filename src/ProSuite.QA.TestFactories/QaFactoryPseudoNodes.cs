using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;
using ProSuite.QA.Tests;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[InternallyUsedTest]
	public class QaFactoryPseudoNodes : TestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
		{
			get { return QaPseudoNodes.Codes; }
		}

		public static string TestDescription
		{
			get { return DocStrings.QaFactoryPseudoNodes; }
		}

		public static readonly string PolylineClassesParam = "polylineClasses";
		public static readonly string IgnoreFieldsParam = "ignoreFields";
		public static readonly string ValidPseudoNodesParam = "validPseudoNodes";
		public static readonly string IgnoreLoopEndPointsParam = "IgnoreLoopEndpoints";
		public static readonly string EndLayerFields = "-";

		public override string GetTestTypeDescription()
		{
			return typeof(QaPseudoNodes).Name;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			var test = new QaPseudoNodes((IList<IReadOnlyFeatureClass>) args[0],
			                             (string[][]) args[1],
			                             (IList<IReadOnlyFeatureClass>) args[2]);

			if (args.Length > 3 && args[3] is bool)
			{
				test.IgnoreLoopEndpoints = (bool) args[3];
			}

			return test;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			var list = new List<TestParameter>
			           {
				           new TestParameter(PolylineClassesParam, typeof(IReadOnlyFeatureClass[]),
				                             DocStrings.QaFactoryPseudoNodes_polylineClasses),
				           new TestParameter(IgnoreFieldsParam, typeof(string[]),
				                             DocStrings.QaFactoryPseudoNodes_ignoreFields),
				           new TestParameter(ValidPseudoNodesParam, typeof(IReadOnlyFeatureClass[]),
				                             DocStrings.QaFactoryPseudoNodes_validPseudoNodes),
				           new TestParameter(IgnoreLoopEndPointsParam, typeof(bool),
				                             DocStrings.QaFactoryPseudoNodes_IgnoreLoopEndpoints,
				                             false)
			           };

			return new ReadOnlyCollection<TestParameter>(list);
		}

		public override string GetTestDescription()
		{
			return TestDescription;
		}

		protected override object[] Args(
			IOpenDataset datasetContext,
			IList<TestParameter> testParameters,
			out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);

			if (objParams.Length != 3)
			{
				throw new ArgumentException(string.Format("expected 3 parameters, got {0}",
				                                          objParams.Length));
			}

			if (! (objParams[0] is IReadOnlyFeatureClass[]))
			{
				throw new ArgumentException(string.Format("expected IFeatureClass[], got {0}",
				                                          objParams[0].GetType()));
			}

			if (! (objParams[1] is string[]))
			{
				throw new ArgumentException(string.Format("expected string[], got {0}",
				                                          objParams[1].GetType()));
			}

			if (! (objParams[2] is IReadOnlyFeatureClass[]))
			{
				throw new ArgumentException(string.Format("expected IFeatureClass[], got {0}",
				                                          objParams[2].GetType()));
			}

			object ignoreLoopEndpoints = GetIgnoreLoopEndpoints(testParameters, datasetContext);

			var objects = new object[4];
			objects[0] = objParams[0];
			objects[2] = objParams[2];
			objects[3] = ignoreLoopEndpoints;

			var layers = (IFeatureClass[]) objParams[0];
			var ignoreFields = (string[]) objParams[1];

			objects[1] = GetIgnoreFieldsPerFeatureClass(layers, ignoreFields);

			return objects;
		}

		[CanBeNull]
		private object GetIgnoreLoopEndpoints(
			[NotNull] IEnumerable<TestParameter> testParameters,
			[NotNull] IOpenDataset datasetContext)
		{
			foreach (TestParameter testParameter in testParameters)
			{
				if (testParameter.Name == IgnoreLoopEndPointsParam)
				{
					object value;
					if (TryGetArgumentValue(testParameter, datasetContext, out value))
					{
						return value;
					}
				}
			}

			return null;
		}

		[NotNull]
		private static string[][] GetIgnoreFieldsPerFeatureClass(
			[NotNull] ICollection<IFeatureClass> layers,
			[NotNull] IEnumerable<string> ignoreFields)
		{
			var result = new string[layers.Count][];

			var ignore = new List<string>();
			int layerIndex = 0;
			foreach (string fieldName in ignoreFields)
			{
				if (fieldName == EndLayerFields)
				{
					var fields = new string[ignore.Count];
					int iField = 0;
					foreach (string s in ignore)
					{
						fields[iField] = s;
						iField++;
					}

					result[layerIndex] = fields;
					ignore = new List<string>();
					layerIndex++;
				}
				else
				{
					ignore.Add(fieldName);
				}
			}

			if (layerIndex != layers.Count)
			{
				throw new ArgumentException(
					string.Format(
						"Expected {0} groups of ignore fields, got {1}",
						layers.Count, layerIndex));
			}

			return result;
		}
	}
}
