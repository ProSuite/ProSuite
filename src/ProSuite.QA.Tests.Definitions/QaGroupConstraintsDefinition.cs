using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaGroupConstraintsDefinition : AlgorithmDefinition
	{
		public IList<ITableSchemaDef> Tables { get; }
		public IList<string> GroupByExpressions { get; }
		public IList<string> DistinctExpressions { get; }
		public int MinDistinctCount { get; }
		public int MaxDistinctCount { get; }
		public bool LimitToTestedRows { get; }

		[Doc(nameof(DocStrings.QaGroupConstraints_0))]
		public QaGroupConstraintsDefinition(
			[Doc(nameof(DocStrings.QaGroupConstraints_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaGroupConstraints_groupByExpression))] [NotNull]
			string groupByExpression,
			[Doc(nameof(DocStrings.QaGroupConstraints_distinctExpression))] [NotNull]
			string distinctExpression,
			[Doc(nameof(DocStrings.QaGroupConstraints_maxDistinctCount))]
			int maxDistinctCount,
			[Doc(nameof(DocStrings.QaGroupConstraints_limitToTestedRows))]
			bool limitToTestedRows)
			: this(new[] { table }, new[] { groupByExpression }, new[] { distinctExpression },
			       0, maxDistinctCount, limitToTestedRows) { }

		[Doc(nameof(DocStrings.QaGroupConstraints_1))]
		public QaGroupConstraintsDefinition(
			[Doc(nameof(DocStrings.QaGroupConstraints_tables))] [NotNull]
			IList<ITableSchemaDef> tables,
			[Doc(nameof(DocStrings.QaGroupConstraints_groupByExpressions))] [NotNull]
			IList<string> groupByExpressions,
			[Doc(nameof(DocStrings.QaGroupConstraints_distinctExpressions))] [NotNull]
			IList<string> distinctExpressions,
			[Doc(nameof(DocStrings.QaGroupConstraints_maxDistinctCount))]
			int maxDistinctCount,
			[Doc(nameof(DocStrings.QaGroupConstraints_limitToTestedRows))]
			bool limitToTestedRows)
			: this(tables, groupByExpressions, distinctExpressions,
			       0, maxDistinctCount, limitToTestedRows) { }

		// TODO document
		[Doc(nameof(DocStrings.QaGroupConstraints_1))]
		public QaGroupConstraintsDefinition(
			[Doc(nameof(DocStrings.QaGroupConstraints_tables))] [NotNull]
			IList<ITableSchemaDef> tables,
			[Doc(nameof(DocStrings.QaGroupConstraints_groupByExpressions))] [NotNull]
			IList<string> groupByExpressions,
			[Doc(nameof(DocStrings.QaGroupConstraints_distinctExpressions))] [NotNull]
			IList<string> distinctExpressions,
			[Doc(nameof(DocStrings.QaGroupConstraints_minDistinctCount))]
			int minDistinctCount,
			[Doc(nameof(DocStrings.QaGroupConstraints_maxDistinctCount))]
			int maxDistinctCount,
			[Doc(nameof(DocStrings.QaGroupConstraints_limitToTestedRows))]
			bool limitToTestedRows)
			: base(tables)
		{
			Assert.AreEqual(tables.Count, groupByExpressions.Count,
			                "tables count and groupByExpressions count differ");
			Assert.AreEqual(tables.Count, distinctExpressions.Count,
			                "tables count and distinctExpressions count differ");
			Assert.ArgumentCondition(
				maxDistinctCount < 0 || maxDistinctCount >= minDistinctCount,
				"Maximum distinct count must be either negative (undefined) or >= the minimum distinct count ({0}): {1}",
				minDistinctCount, maxDistinctCount);

			Tables = tables;
			GroupByExpressions = groupByExpressions;
			DistinctExpressions = distinctExpressions;
			MinDistinctCount = minDistinctCount;
			MaxDistinctCount = maxDistinctCount;
			LimitToTestedRows = limitToTestedRows;
		}

		[TestParameter]
		public IList<string> ExistsRowGroupFilters { get; set; }
	}
}
