using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaDangleFactory : TestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
		{
			get { return QaConnections.Codes; }
		}

		public override string GetTestTypeDescription()
		{
			return typeof(QaConnections).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			var list = new List<TestParameter>
			           {
				           new TestParameter("polylineClasses", typeof(IFeatureClass[]),
				                             DocStrings.QaDangleFactory_polylineClasses)
			           };

			return list.AsReadOnly();
		}

		public override string GetTestDescription()
		{
			return DocStrings.QaDangleFactory;
		}

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

			if (objParams[0] is IFeatureClass[] == false)
			{
				throw new ArgumentException(string.Format("expected IFeatureClass[], got {0}",
				                                          objParams[0].GetType()));
			}

			var objects = new object[2];
			objects[0] = objParams[0];

			var featureClasses = (IFeatureClass[]) objParams[0];

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
			var featureClasses = (IFeatureClass[]) args[0];
			var rules = (IList<string[]>) args[1];

			return new QaConnections(featureClasses, rules);
		}
	}
}