using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[SchemaTest]
	public class QaGdbReleaseDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		// public string ExpectedVersion { get; }
		public string MinimumVersion { get; }
		public string MaximumVersion { get; }

		[UsedImplicitly]
		[Doc(nameof(DocStrings.QaGdbRelease_0))]
		public QaGdbReleaseDefinition(
			[Doc(nameof(DocStrings.QaGdbRelease_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaGdbRelease_expectedVersion))] [CanBeNull]
			string expectedVersion)
			: this(table, expectedVersion, expectedVersion) { }

		[UsedImplicitly]
		[Doc(nameof(DocStrings.QaGdbRelease_1))]
		public QaGdbReleaseDefinition(
			[Doc(nameof(DocStrings.QaGdbRelease_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaGdbRelease_minimumVersion))] [CanBeNull]
			string minimumVersion,
			[Doc(nameof(DocStrings.QaGdbRelease_maximumVersion))] [CanBeNull]
			string maximumVersion)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Table = table;
			//ExpectedVersion = expectedVersion;
			MinimumVersion = minimumVersion;
			MaximumVersion = maximumVersion;
		}
	}
}
