using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRequiredFields : ContainerTest
	{
		private readonly bool _allowEmptyStrings;
		private readonly List<int> _requiredFieldIndices = new List<int>();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NullValue = "NullValue";
			public const string EmptyString = "EmptyString";

			public Code() : base("RequiredFields") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFields(
				[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaRequiredFields_requiredFieldNames))] [NotNull]
				IEnumerable<string>
					requiredFieldNames)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, requiredFieldNames, false) { }

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFields(
				[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaRequiredFields_requiredFieldNames))] [NotNull]
				IEnumerable<string>
					requiredFieldNames,
				[Doc(nameof(DocStrings.QaRequiredFields_allowEmptyStrings))]
				bool allowEmptyStrings)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, requiredFieldNames, allowEmptyStrings, false) { }

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFields(
			[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRequiredFields_requiredFieldNames))] [NotNull]
			IEnumerable<string> requiredFieldNames,
			[Doc(nameof(DocStrings.QaRequiredFields_allowEmptyStrings))]
			bool allowEmptyStrings,
			[Doc(nameof(DocStrings.QaRequiredFields_allowMissingFields))]
			bool allowMissingFields)
			: base(table)
		{
			Assert.ArgumentNotNull(requiredFieldNames, nameof(requiredFieldNames));

			_allowEmptyStrings = allowEmptyStrings;

			foreach (string fieldName in requiredFieldNames)
			{
				int index = table.FindField(fieldName);
				if (index < 0)
				{
					if (allowMissingFields)
					{
						// ignore non-existing field
						continue;
					}

					throw new ArgumentException(
						string.Format("Field not found in table {0}: {1}",
						              table.Name,
						              fieldName), nameof(requiredFieldNames));
				}

				_requiredFieldIndices.Add(index);
			}
		}

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFields(
			[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRequiredFields_requiredFieldNamesString))] [NotNull]
			string
				requiredFieldNamesString,
			[Doc(nameof(DocStrings.QaRequiredFields_allowEmptyStrings))]
			bool allowEmptyStrings,
			[Doc(nameof(DocStrings.QaRequiredFields_allowMissingFields))]
			bool allowMissingFields)
			: this(table, TestUtils.GetTokens(requiredFieldNamesString),
			       allowEmptyStrings, allowMissingFields) { }

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFields(
			[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRequiredFields_allowEmptyStrings))]
			bool allowEmptyStrings)
			: this(table, GetAllEditableFieldNames(table),
			       allowEmptyStrings, false) { }

		[InternallyUsedTest]
		public QaRequiredFields([NotNull] QaRequiredFieldsDefinition definition)
			: this((IReadOnlyTable) definition.Table,
			       definition.RequiredFieldNames ??
			       GetAllEditableFieldNames((IReadOnlyTable) definition.Table),
			       definition.AllowEmptyString, definition.AllowMissingFields) { }

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool IsGeometryUsedTable(int tableIndex)
		{
			return AreaOfInterest != null;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override void ConfigureQueryFilter(int tableIndex, ITableFilter filter)
		{
			foreach (int requiredFieldIndex in _requiredFieldIndices)
			{
				filter.AddField(InvolvedTables[tableIndex].Fields.Field[requiredFieldIndex].Name);
			}

			base.ConfigureQueryFilter(tableIndex, filter);
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			int errorCount = 0;

			foreach (int fieldIndex in _requiredFieldIndices)
			{
				object value = row.get_Value(fieldIndex);

				string fieldName;
				if (value == null || value is DBNull)
				{
					string description =
						string.Format("Required field has null value: {0}",
						              TestUtils.GetFieldDisplayName(
							              row, fieldIndex, out fieldName));

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row),
						TestUtils.GetShapeCopy(row),
						Codes[Code.NullValue], fieldName);
				}
				else
				{
					var stringValue = value as string;
					if (stringValue != null)
					{
						if (! _allowEmptyStrings && stringValue.Length == 0)
						{
							string description = string.Format(
								"Required field has empty string value: {0}",
								TestUtils.GetFieldDisplayName(row, fieldIndex, out fieldName));

							errorCount += ReportError(
								description, InvolvedRowUtils.GetInvolvedRows(row),
								TestUtils.GetShapeCopy(row), Codes[Code.EmptyString], fieldName);
						}
					}
				}
			}

			return errorCount;
		}

		[NotNull]
		private static IEnumerable<string> GetAllEditableFieldNames([NotNull] IReadOnlyTable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var featureClass = table as IReadOnlyFeatureClass;
			string shapeFieldName = featureClass?.ShapeFieldName;

			foreach (IField field in DatasetUtils.GetFields(table.Fields))
			{
				if (! field.Editable || ! field.IsNullable)
				{
					continue;
				}

				if (shapeFieldName != null &&
				    string.Equals(field.Name, shapeFieldName, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				yield return field.Name;
			}
		}
	}
}
