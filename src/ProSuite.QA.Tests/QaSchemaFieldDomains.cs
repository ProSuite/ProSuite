using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldDomains : QaSchemaTestBase
	{
		[NotNull] private readonly IReadOnlyTable _table;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string FieldTypeMismatch = "FieldTypeMismatch";

			public Code() : base("FieldDomains") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSchemaFieldDomains_0))]
		public QaSchemaFieldDomains(
			[Doc(nameof(DocStrings.QaSchemaFieldDomains_table))] [NotNull]
			IReadOnlyTable table)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
		}

		[InternallyUsedTest]
		public QaSchemaFieldDomains([NotNull] QaSchemaFieldDomainsDefinition definition)
			: this((IReadOnlyTable) definition.Table) { }

		public override int Execute()
		{
			var errorCount = 0;

			foreach (DomainUsage domainUsage in SchemaTestUtils.GetDomainUsages(_table))
			{
				foreach (IField referencingField in domainUsage.ReferencingFields)
				{
					errorCount += CheckDomainUsage(domainUsage.Domain, referencingField);
				}
			}

			return errorCount;
		}

		private int CheckDomainUsage([NotNull] IDomain domain,
		                             [NotNull] IField referencingField)
		{
			if (domain.FieldType == referencingField.Type)
			{
				return NoError;
			}

			return ReportSchemaPropertyError(
				Codes[Code.FieldTypeMismatch], domain.Name,
				new object[] {referencingField.Name},
				"Domain '{0}' is used for field '{1}' with type {2}, but the domain field type is {3}",
				domain.Name, referencingField.Name,
				FieldUtils.GetFieldTypeDisplayText(referencingField.Type),
				FieldUtils.GetFieldTypeDisplayText(domain.FieldType));
		}
	}
}
