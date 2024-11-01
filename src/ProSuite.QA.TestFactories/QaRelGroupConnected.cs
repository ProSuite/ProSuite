using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaRelGroupConnected : QaRelationTestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaGroupConnected.Codes;
		
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

			if (objParams[4].GetType() != typeof(ShapeAllowed))
			{
				throw new ArgumentException(string.Format("expected ShapeAllowed, got {0}",
				                                          objParams[4].GetType()));
			}

			var objects = new object[4];

			var tables = (IList<IReadOnlyTable>) objParams[0];
			var associationName = (string) objParams[1];
			var join = (JoinType) objParams[2];

			IReadOnlyTable queryTable =
				CreateQueryTable(datasetContext, associationName, tables, join);

			if (queryTable is IReadOnlyFeatureClass == false)
			{
				throw new InvalidOperationException(
					"Relation table is not a FeatureClass, try change join type");
			}

			objects[0] = queryTable;
			objects[1] = objParams[3];
			objects[2] = objParams[4];
			objects[3] = tables;

			IDictionary<string, string> replacements = GetTableNameReplacements(
				Assert.NotNull(Condition).ParameterValues.OfType<DatasetTestParameterValue>(),
				datasetContext);

			bool useCaseSensitiveQaSql;
			string tableConstraint = CombineTableParameters(tableParameters, replacements,
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
			                                (ShapeAllowed) args[2]);
			return test;
		}
	}
}
