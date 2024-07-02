using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaEmptyNotNullTextFields : ContainerTest
	{
		private readonly IList<int> _notNullTextFieldIndices;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ValueIsNull = "ValueIsNull";
			public const string UnexpectedValue = "UnexpectedValue";
			public const string EmptyText = "EmptyText";

			public Code() : base("EmptyNotNullTextFields") { }
		}

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_0))]
		public QaEmptyNotNullTextFields(
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_table))] [NotNull]
			IReadOnlyTable table)
			: this(table, GetNotNullTextFields(table)) { }

		[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_1))]
		public QaEmptyNotNullTextFields(
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_notNullTextFields))] [NotNull]
			string[] notNullTextFields)
			: base(table)
		{
			Assert.ArgumentNotNull(notNullTextFields, nameof(notNullTextFields));

			var fieldIndices = new List<int>(notNullTextFields.Length);
			foreach (string notNullTextField in notNullTextFields)
			{
				int fieldIndex = table.FindField(notNullTextField);
				Assert.True(fieldIndex >= 0, "field '{0}' not found in table '{1}'",
				            notNullTextField, table.Name);

				fieldIndices.Add(fieldIndex);
			}

			_notNullTextFieldIndices = fieldIndices;
		}

		[InternallyUsedTest]
		public QaEmptyNotNullTextFields([NotNull] QaEmptyNotNullTextFieldsDefinition definition)
			: base((IReadOnlyTable) definition.Table)
		{
			if (definition.NotNullTextFields == null)
			{
				var fieldIndices =
					new List<int>(GetNotNullTextFields((IReadOnlyTable) definition.Table).Length);
				foreach (string notNullTextField in GetNotNullTextFields(
					         (IReadOnlyTable) definition.Table))
				{
					int fieldIndex = definition.Table.FindField(notNullTextField);
					Assert.True(fieldIndex >= 0, "field '{0}' not found in table '{1}'",
					            notNullTextField, definition.Table.Name);

					fieldIndices.Add(fieldIndex);
				}

				_notNullTextFieldIndices = fieldIndices;
			}
		}

		#endregion

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (_notNullTextFieldIndices.Count == 0)
			{
				return 0;
			}

			int errorCount = 0;
			foreach (int fieldIndex in _notNullTextFieldIndices)
			{
				object value = row.get_Value(fieldIndex);

				string fieldName;

				if (value is DBNull || value == null)
				{
					string description = string.Format(
						"field value is null: {0}",
						TestUtils.GetFieldDisplayName(row, fieldIndex, out fieldName));

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row),
						TestUtils.GetShapeCopy(row), Codes[Code.ValueIsNull], fieldName);
				}
				else
				{
					var stringValue = value as string;
					if (stringValue == null)
					{
						string description = string.Format(
							"Unexpected value for field {0}: {1}",
							TestUtils.GetFieldDisplayName(row, fieldIndex, out fieldName),
							value);

						errorCount += ReportError(
							description, InvolvedRowUtils.GetInvolvedRows(row),
							TestUtils.GetShapeCopy(row), Codes[Code.UnexpectedValue], fieldName);
					}
					else if (stringValue.Length == 0)
					{
						string description = string.Format(
							"Empty text in field {0}",
							TestUtils.GetFieldDisplayName(row, fieldIndex, out fieldName));

						errorCount += ReportError(
							description, InvolvedRowUtils.GetInvolvedRows(row),
							TestUtils.GetShapeCopy(row), Codes[Code.EmptyText], fieldName);
					}
				}
			}

			return errorCount;
		}

		[NotNull]
		private static string[] GetNotNullTextFields([NotNull] IReadOnlyTable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var list = new List<string>();
			foreach (IField field in DatasetUtils.GetFields(table.Fields))
			{
				if (field.Type != esriFieldType.esriFieldTypeString)
				{
					continue;
				}

				if (field.IsNullable)
				{
					continue;
				}

				list.Add(field.Name);
			}

			return list.ToArray();
		}
	}
}
