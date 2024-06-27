using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	public class QaExportTablesDefinition : AlgorithmDefinition
	{
		public IList<ITableSchemaDef> Tables { get; }
		public string FileGdbPath { get; }

		[Doc(nameof(DocStrings.QaExportTables_0))]
		public QaExportTablesDefinition(
			[Doc(nameof(DocStrings.QaExportTables_tables))] [NotNull]
			IList<ITableSchemaDef> tables,
			[Doc(nameof(DocStrings.QaExportTables_fileGdbPath))] [NotNull]
			string fileGdbPath)
			: base(tables)
		{
			Tables = tables;
			FileGdbPath = fileGdbPath;
		}

		[Doc(nameof(DocStrings.QaExportTables_ExportTileIds))]
		[TestParameter]
		public bool ExportTileIds { get; set; }

		[Doc(nameof(DocStrings.QaExportTables_ExportTiles))]
		[TestParameter]
		public bool ExportTiles { get; set; }
	}
}
