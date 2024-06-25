using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	public class QaRowCountDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public IList<ITableSchemaDef> ReferenceTables { get; }
		public int MinimumRowCount { get; }
		public int MaximumRowCount { get; }
		public OffsetSpecification MinimumValueOffset { get; }
		public OffsetSpecification MaximumValueOffset { get; }

		[Doc(nameof(DocStrings.QaRowCount_0))]
		public QaRowCountDefinition(
			[Doc(nameof(DocStrings.QaRowCount_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaRowCount_minimumRowCount))]
			int minimumRowCount,
			[Doc(nameof(DocStrings.QaRowCount_maximumRowCount))]
			int maximumRowCount)
			: base(new[] { table })
		{
			Assert.ArgumentNotNull(table, nameof(table));

			Table = table;
			MinimumRowCount = minimumRowCount;
			MaximumRowCount = maximumRowCount;
		}

		[Doc(nameof(DocStrings.QaRowCount_1))]
		public QaRowCountDefinition(
			[Doc(nameof(DocStrings.QaRowCount_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaRowCount_referenceTables))] [NotNull]
			IList<ITableSchemaDef> referenceTables,
			[Doc(nameof(DocStrings.QaRowCount_minimumValueOffset))] [CanBeNull]
			string minimumValueOffset,
			[Doc(nameof(DocStrings.QaRowCount_maximumValueOffset))] [CanBeNull]
			string maximumValueOffset)
			: base((new[] { table }.Union(referenceTables)))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(referenceTables, nameof(referenceTables));

			Table = table;
			ReferenceTables = referenceTables;

			CultureInfo formatProvider = CultureInfo.InvariantCulture;

			if (minimumValueOffset != null)
			{
				MinimumValueOffset = OffsetSpecification.Parse(minimumValueOffset, formatProvider);
			}

			if (maximumValueOffset != null)
			{
				MaximumValueOffset = OffsetSpecification.Parse(maximumValueOffset, formatProvider);
			}

			Assert.ArgumentCondition(
				minimumValueOffset != null || maximumValueOffset != null,
				"At least one offset must be specified");
		}
	}
}
