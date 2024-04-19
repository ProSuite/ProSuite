using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[AttributeTest]
	[UsedImplicitly]
	public class QaValidUrlsDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }

		public string UrlExpression { get; }

		private const int _defaultMaximumParallelTasks = 1;

		[Doc(nameof(DocStrings.QaValidUrls_0))]
		public QaValidUrlsDefinition(
			[Doc(nameof(DocStrings.QaValidUrls_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValidUrls_urlExpression))] [NotNull]
			string urlExpression)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(urlExpression, nameof(urlExpression));

			Table = table;
			UrlExpression = urlExpression;
		}

		[TestParameter(_defaultMaximumParallelTasks)]
		[Doc(nameof(DocStrings.QaValidUrls_MaximumParallelTasks))]
		public int MaximumParallelTasks { get; set; } = _defaultMaximumParallelTasks;
	}
}
