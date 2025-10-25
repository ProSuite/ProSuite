using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Checks Constraints on a table
	/// </summary>
	[UsedImplicitly]
	[AttributeTest]
	public class QaValue : ContainerTest
	{
		private readonly Dictionary<string, FieldInfo> _fieldInfos;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ValueNotValidForFieldType =
				"ValueNotValidForFieldType";

			public Code() : base("FieldValues") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaValue_0))]
		public QaValue(
			[Doc(nameof(DocStrings.QaValue_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValue_fields))] [CanBeNull]
			IList<string> fields)
			: base(table)
		{
			_fieldInfos = GetFieldInfos(table, GetFieldNames(table, fields));
		}

		[InternallyUsedTest]
		public QaValue([NotNull] QaValueDefinition definition)
			: this((IReadOnlyTable)definition.Table, definition.Fields)
		{ }

		[NotNull]
		private static List<string> GetFieldNames(
			[NotNull] IReadOnlyTable table,
			[CanBeNull] IEnumerable<string> fields)
		{
			return fields == null
				       ? DatasetUtils.GetFields(table.Fields)
				                     .Select(field => field.Name)
				                     .ToList()
				       : new List<string>(fields);
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			AssertValidInvolvedTableIndex(tableIndex);

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

		protected override void ConfigureQueryFilter(int tableIndex,
		                                             ITableFilter queryFilter)
		{
			base.ConfigureQueryFilter(tableIndex, queryFilter);

			foreach (string field in _fieldInfos.Keys)
			{
				queryFilter.AddField(field);
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return _fieldInfos.Values.Sum(fieldInfo => CheckField(row, fieldInfo));
		}

		[NotNull]
		private static Dictionary<string, FieldInfo> GetFieldInfos(
			[NotNull] IReadOnlyTable table,
			[NotNull] ICollection<string> fieldNames)
		{
			var result = new Dictionary<string, FieldInfo>(fieldNames.Count);

			IFields fields = table.Fields;

			foreach (string fieldName in fieldNames)
			{
				Assert.ArgumentCondition(StringUtils.IsNotEmpty(fieldName),
				                         "undefined field name");

				int fieldIndex = table.FindField(fieldName);

				Assert.ArgumentCondition(fieldIndex >= 0,
				                         "field '{0}' not found in table '{1}'",
				                         fieldName,
				                         table.Name);

				IField field = fields.get_Field(fieldIndex);

				result.Add(fieldName, new FieldInfo(fieldName, field, fieldIndex));
			}

			return result;
		}

		private int CheckField([NotNull] IReadOnlyRow row, [NotNull] FieldInfo fieldInfo)
		{
			object value = row.get_Value(fieldInfo.Index);

			if (value == DBNull.Value || value == null)
			{
				return NoError;
			}

			if (fieldInfo.Validate(value))
			{
				return NoError;
			}

			// not valid

			string description =
				string.IsNullOrEmpty(fieldInfo.LastMessage)
					? string.Format("Invalid value in field {0}: {1}",
					                fieldInfo.Name, value)
					: string.Format("Invalid value in field {0}: {1} ({2})",
					                fieldInfo.Name, value, fieldInfo.LastMessage);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				TestUtils.GetShapeCopy(row),
				Codes[Code.ValueNotValidForFieldType], fieldInfo.Name);
		}

		#region Nested type: FieldInfo

		private class FieldInfo
		{
			public readonly int Index;
			public readonly string Name;
			public readonly Func<object, bool> Validate;

			public string LastMessage;

			/// <summary>
			/// Initializes a new instance of the <see cref="FieldInfo"/> class.
			/// </summary>
			/// <param name="name">The name.</param>
			/// <param name="field">The field.</param>
			/// <param name="index">The index.</param>
			public FieldInfo([NotNull] string name, [NotNull] IField field, int index)
			{
				Name = name;
				Index = index;

				Validate = GetValidationFunction(field);
			}

			[NotNull]
			private Func<object, bool> GetValidationFunction(
				[NotNull] IField field)
			{
				switch (field.Type)
				{
					case esriFieldType.esriFieldTypeBlob:
						return o => true;

					case esriFieldType.esriFieldTypeDate:
						return o => o is DateTime;

					case esriFieldType.esriFieldTypeDouble:
						return o => o is double;

					case esriFieldType.esriFieldTypeGeometry:
						return o => o is IGeometry;

					case esriFieldType.esriFieldTypeGlobalID:
						return IsGuid;

					case esriFieldType.esriFieldTypeGUID:
						return IsGuid;

					case esriFieldType.esriFieldTypeInteger:
						return o => o is int;

					case esriFieldType.esriFieldTypeOID:
						return o => o is int;

					case esriFieldType.esriFieldTypeRaster:
						return o => true;

					case esriFieldType.esriFieldTypeSingle:
						return o => o is float;

					case esriFieldType.esriFieldTypeSmallInteger:
						return o => o is short;

					case esriFieldType.esriFieldTypeString:
						return o => o is string;

					default:
						throw new NotImplementedException(
							"Unhandled type " + field.Type);
				}
			}

			private bool IsGuid([NotNull] object value)
			{
				if (value is Guid)
				{
					return true;
				}

				var stringValue = value as string;
				if (stringValue == null)
				{
					return false;
				}

				try
				{
					// NOTE: Guid.TryParse() is not available in .Net 3.5
					new Guid(stringValue);
				}
				catch (FormatException)
				{
					return false;
				}

				if (stringValue != stringValue.ToUpper())
				{
					LastMessage = "Upper case string expected";
					return false;
				}

				return true;
			}
		}

		#endregion
	}
}
