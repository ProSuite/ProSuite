using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Container.TestContainer
{
	/// <summary>
	/// Encapsulates the sub-fields of a table filter.
	/// </summary>
	internal class TableSubFields
	{
		private static readonly bool _includeBlobFields =
			EnvironmentUtils.GetBooleanEnvironmentVariableValue("PROSUITE_QA_INCLUDE_BLOB_FIELDS");

		[NotNull] private readonly List<string> _excludedFields = new List<string>();

		private const string _allFields = "*";

		/// <summary>
		/// Initializes a new instance of the <see cref="TableSubFields"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="shapeFieldExcluded">if set to <c>true</c> the shape field was excluded for the feature class.</param>
		public TableSubFields([NotNull] IReadOnlyTable table,
		                      bool shapeFieldExcluded)
		{
			Table = table;
			ShapeFieldExcluded = shapeFieldExcluded;

			if (shapeFieldExcluded && table is IReadOnlyFeatureClass fc)
			{
				_excludedFields.Add(fc.ShapeFieldName);
			}
		}

		[NotNull]
		public IReadOnlyTable Table { get; }

		public bool ShapeFieldExcluded { get; }

		[NotNull]
		public string GetSubFields()
		{
			return TryExcludeFieldNames(out string subFields) ? subFields : _allFields;
		}

		public bool AdaptSubFields([CanBeNull] string originalSubFields,
		                           out string result)
		{
			result = null;

			if (! string.IsNullOrEmpty(originalSubFields) &&
			    ! originalSubFields.Equals("*"))
			{
				// If someone else explicitly requested the sub-field, we don't change it
				return false;
			}

			if (TryExcludeFieldNames(out string subFieldsWithoutExcluded))
			{
				result = subFieldsWithoutExcluded;
				return true;
			}

			return false;
		}

		public override string ToString() =>
			$"{Table.Name}; {GetSubFields()}; excluded:{StringUtils.Concatenate(_excludedFields, ", ")}";

		private bool TryExcludeFieldNames(out string subFieldsWithoutExcluded)
		{
			IList<IField> fields = DatasetUtils.GetFields(Table.Fields);

			var sb = new StringBuilder();

			bool hasExcludedFields = false;
			for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
			{
				if (IsExcluded(fields, fieldIndex))
				{
					hasExcludedFields = true;
					continue;
				}

				sb.AppendFormat("{0},", fields[fieldIndex].Name);
			}

			subFieldsWithoutExcluded = sb.ToString(0, sb.Length - 1);

			return hasExcludedFields;
		}

		private bool IsExcluded([NotNull] IList<IField> fields,
		                        int fieldIndex)
		{
			string fieldName = fields[fieldIndex].Name;

			if (_excludedFields.Any(
				    excludedField => string.Equals(
					    fieldName, excludedField, StringComparison.OrdinalIgnoreCase)))
			{
				return true;
			}

			if (! _includeBlobFields &&
			    fields[fieldIndex].Type == esriFieldType.esriFieldTypeBlob &&
			    fieldName != InvolvedRowUtils.BaseRowField)
			{
				return true;
			}

			return false;
		}
	}
}
