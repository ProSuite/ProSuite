using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Tests;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRelUnique : QaRelationTestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
		{
			get { return QaUnique.Codes; }
		}

		public override string GetTestTypeDescription()
		{
			return typeof(QaRelUnique).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			// relationTables is redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<ITable>),
					                  DocStrings.QaRelUnique_relationTables),
					new TestParameter("relation", typeof(string),
					                  DocStrings.QaRelUnique_relation),
					new TestParameter("join", typeof(JoinType),
					                  DocStrings.QaRelUnique_join),
					new TestParameter("unique", typeof(string),
					                  DocStrings.QaRelUnique_unique),
					new TestParameter("maxRows", typeof(int),
					                  DocStrings.QaRelUnique_maxRows)
				};

			return list.AsReadOnly();
		}

		public override string GetTestDescription()
		{
			return DocStrings.QaRelUnique;
		}

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);
			if (objParams.Length != 5)
			{
				throw new ArgumentException(string.Format("expected 5 parameter, got {0}",
				                                          objParams.Length));
			}

			if (objParams[0] as IList<ITable> == null)
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

			if (objParams[3] as string == null)
			{
				throw new ArgumentException(string.Format("expected string, got {0}",
				                                          objParams[3].GetType()));
			}

			if (objParams[4] is int == false)
			{
				throw new ArgumentException(string.Format("expected int, got {0}",
				                                          objParams[4].GetType()));
			}

			var objects = new object[4];

			var tables = (IList<ITable>) objParams[0];
			var associationName = (string) objParams[1];
			var join = (JoinType) objParams[2];

			ITable queryTable = CreateQueryTable(datasetContext, associationName, tables, join);

			if (queryTable is IFeatureClass == false)
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
			var table = (ITable) args[0];
			var unique = (string) args[1];
			var maxRows = (int) args[2];

			QaUnique test = maxRows > 0
				                ? new QaUnique(table, unique, maxRows)
				                : new QaUnique(table, unique);

			test.SetRelatedTables((IList<ITable>) args[3]);
			return test;
		}
	}
}
