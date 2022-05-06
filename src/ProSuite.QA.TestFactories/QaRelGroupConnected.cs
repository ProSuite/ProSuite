using System;
using System.Collections.Generic;
using ProSuite.QA.Tests;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaRelGroupConnected : QaRelationTestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaGroupConnected.Codes;

		public override string GetTestTypeDescription()
		{
			return typeof(QaRelGroupConnected).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			// redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<IReadOnlyTable>),
					                  DocStrings.QaRelGroupConnected_relationTables),
					new TestParameter("relation", typeof(string),
					                  DocStrings.QaRelGroupConnected_relation),
					new TestParameter("join", typeof(JoinType),
					                  DocStrings.QaRelGroupConnected_join),
					new TestParameter("groupBy", typeof(IList<string>),
					                  DocStrings.QaRelGroupConnected_groupBy),
					new TestParameter("allowedShape",
					                  typeof(QaGroupConnected.ShapeAllowed),
					                  DocStrings.QaRelGroupConnected_allowedShape)
				};

			AddOptionalTestParameters(
				list, typeof(QaGroupConnected),
				additionalProperties: new[]
				                      {
					                      nameof(QaGroupConnected.ErrorReporting)
				                      });

			return list.AsReadOnly();
		}

		public override string TestDescription => DocStrings.QaRelGroupConnected;

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters,
			                               out tableParameters);
			if (objParams.Length != 5)
			{
				throw new ArgumentException(string.Format("expected 5 parameter, got {0}",
				                                          objParams.Length));
			}

			if (! (objParams[0] is IList<IReadOnlyTable>))
			{
				throw new ArgumentException(string.Format("expected IList<ITable>, got {0}",
				                                          objParams[0].GetType()));
			}

			if (objParams[1] is string == false)
			{
				throw new ArgumentException(
					string.Format("expected string (for relation), got {0}",
					              objParams[1].GetType()));
			}

			if (objParams[2].GetType() != typeof(JoinType))
			{
				throw new ArgumentException(string.Format("expected JoinType, got {0}",
				                                          objParams[2].GetType()));
			}

			if (! (objParams[3] is IList<string>))
			{
				throw new ArgumentException(string.Format("expected IList<string>, got {0}",
				                                          objParams[3].GetType()));
			}

			if (objParams[4].GetType() != typeof(QaGroupConnected.ShapeAllowed))
			{
				throw new ArgumentException(string.Format("expected ShapeAllowed, got {0}",
				                                          objParams[4].GetType()));
			}

			var objects = new object[4];

			var tables = (IList<IReadOnlyTable>) objParams[0];
			var associationName = (string) objParams[1];
			var join = (JoinType) objParams[2];

			IReadOnlyTable queryTable = CreateQueryTable(datasetContext, associationName, tables, join);

			if (queryTable is IReadOnlyFeatureClass == false)
			{
				throw new InvalidOperationException(
					"Relation table is not a FeatureClass, try change join type");
			}

			objects[0] = queryTable;
			objects[1] = objParams[3];
			objects[2] = objParams[4];
			objects[3] = tables;

			bool useCaseSensitiveQaSql;
			string tableConstraint = CombineTableParameters(tableParameters,
			                                                out useCaseSensitiveQaSql);

			tableParameters = new List<TableConstraint>
			                  {
				                  new TableConstraint(queryTable, tableConstraint,
				                                      useCaseSensitiveQaSql)
			                  };

			return objects;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			var test = new QaGroupConnected((IReadOnlyFeatureClass) args[0],
			                                (IList<string>) args[1],
			                                (QaGroupConnected.ShapeAllowed) args[2]);

			test.AddRelatedTables((IReadOnlyTable) args[0], (IList<IReadOnlyTable>) args[3]);
			return test;
		}
	}
}
