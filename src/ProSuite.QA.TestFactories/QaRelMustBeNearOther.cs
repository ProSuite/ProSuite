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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaRelMustBeNearOther : QaRelationTestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaMustBeNearOther.Codes;

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

			List<IReadOnlyTable> tables = ToReadOnlyTableList<IReadOnlyTable>(objParams[0]);
			var associationName =
				ValidateType<string>(objParams[1], "string (for relation)");
			var join = ValidateType<JoinType>(objParams[2]);

			IList<IReadOnlyFeatureClass> nearClasses =
				ToReadOnlyTableList<IReadOnlyFeatureClass>(objParams[3]);
			//ValidateType<IList<IReadOnlyFeatureClass>>(objParams[3]);

			var maximumDistance = ValidateType<double>(objParams[4]);

			// TOP-5291: relevantRelationCondition is nullable in the test
			string relevantRelationCondition = null;
			object relevantRelParam = objParams[5];
			if (relevantRelParam != null)
			{
				relevantRelationCondition = ValidateType<string>(relevantRelParam);
			}

			IReadOnlyTable queryTable =
				CreateQueryTable(datasetContext, associationName, tables, join);

			var objects = new object[5];

			objects[0] = queryTable;
			objects[1] = nearClasses;
			objects[2] = maximumDistance;
			objects[3] = relevantRelationCondition;
			objects[4] = tables;

			IDictionary<string, string> replacements = GetTableNameReplacements(
				Assert.NotNull(Condition).ParameterValues.OfType<DatasetTestParameterValue>(),
				datasetContext);
			bool useCaseSensitiveQaSql;
			string joinConstraint = CombineTableParameters(
				ddxTableParameters.GetRange(0, 2), replacements, out useCaseSensitiveQaSql);

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
			var test = new QaMustBeNearOther((IReadOnlyFeatureClass) args[0],
			                                 (ICollection<IReadOnlyFeatureClass>) args[1],
			                                 (double) args[2],
			                                 (string) args[3]);
			return test;
		}
	}
}
