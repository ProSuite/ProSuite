using System.Collections.Generic;
using System.Linq;
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
	public class QaForeignKeyDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public IList<string> ForeignKeyFields { get; }
		public ITableSchemaDef ReferencedTable { get; }
		public IList<string> ReferencedKeyFields { get; }
		public bool ReferenceIsError { get; }

		[Doc(nameof(DocStrings.QaForeignKey_0))]
		public QaForeignKeyDefinition(
			[Doc(nameof(DocStrings.QaForeignKey_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaForeignKey_foreignKeyField))] [NotNull]
			string foreignKeyField,
			[Doc(nameof(DocStrings.QaForeignKey_referencedTable))] [NotNull]
			ITableSchemaDef referencedTable,
			[Doc(nameof(DocStrings.QaForeignKey_referencedKeyField))] [NotNull]
			string referencedKeyField)
			: this(table, new[] { foreignKeyField }, referencedTable,
			       new[] { referencedKeyField }) { }

		[Doc(nameof(DocStrings.QaForeignKey_1))]
		public QaForeignKeyDefinition(
				[Doc(nameof(DocStrings.QaForeignKey_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaForeignKey_foreignKeyFields))] [NotNull]
				IEnumerable<string> foreignKeyFields,
				[Doc(nameof(DocStrings.QaForeignKey_referencedTable))] [NotNull]
				ITableSchemaDef referencedTable,
				[Doc(nameof(DocStrings.QaForeignKey_referencedKeyFields))] [NotNull]
				IEnumerable<string> referencedKeyFields)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, foreignKeyFields, referencedTable, referencedKeyFields, false) { }

		[Doc(nameof(DocStrings.QaForeignKey_2))]
		public QaForeignKeyDefinition(
			[Doc(nameof(DocStrings.QaForeignKey_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaForeignKey_foreignKeyFields))] [NotNull]
			IEnumerable<string> foreignKeyFields,
			[Doc(nameof(DocStrings.QaForeignKey_referencedTable))] [NotNull]
			ITableSchemaDef referencedTable,
			[Doc(nameof(DocStrings.QaForeignKey_referencedKeyFields))] [NotNull]
			IEnumerable<string> referencedKeyFields,
			[Doc(nameof(DocStrings.QaForeignKey_referenceIsError))]
			bool referenceIsError)
			: base(new[] { table, referencedTable })
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(foreignKeyFields, nameof(foreignKeyFields));
			Assert.ArgumentNotNull(referencedTable, nameof(referencedTable));
			Assert.ArgumentNotNull(referencedKeyFields, nameof(referencedKeyFields));

			Table = table;
			ForeignKeyFields = foreignKeyFields.ToList();
			ReferencedKeyFields = referencedKeyFields.ToList();
			ReferencedTable = referencedTable;
			ReferenceIsError = referenceIsError;

			// Validation:
			Assert.ArgumentCondition(ForeignKeyFields.Count > 0,
			                         "There must be at least one foreign key field");
			Assert.ArgumentCondition(ForeignKeyFields.Count == ReferencedKeyFields.Count,
			                         "The number of foreign key fields must be equal to " +
			                         "the number of referenced key fields");
		}
	}
}
