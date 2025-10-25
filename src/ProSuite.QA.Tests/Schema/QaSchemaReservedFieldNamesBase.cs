using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests.Schema
{
	public abstract class QaSchemaReservedFieldNamesBase : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly IReadOnlyTable _reservedNamesTable;
		private readonly string _reservedNameFieldName;
		private readonly string _reservedReasonFieldName;
		private readonly string _validNameFieldName;
		private List<ReservedName> _reservedNames;
		private IDictionary<string, FieldSpecification> _fieldSpecificationsByName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string FieldNameIsReserved = "FieldNameIsReserved";

			public Code() : base("ReservedFieldNames") { }
		}

		#endregion

		protected QaSchemaReservedFieldNamesBase(
			[NotNull] QaSchemaReservedFieldNamesDefinition definition)
			: base((IReadOnlyTable) definition.Table,
			       new[] { (IReadOnlyTable) definition.ReservedNamesTable })
		{
			_table = (IReadOnlyTable) definition.Table;

			if (definition.ReservedNames != null)
			{
				_reservedNames = ToReservedNames(definition.ReservedNames);
			}
			else
			{
				Assert.ArgumentNotNull(definition.ReservedNamesTable,
				                       nameof(definition.ReservedNamesTable));
				Assert.ArgumentNotNull(definition.ReservedNameFieldName,
				                       nameof(definition.ReservedNameFieldName));
				Assert.ArgumentNotNullOrEmpty(definition.ReservedReasonFieldName,
				                              nameof(definition.ReservedReasonFieldName));

				_reservedNamesTable = (IReadOnlyTable) definition.ReservedNamesTable;
				_reservedNameFieldName = definition.ReservedNameFieldName;
				_reservedReasonFieldName = definition.ReservedReasonFieldName;
				_validNameFieldName = definition.ValidNameFieldName;
			}
		}

		protected QaSchemaReservedFieldNamesBase([NotNull] IReadOnlyTable table,
		                                         [NotNull] IEnumerable<string> reservedNames)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(reservedNames, nameof(reservedNames));

			_table = table;

			_reservedNames = ToReservedNames(reservedNames);
		}

		protected QaSchemaReservedFieldNamesBase([NotNull] IReadOnlyTable table,
		                                         [NotNull] string reservedNamesString)
			: this(table, TestUtils.GetTokens(reservedNamesString)) { }

		protected QaSchemaReservedFieldNamesBase([NotNull] IReadOnlyTable table,
		                                         [NotNull] IReadOnlyTable reservedNamesTable,
		                                         [NotNull] string reservedNameFieldName,
		                                         [CanBeNull] string reservedReasonFieldName,
		                                         [CanBeNull] string validNameFieldName,
		                                         [CanBeNull] IReadOnlyTable referenceTable = null)
			: base(table, new[] { reservedNamesTable, referenceTable })
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(reservedNamesTable, nameof(reservedNamesTable));
			Assert.ArgumentNotNullOrEmpty(reservedNameFieldName,
			                              nameof(reservedNameFieldName));

			_table = table;

			_reservedNamesTable = reservedNamesTable;
			_reservedNameFieldName = reservedNameFieldName;
			_reservedReasonFieldName = reservedReasonFieldName;
			_validNameFieldName = validNameFieldName;
		}

		public override int Execute()
		{
			EnsureReservedNames();

			return DatasetUtils.GetFields(_table.Fields).Sum(field => CheckField(field));
		}

		[NotNull]
		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual IEnumerable<FieldSpecification> GetFieldSpecifications()
		{
			yield break;
		}

		/// <summary>
		/// Ensures that the list of reserved names is ready.
		/// </summary>
		private void EnsureReservedNames()
		{
			if (_reservedNames != null)
			{
				return;
			}

			int nameFieldIndex;
			int reasonFieldIndex;
			int validNameFieldIndex;
			string subFields = GetLookupSubFields(out nameFieldIndex,
			                                      out reasonFieldIndex,
			                                      out validNameFieldIndex);

			var queryFilter = new AoTableFilter
			                  {
				                  SubFields = subFields,
				                  WhereClause = GetConstraint(_reservedNamesTable)
			                  };

			_reservedNames = new List<ReservedName>();

			const bool recycle = true;
			foreach (
				IReadOnlyRow row in _reservedNamesTable.EnumRows(queryFilter, recycle))
			{
				var name = row.get_Value(nameFieldIndex) as string;

				if (string.IsNullOrEmpty(name))
				{
					continue;
				}

				string reason = reasonFieldIndex >= 0
					                ? row.get_Value(reasonFieldIndex) as string
					                : null;
				string validName = validNameFieldIndex >= 0
					                   ? row.get_Value(validNameFieldIndex) as string
					                   : null;

				_reservedNames.Add(new ReservedName(name, reason, validName));
			}
		}

		[NotNull]
		private string GetLookupSubFields(out int nameFieldIndex,
		                                  out int reasonFieldIndex,
		                                  out int validNameFieldIndex)
		{
			var subFieldsBuilder = new StringBuilder();

			Assert.NotNull(_reservedNamesTable, "both list and lookup table are null");
			Assert.NotNullOrEmpty(_reservedNameFieldName,
			                      "both list and lookup field name are null/undefined");

			nameFieldIndex = _reservedNamesTable.FindField(_reservedNameFieldName);
			AssertLookupFieldExists(nameFieldIndex, _reservedNameFieldName);

			subFieldsBuilder.Append(_reservedNameFieldName);

			reasonFieldIndex = -1;
			validNameFieldIndex = -1;

			if (StringUtils.IsNotEmpty(_reservedReasonFieldName))
			{
				reasonFieldIndex =
					_reservedNamesTable.FindField(_reservedReasonFieldName);
				AssertLookupFieldExists(reasonFieldIndex, _reservedReasonFieldName);

				subFieldsBuilder.AppendFormat(",{0}", _reservedReasonFieldName);
			}

			if (StringUtils.IsNotEmpty(_validNameFieldName))
			{
				validNameFieldIndex = _reservedNamesTable.FindField(_validNameFieldName);
				AssertLookupFieldExists(validNameFieldIndex, _validNameFieldName);

				subFieldsBuilder.AppendFormat(",{0}", _validNameFieldName);
			}

			return subFieldsBuilder.ToString();
		}

		private static void AssertLookupFieldExists(int fieldIndex,
		                                            [NotNull] string fieldName)
		{
			Assert.True(fieldIndex >= 0,
			            "Field not found in table of reserved names: {0}",
			            fieldName);
		}

		private int CheckField([NotNull] IField field)
		{
			Assert.ArgumentNotNull(field, nameof(field));

			string fieldName = field.Name;

			foreach (ReservedName reservedName in _reservedNames)
			{
				if (reservedName == null)
				{
					continue;
				}

				if (reservedName.Matches(fieldName))
				{
					IssueCode issueCode;
					string description = GetDescription(field, reservedName,
					                                    GetValidNameFieldSpecification(
						                                    reservedName),
					                                    out issueCode);

					return ReportSchemaPropertyError(issueCode, fieldName, description);
				}
			}

			return 0;
		}

		[CanBeNull]
		private FieldSpecification GetValidNameFieldSpecification(
			[NotNull] ReservedName reservedName)
		{
			if (reservedName.CorrectName == null)
			{
				return null;
			}

			if (_fieldSpecificationsByName == null)
			{
				_fieldSpecificationsByName = GetFieldSpecifications().ToDictionary(
					fieldSpecification => fieldSpecification.FieldName,
					StringComparer.OrdinalIgnoreCase);
			}

			FieldSpecification result;
			return _fieldSpecificationsByName.TryGetValue(reservedName.CorrectName,
			                                              out result)
				       ? result
				       : null;
		}

		[NotNull]
		private static string GetDescription(
			[NotNull] IField field,
			[NotNull] ReservedName reservedName,
			[CanBeNull] FieldSpecification validNameFieldSpecification,
			out IssueCode issueCode)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("Field name '{0}' corresponds to a reserved name",
			                field.Name);

			if (StringUtils.IsNotEmpty(reservedName.Reason))
			{
				sb.AppendFormat(". Reason: {0}", reservedName.Reason);
			}

			if (StringUtils.IsNotEmpty(reservedName.CorrectName))
			{
				sb.AppendFormat(". Valid name: {0}", reservedName.CorrectName);
			}

			if (validNameFieldSpecification != null)
			{
				foreach (
					KeyValuePair<string, IssueCode> pair in
					validNameFieldSpecification.GetIssues(field, null))
				{
					sb.AppendFormat(". {0}", pair.Key);
				}
			}

			issueCode = Codes[Code.FieldNameIsReserved];
			return sb.ToString();
		}

		private static List<ReservedName> ToReservedNames(IEnumerable<string> reservedNames)
		{
			var result = new List<ReservedName>();

			foreach (string reservedName in reservedNames)
			{
				result.Add(new ReservedName(reservedName));
			}

			return result;
		}

		private class ReservedName
		{
			public ReservedName([NotNull] string name) : this(name, null, null) { }

			public ReservedName([NotNull] string name,
			                    [CanBeNull] string reason,
			                    [CanBeNull] string correctName)
			{
				Assert.ArgumentNotNullOrEmpty(name, nameof(name));

				Name = name;
				Reason = reason;
				CorrectName = correctName;
			}

			[NotNull]
			private string Name { get; }

			[CanBeNull]
			public string Reason { get; }

			[CanBeNull]
			public string CorrectName { get; }

			public bool Matches([NotNull] string fieldName)
			{
				return string.Equals(Name, fieldName,
				                     StringComparison.InvariantCultureIgnoreCase);
			}
		}
	}
}
