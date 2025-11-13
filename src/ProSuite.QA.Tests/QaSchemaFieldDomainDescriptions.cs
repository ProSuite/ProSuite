using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldDomainDescriptions : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly int _maximumLength;
		private readonly bool _requireUniqueDescriptions;
		private readonly IReadOnlyTable _targetWorkspaceTable;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string TextLength_TooLong = "TextLength.TooLong";
			public const string NotUnique = "NotUnique";

			public Code() : base("DomainDescriptions") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_0))]
		public QaSchemaFieldDomainDescriptions(
			[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_requireUniqueDescriptions))]
			bool requireUniqueDescriptions,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_targetWorkspaceTable))]
			[CanBeNull] IReadOnlyTable targetWorkspaceTable)
			: base(table,targetWorkspaceTable)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
			_maximumLength = maximumLength;
			_requireUniqueDescriptions = requireUniqueDescriptions;
			_targetWorkspaceTable = targetWorkspaceTable;
		}

		[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_0))]
		public QaSchemaFieldDomainDescriptions(
				[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_maximumLength))]
				int maximumLength,
				[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_requireUniqueDescriptions))]
				bool requireUniqueDescriptions)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, maximumLength, requireUniqueDescriptions, null) { }

		[InternallyUsedTest]
		public QaSchemaFieldDomainDescriptions(
			[NotNull] QaSchemaFieldDomainDescriptionsDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.MaximumLength,
			       definition.RequireUniqueDescriptions,
			       (IReadOnlyTable) definition.TargetWorkspaceTable) { }

		public override int Execute()
		{
			int errorCount = 0;

			IList<DomainUsage> domainUsages = SchemaTestUtils.GetDomainUsages(_table);

			foreach (DomainUsage domainUsage in domainUsages)
			{
				IDomain domain = domainUsage.Domain;

				string message;
				TextLengthIssue? lengthIssue = SchemaTestUtils.HasValidLength(
					domain.Description, _maximumLength, "description", out message);
				if (lengthIssue != null)
				{
					errorCount += ReportSchemaPropertyError(
						GetIssueCode(lengthIssue.Value), domain.Name,
						LocalizableStrings
							.QaSchemaFieldDomainDescriptions_DomainDescriptionInvalidLength,
						domain.Name, message, domain.Description);
				}
			}

			if (_requireUniqueDescriptions)
			{
				errorCount += CheckDuplicateDescriptions(domainUsages);
			}

			return errorCount;
		}

		private int CheckDuplicateDescriptions(
			[NotNull] IEnumerable<DomainUsage> domainUsages)
		{
			Assert.ArgumentNotNull(domainUsages, nameof(domainUsages));

			IWorkspace targetWorkspace = _targetWorkspaceTable?.Workspace ?? _table.Workspace;

			// TODO also search for duplicates in the source workspace? Or would this be a separate test instance?

			Dictionary<string, List<IDomain>> domainsByDescription =
				GetDomainsByDescription(targetWorkspace);

			var descriptions = new SimpleSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (DomainUsage domainUsage in domainUsages)
			{
				string description = domainUsage.Domain.Description;

				if (description != null && ! descriptions.Contains(description))
				{
					descriptions.Add(description);
				}
			}

			int errorCount = 0;

			foreach (string domainDescription in descriptions)
			{
				List<IDomain> domainsWithSameDescription;
				if (! domainsByDescription.TryGetValue(domainDescription,
				                                       out domainsWithSameDescription))
				{
					continue;
				}

				if (domainsWithSameDescription.Count <= 1)
				{
					continue;
				}

				string description =
					string.Format(
						LocalizableStrings
							.QaSchemaFieldDomainDescriptions_DomainDescriptionNotUnique,
						domainDescription,
						StringUtils.Concatenate(GetDomainNames(domainsWithSameDescription), ", "));

				errorCount += ReportSchemaError(Codes[Code.NotUnique], description);
			}

			return errorCount;
		}

		[NotNull]
		private static IEnumerable<string> GetDomainNames(
			[NotNull] IEnumerable<IDomain> domains)
		{
			return domains.Select(domain => domain.Name);
		}

		[NotNull]
		private static Dictionary<string, List<IDomain>> GetDomainsByDescription(
			[NotNull] IWorkspace targetWorkspace)
		{
			Assert.ArgumentNotNull(targetWorkspace, nameof(targetWorkspace));

			var result = new Dictionary<string, List<IDomain>>(
				StringComparer.CurrentCultureIgnoreCase);

			foreach (IDomain existingDomain in DomainUtils.GetDomains(targetWorkspace))
			{
				string description = existingDomain.Description;

				List<IDomain> domains;
				if (! result.TryGetValue(description, out domains))
				{
					domains = new List<IDomain>();
					result.Add(description, domains);
				}

				domains.Add(existingDomain);
			}

			return result;
		}

		[CanBeNull]
		private static IssueCode GetIssueCode(TextLengthIssue lengthIssue)
		{
			switch (lengthIssue)
			{
				case TextLengthIssue.LessThanMinimum:
					throw new ArgumentOutOfRangeException(nameof(lengthIssue), lengthIssue,
					                                      @"Unexpected length issue");

				case TextLengthIssue.GreaterThanMaximum:
					return Codes[Code.TextLength_TooLong];

				default:
					throw new ArgumentOutOfRangeException(nameof(lengthIssue), lengthIssue,
					                                      @"Unknown length issue");
			}
		}
	}
}
