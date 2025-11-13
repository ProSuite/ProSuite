using System.Collections.Generic;
using System.ComponentModel;
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
	public class QaUniqueDefinition : AlgorithmDefinition
	{
		public IList<ITableSchemaDef> Tables { get; }
		public IList<string> Uniques { get; }

		public int MaxRows { get; } = _defaultMaxRows;

		private const int _defaultMaxRows = short.MaxValue;

		[Doc(nameof(DocStrings.QaUnique_0))]
		public QaUniqueDefinition(
				[Doc(nameof(DocStrings.QaUnique_table))]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaUnique_unique))]
				string unique)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, unique, _defaultMaxRows) { }

		[Doc(nameof(DocStrings.QaUnique_0))]
		public QaUniqueDefinition(
			[Doc(nameof(DocStrings.QaUnique_table))]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaUnique_unique))]
			string unique,
			[Doc(nameof(DocStrings.QaUnique_maxRows))] [DefaultValue(_defaultMaxRows)]
			int maxRows)
			: this(new[] { table }, new[] { unique })
		{
			MaxRows = maxRows;
		}

		[Doc(nameof(DocStrings.QaUnique_1))]
		public QaUniqueDefinition(
			[Doc(nameof(DocStrings.QaUnique_tables))]
			IList<ITableSchemaDef> tables,
			[Doc(nameof(DocStrings.QaUnique_uniques))]
			IList<string> uniques)
			: base(tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));
			Assert.ArgumentNotNull(uniques, nameof(uniques));

			Assert.ArgumentCondition(uniques.Count == 1 || uniques.Count == tables.Count,
			                         "uniques must contain 1 value, or 1 value per table");

			Tables = tables;
			Uniques = uniques;
		}
	}
}
