using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests.Schema
{
	public class FieldSpecifications
	{
		private readonly ITable _table;
		private readonly bool _matchAliasName;
		private readonly IFieldSpecificationsIssueCodes _issueCodes;

		private readonly List<FieldSpecification> _fieldSpecifications;

		private readonly Dictionary<string, FieldSpecification>
			_fieldSpecificationsByAliasName
				= new Dictionary<string, FieldSpecification>(
					StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldSpecifications"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="fieldSpecifications">The field specifications.</param>
		/// <param name="matchAliasName">if set to <c>true</c> [match alias name].</param>
		/// <param name="issueCodes">The issue codes.</param>
		public FieldSpecifications(
			[NotNull] ITable table,
			[NotNull] IEnumerable<FieldSpecification> fieldSpecifications,
			bool matchAliasName,
			[NotNull] IFieldSpecificationsIssueCodes issueCodes)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(fieldSpecifications, nameof(fieldSpecifications));
			Assert.ArgumentNotNull(issueCodes, nameof(issueCodes));

			_fieldSpecifications = fieldSpecifications.ToList();

			AssertUniqueFieldNames(_fieldSpecifications);

			_table = table;
			_matchAliasName = matchAliasName;
			_issueCodes = issueCodes;

			if (matchAliasName)
			{
				foreach (FieldSpecification fieldSpecification in _fieldSpecifications)
				{
					string aliasName = fieldSpecification.ExpectedAliasName;

					if (string.IsNullOrEmpty(aliasName))
					{
						continue;
					}

					if (_fieldSpecificationsByAliasName.TryGetValue(
						    aliasName, out FieldSpecification assignedFieldSpecification))
					{
						Assert.Fail("Duplicate alias names in field specifications: " +
						            "{0} (used in specifications for {1}, {2}), " +
						            "unable to uniquely match field to specification by alias name",
						            aliasName,
						            assignedFieldSpecification.FieldName,
						            fieldSpecification.FieldName);
					}

					_fieldSpecificationsByAliasName.Add(aliasName, fieldSpecification);
				}
			}
		}

		public int Verify([NotNull] Func<string, string, IssueCode, int> reportSchemaError)
		{
			int errorCount = 0;

			foreach (FieldSpecification fieldSpecification in _fieldSpecifications)
			{
				string fieldName = fieldSpecification.FieldName;

				int fieldIndex = _table.FindField(fieldName);

				if (fieldIndex < 0)
				{
					if (! fieldSpecification.FieldIsOptional)
					{
						IssueCode issueCode = _issueCodes.MissingField;
						errorCount += reportSchemaError(
							fieldName,
							string.Format("Required field '{0}' does not exist", fieldName),
							issueCode);
					}

					continue;
				}

				IField field = _table.Fields.Field[fieldIndex];

				foreach (KeyValuePair<string, IssueCode> pair in
				         fieldSpecification.GetIssues(field, _issueCodes))
				{
					errorCount += reportSchemaError(fieldName, pair.Key, pair.Value);
				}
			}

			if (_matchAliasName)
			{
				foreach (IField field in DatasetUtils.GetFields(_table))
				{
					string aliasName = field.AliasName;
					string fieldName = field.Name;

					if (string.IsNullOrEmpty(aliasName))
					{
						continue;
					}

					if (! _fieldSpecificationsByAliasName.TryGetValue(
						    aliasName.Trim(), out FieldSpecification fieldSpecification))
					{
						continue;
					}

					if (string.Equals(fieldName, fieldSpecification.FieldName))
					{
						continue;
					}

					IssueCode issueCode = _issueCodes.UnexpectedFieldNameForAlias;
					errorCount += reportSchemaError(
						fieldName,
						string.Format(
							"Field '{0}' has same alias ('{1}') as specification for field '{2}'. " +
							"Field name should also be equal",
							fieldName, aliasName, fieldSpecification.FieldName),
						issueCode);
				}
			}

			return errorCount;
		}

		private static void AssertUniqueFieldNames(
			[NotNull] IEnumerable<FieldSpecification> fieldSpecifications)
		{
			var fieldNames = new SimpleSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (FieldSpecification fieldSpecification in fieldSpecifications)
			{
				if (fieldNames.Contains(fieldSpecification.FieldName))
				{
					Assert.Fail(
						"There exists more than one field specification for field name '{0}'",
						fieldSpecification.FieldName);
				}

				fieldNames.Add(fieldSpecification.FieldName);
			}
		}
	}
}
