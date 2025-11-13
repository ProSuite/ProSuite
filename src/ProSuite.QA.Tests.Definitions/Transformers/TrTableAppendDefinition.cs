using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrTableAppendDefinition : AlgorithmDefinition
	{
		public IList<ITableSchemaDef> Tables { get; }

		private AppendedTable _transformedTable;

		[DocTr(nameof(DocTrStrings.TrTableAppend_0))]
		public TrTableAppendDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableAppend_tables))]
			IList<ITableSchemaDef> tables)
			: base(tables) { }

		public ITableSchemaDef GetTransformed()
		{
			return (ITableSchemaDef) (_transformedTable ??
			                          (_transformedTable = InitTransformedTable()));
		}

		private AppendedTable InitTransformedTable()
		{
			return AppendedTable.Create(Tables, "transformed");
		}

		private class AppendedTable
		{
			public IList<ITableSchemaDef> Tables { get; }
			public string Name { get; }

			private AppendedTable(IList<ITableSchemaDef> tables, string name)
			{
				Tables = tables;
				Name = name ?? "appended"; // Fallback to "appended" if no name is provided
			}

			public static AppendedTable Create(IList<ITableSchemaDef> tables, string name = null)
			{
				if (tables == null || tables.Count == 0)
				{
					throw new ArgumentException("Tables cannot be null or empty.", nameof(tables));
				}

				return new AppendedTable(tables, name);
			}
		}
	}
}
