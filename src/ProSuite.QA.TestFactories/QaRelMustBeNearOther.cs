using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Tests;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	public class QaRelMustBeNearOther : QaRelationTestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaMustBeNearOther.Codes;

		public override string GetTestTypeDescription()
		{
			return typeof(QaRelLineGroupConstraints).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			// redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<ITable>),
					                  DocStrings.QaRelGroupConnected_relationTables),
					new TestParameter("relation", typeof(string),
					                  DocStrings.QaRelGroupConnected_relation),
					new TestParameter("join", typeof(JoinType),
					                  DocStrings.QaRelGroupConnected_join),
				};
			AddConstructorParameters(list, typeof(QaMustBeNearOther),
			                         constructorIndex: 0,
			                         ignoreParameters: new[] {0});

			AddOptionalTestParameters(
				list, typeof(QaMustBeNearOther));

			return list.AsReadOnly();
		}

		public override string GetTestDescription()
		{
			return DocStrings.QaRelConstraint;
		}

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			List<TableConstraint> ddxTableParameters;
			object[] objParams = base.Args(datasetContext, testParameters, out ddxTableParameters);
			if (objParams.Length != 6)
			{
				throw new ArgumentException(string.Format("expected 6 parameter, got {0}",
				                                          objParams.Length));
			}

			var tables = ValidateType<IList<ITable>>(objParams[0], "IList<ITable>");
			var associationName =
				ValidateType<string>(objParams[1], "string (for relation)");
			var join = ValidateType<JoinType>(objParams[2]);

			var nearClasses = ValidateType<IList<IFeatureClass>>(objParams[3]);
			var maximumDistance = ValidateType<double>(objParams[4]);
			var relevantRelationCondition = ValidateType<string>(objParams[5]);

			ITable queryTable = CreateQueryTable(datasetContext, associationName, tables, join);

			var objects = new object[5];

			objects[0] = queryTable;
			objects[1] = nearClasses;
			objects[2] = maximumDistance;
			objects[3] = relevantRelationCondition;
			objects[4] = tables;

			bool useCaseSensitiveQaSql;
			string joinConstraint = CombineTableParameters(ddxTableParameters.GetRange(0, 2),
			                                               out useCaseSensitiveQaSql);

			tableParameters = new List<TableConstraint>
			                  {
				                  new TableConstraint(queryTable, joinConstraint,
				                                      useCaseSensitiveQaSql)
			                  };
			tableParameters.AddRange(ddxTableParameters.GetRange(2, ddxTableParameters.Count - 2));

			return objects;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			var test = new QaMustBeNearOther((IFeatureClass) args[0],
			                                 (ICollection<IFeatureClass>) args[1],
			                                 (double) args[2],
			                                 (string) args[3]);
			test.AddRelatedTables((ITable) args[0], (IList<ITable>) args[4]);
			return test;
		}
	}
}
