using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Tests.Transformers
{
	/// <summary>
	/// Provides functionality to organize the fields in a result table that originate from a
	/// single source table or are calculated fields using input values from the source table.
	/// The mapping between the fields is maintained in <see cref="FieldIndexMapping"/>
	/// to support look-up of source values by output field index..
	/// </summary>
	public class TransformedTableFields
	{
		private const string _tablePrefixFormat = "{0}_{1}";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public IReadOnlyTable SourceTable { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformedTableFields"/> class.
		/// </summary>
		/// <param name="sourceTable"></param>
		public TransformedTableFields([NotNull] IReadOnlyTable sourceTable)
		{
			SourceTable = sourceTable;
		}

		/// <summary>
		/// Prefix for the fields that are copied 1:1 by the <see cref="AddAllFields"/> method.
		/// Calculated or other user-defined fields are not automatically prefixed.
		/// </summary>
		public string NonUserDefinedFieldPrefix { get; set; }

		public bool ExcludeBaseRowsField { get; set; }

		public bool AreResultRowsGrouped { get; set; }

		/// <summary>
		/// The target-source index mapping to allow looking up the field index of the source table
		/// using the index of the output table.
		/// </summary>
		[NotNull]
		public IDictionary<int, int> FieldIndexMapping { get; set; } = new Dictionary<int, int>();

		/// <summary>
		/// The list of fields that are calculated and do not exist as physical fields in the
		/// source. Especially if the resulting rows are grouped calculated fields are used
		/// to provide aggregate functions such as SUM, COUNT etc.
		/// </summary>
		[CanBeNull]
		public IReadOnlyList<FieldInfo> CalculatedFields => _calculatedFields;

		private List<FieldInfo> _calculatedFields;

		public IList<string> ExcludedSourceFields { get; } = new List<string>();

		public int ShapeFieldIndex { get; private set; } = -1;

		[CanBeNull]
		private TableView TableView { get; set; }

		public IDictionary<int, string> AddedFieldsSourceTable { get; } =
			new Dictionary<int, string>();

		public HashSet<int> AddedFields { get; } = new HashSet<int>();

		public IList<TransformedTableFields> PreviouslyAddedFields { get; } =
			new List<TransformedTableFields>();

		/// <summary>
		/// Adds all fields from the source table to the specified output table in the exact same
		/// order. If no fields have been previously added, the field indexes are identical and the
		/// the <see cref="FieldIndexMapping"/> is not needed for target->source index look-ups.
		/// A shape field is only added if no shape field exists in the output class.
		/// The OID field is always added, however it will only become the official OIDField of the
		/// target if no OID field has been set previously. It is advisable to set the OID field
		/// explicitly using <see cref="AddOIDField"/> or, if the values are generated by the caller
		/// rather than taken from the source rows, <see cref="AddCustomOIDField"/>. 
		/// </summary>
		/// <param name="toOutputClass"></param>
		/// <param name="skipIfAlreadyExist"></param>
		public void AddAllFields([NotNull] GdbTable toOutputClass,
		                         bool skipIfAlreadyExist = false)
		{
			IFields sourceFields = SourceTable.Fields;
			for (int sourceIdx = 0; sourceIdx < sourceFields.FieldCount; sourceIdx++)
			{
				IField sourceField = SourceTable.Fields.Field[sourceIdx];

				string targetName = NonUserDefinedFieldPrefix != null
					                    ? NonUserDefinedFieldPrefix + sourceField.Name
					                    : sourceField.Name;

				int conflictingFieldIndex = toOutputClass.FindField(targetName);
				bool alreadyExists = conflictingFieldIndex >= 0;

				string newFieldName = targetName;
				if (skipIfAlreadyExist)
				{
					if (alreadyExists)
					{
						continue;
					}
				}
				else if (alreadyExists)
				{
					// Prefix with tableName_
					newFieldName = MakeUnique(targetName, toOutputClass);
				}

				int targetIndex = CopyField(sourceIdx, toOutputClass, sourceFields, newFieldName);

				if (alreadyExists && targetIndex >= 0)
				{
					// The field was actually added but there is an existing field that should
					// also  be qualified
					PrefixConflictingField(toOutputClass, conflictingFieldIndex, targetName);
				}
			}

			if (! ExcludeBaseRowsField)
				EnsureBaseRowField(toOutputClass);
		}

		public void AddUserDefinedFields([NotNull] IList<string> userAttributes,
		                                 [NotNull] GdbTable toOutputClass,
		                                 [CanBeNull] IList<string> calculatedFields = null)
		{
			Assert.NotNull(userAttributes, nameof(userAttributes));

			const bool allowExpressions = true;
			IList<string> allAttributes;
			if (calculatedFields == null)
			{
				allAttributes = userAttributes;
			}
			else
			{
				List<string> comb = new List<string>(calculatedFields);
				comb.AddRange(userAttributes);
				allAttributes = comb;
			}

			if (! ValidateFieldNames(allAttributes, allowExpressions, out string message))
			{
				throw new InvalidOperationException(
					$"Error adding fields to {toOutputClass.Name}: {message}");
			}

			List<string> expressionAttributes = new List<string>();

			foreach (string userAttribute in userAttributes)
			{
				string inputField =
					ExpressionUtils.GetExpression(userAttribute, out string resultField);

				if (IsExpression(inputField, out List<string> _))
				{
					expressionAttributes.Add(userAttribute);
					continue;
				}

				string unqualifiedInputField = GetUnqualifiedFieldName(SourceTable, inputField);

				int sourceIndex = SourceTable.FindField(unqualifiedInputField);

				if (resultField.Equals(inputField, StringComparison.InvariantCultureIgnoreCase))
				{
					resultField = unqualifiedInputField;
				}

				//if (resultField.Contains('.'))
				//{
				//	throw new InvalidOperationException(
				//		$"Error adding fields to {toOutputClass.Name}: Result field {resultField} should be unqualified name");
				//}

				CopyField(sourceIndex, toOutputClass, SourceTable.Fields, resultField);
			}

			if (expressionAttributes.Count > 0)
			{
				AddExpressionFields(expressionAttributes, toOutputClass, calculatedFields);
			}

			if (! ExcludeBaseRowsField)
				EnsureBaseRowField(toOutputClass);
		}

		/// <summary>
		/// Adds the OID field from the source table to the target and adds the corresponding
		/// field index mapping to the <see cref="FieldIndexMapping"/>. Make sure to provided
		/// the OID value from the source row when creating the target row.
		/// </summary>
		public void AddOIDField([NotNull] GdbTable toOutputClass,
		                        string name = null,
		                        bool omitFromFieldIndexMapping = false)
		{
			if (! SourceTable.HasOID)
			{
				throw new InvalidOperationException(
					$"Source table {SourceTable.Name} has no OID field that can be used in target");
			}

			int sourceIndex = SourceTable.FindField(SourceTable.OIDFieldName);

			Assert.False(sourceIndex < 0, "No OID field found in source table");

			CopyField(sourceIndex, toOutputClass, SourceTable.Fields, name,
			          omitFromFieldIndexMapping);
		}

		/// <summary>
		/// Adds the SHAPE field from the source table to the target and adds the corresponding
		/// field index mapping to the <see cref="FieldIndexMapping"/>. This shall be used if the
		/// target rows should transparently provide the source rows' shape.
		/// </summary>
		public void AddShapeField([NotNull] GdbFeatureClass toOutputClass,
		                          [CanBeNull] string fieldName = "SHAPE",
		                          bool omitFromFieldIndexMapping = false)
		{
			var sourceFeatureClass = SourceTable as IReadOnlyFeatureClass;

			Assert.NotNull(sourceFeatureClass, "Source is no feature class");

			if (HasShapeField(toOutputClass))
			{
				throw new InvalidOperationException(
					$"Feature class {toOutputClass.Name} already has a shape field");
			}

			int sourceIndex = sourceFeatureClass.FindField(sourceFeatureClass.ShapeFieldName);

			int targetIndex = CopyField(sourceIndex, toOutputClass, SourceTable.Fields, fieldName,
			                            omitFromFieldIndexMapping);

			if (targetIndex >= 0)
			{
				ShapeFieldIndex = targetIndex;
			}
		}

		/// <summary>
		/// Add an OBJECTID field to the output table and make it the official OID field of the
		/// output table. The client code will be responsible for providing a valid OID value
		/// at the index of this field for each created row e.g. via a value list.
		/// </summary>
		/// <param name="toOutputClass"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public int AddCustomOIDField([NotNull] GdbTable toOutputClass,
		                             string name = "OBJECTID")
		{
			int resultIndex = toOutputClass.AddFieldT(FieldUtils.CreateOIDField(name));

			toOutputClass.SetOIDFieldName(name);

			return resultIndex;
		}

		/// <summary>
		/// Add a SHAPE field to the output feature class which will be under the control of the
		/// calling code. The client code will be responsible for providing a valid geometry
		/// at the index of this field for each created row e.g. via a value list.
		/// </summary>
		/// <param name="toOutputClass"></param>
		/// <param name="newGeometryType"></param>
		/// <param name="geometryDef"></param>
		public void AddCustomShapeField([NotNull] GdbFeatureClass toOutputClass,
		                                esriGeometryType newGeometryType,
		                                [NotNull] IGeometryDef geometryDef)
		{
			IField shapeField = FieldUtils.CreateShapeField(
				newGeometryType,
				geometryDef.SpatialReference, geometryDef.GridSize[0], geometryDef.HasZ,
				geometryDef.HasM);

			toOutputClass.AddFieldT(shapeField);
		}

		public void ExcludeAllShapeFields()
		{
			if (SourceTable is IReadOnlyFeatureClass featureClass)
			{
				ExcludedSourceFields.Add(featureClass.ShapeFieldName);

				string lengthField = DatasetUtils.GetLengthField(featureClass)?.Name;
				string areaField = DatasetUtils.GetAreaField(featureClass)?.Name;

				if (lengthField != null)
				{
					ExcludedSourceFields.Add(lengthField);
				}

				if (areaField != null)
				{
					ExcludedSourceFields.Add(areaField);
				}
			}
		}

		public bool ValidateFieldNames([NotNull] IList<string> userAttributes,
		                               bool allowExpressions,
		                               out string message)
		{
			message = null;

			HashSet<string> resultFieldNames =
				new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (string userAttribute in userAttributes)
			{
				if (string.IsNullOrEmpty(userAttribute))
				{
					message =
						$"Null or empty field name or expression defined for {SourceTable.Name}";
					return false;
				}

				string sourceField =
					ExpressionUtils.GetExpression(userAttribute, out string resultField);

				if (resultFieldNames.Contains(resultField))
				{
					message = $"The field name '{resultField}' is defined multiple times.";
					return false;
				}

				if (IsExpression(sourceField, out List<string> expressionTokens))
				{
					if (! ValidateExpression(allowExpressions, expressionTokens, sourceField,
					                         resultFieldNames, out message))
					{
						return false;
					}
				}
				else
				{
					if (! ValidateField(SourceTable, sourceField, out message))
					{
						return false;
					}
				}

				resultFieldNames.Add(resultField);
			}

			return true;
		}

		public IEnumerable<CalculatedValue> GetCalculatedValues(
			[NotNull] IList<IReadOnlyRow> rowsToGroup)
		{
			if (rowsToGroup.Count == 0)
			{
				yield break;
			}

			// For several source rows, they all must be grouped into one by calculation functions:
			// TODO: Are there exceptions? Group-by value? -> Test

			IReadOnlyList<FieldInfo> calculatedFields = CalculatedFields;

			if (calculatedFields == null)
			{
				yield break;
			}

			foreach (CalculatedValue calculatedValue in GetCalculatedValues(
				         rowsToGroup, calculatedFields,
				         Assert.NotNull(TableView)))
			{
				yield return calculatedValue;
			}
		}

		private static IEnumerable<CalculatedValue> GetCalculatedValues(
			[NotNull] IList<IReadOnlyRow> sources,
			[NotNull] IReadOnlyList<FieldInfo> calculatedFields,
			[NotNull] TableView tableView)
		{
			// NOTE: The tableView never contains columns from several source tables

			DataRow tableRow = null;
			foreach (IReadOnlyRow row in sources)
			{
				tableRow = tableView.Add(row);
			}

			if (tableRow != null)
			{
				Assert.NotNull(calculatedFields);

				foreach (FieldInfo fieldInfo in calculatedFields)
				{
					yield return new CalculatedValue(targetIndex: fieldInfo.Index,
					                                 value: tableRow[fieldInfo.Name]);
				}
			}

			tableView.ClearRows();
		}

		private static bool IsExpression([NotNull] string sourceField,
		                                 out List<string> expressionTokens)
		{
			expressionTokens = ExpressionUtils.GetExpressionTokens(sourceField).ToList();

			return expressionTokens.Count > 1;
		}

		private bool ValidateExpression(bool allowExpressions,
		                                [NotNull] List<string> expressionTokens,
		                                string inputField,
		                                [NotNull] HashSet<string> previousFields,
		                                out string message)
		{
			if (! allowExpressions)
			{
				message =
					$"Field {inputField} is an expression. This is not yet supported.";
				return false;
			}

			bool any = false;
			foreach (string candidate in expressionTokens)
			{
				if (ValidateField(SourceTable, candidate, out message)
				    || previousFields.Contains(candidate))
				{
					any = true;
				}
			}

			if (! any)
			{
				message =
					$"The expression {inputField} does not contain a valid field name.";
				return false;
			}

			message = null;
			return true;
		}

		private static bool HasShapeField(IReadOnlyTable table)
		{
			if (! (table is IReadOnlyFeatureClass featureClass))
			{
				return false;
			}

			return featureClass.ShapeFieldName != null
			       && featureClass.FindField(featureClass.ShapeFieldName) >= 0;
		}

		private int CopyField(int sourceIdx,
		                      [NotNull] GdbTable toOutputClass,
		                      [NotNull] IFields sourceFields,
		                      [CanBeNull] string resultFieldName = null,
		                      bool omitFromFieldIndexMapping = false)
		{
			Assert.True(sourceIdx >= 0 && sourceIdx < sourceFields.FieldCount,
			            $"Invalid source index: {sourceIdx}");

			IField field = CanAddField(sourceIdx, toOutputClass, sourceFields, resultFieldName);

			if (field != null)
			{
				return AddField(field, toOutputClass, sourceIdx, omitFromFieldIndexMapping);
			}

			return -1;
		}

		private IField CanAddField(int sourceIdx,
		                           [NotNull] GdbTable toOutputClass,
		                           [NotNull] IFields sourceFields,
		                           [CanBeNull] string resultFieldName)
		{
			IField field = sourceFields.Field[sourceIdx];

			if (field.Type == esriFieldType.esriFieldTypeGeometry && HasShapeField(toOutputClass))
			{
				// It's already got one
				return null;
			}

			if (ExcludedSourceFields.Contains(field.Name))
			{
				// NOTE: Included Length/Area fields fail in search if the respective shape is excluded
				return null;
			}

			string targetName = resultFieldName ?? field.Name;

			// Always clone the field, even if it is not re-named now! It could be re-named
			// somewhere down the road:
			field = (IField) ((IClone) field).Clone();

			if (field.Name != targetName)
			{
				((IFieldEdit) field).Name_2 = targetName;
			}

			return field;
		}

		private int AddField(IField field, GdbTable toOutputClass, int sourceIdx,
		                     bool omitFromFieldIndexMapping)
		{
			int targetIdx;
			try
			{
				targetIdx = toOutputClass.AddFieldT(field);

				if (field.Type == esriFieldType.esriFieldTypeOID &&
				    ! toOutputClass.HasOID)
				{
					toOutputClass.SetOIDFieldName(field.Name);
				}
			}
			catch (Exception e)
			{
				_msg.Debug($"Error adding field {field.Name} to {toOutputClass.Name}.", e);
				_msg.Debug(GetFieldListMessage(toOutputClass));

				throw;
			}

			if (! omitFromFieldIndexMapping)
			{
				FieldIndexMapping.Add(targetIdx, sourceIdx);
				AddedFields.Add(targetIdx);
			}

			return targetIdx;
		}

		private bool ValidateField(IReadOnlyTable sourceTable, string fieldName,
		                           out string message)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				message = $"Null or empty field name defined for {sourceTable.Name}";
				return false;
			}

			string unqualifiedField = GetUnqualifiedFieldName(sourceTable, fieldName,
			                                                  NonUserDefinedFieldPrefix);

			if (sourceTable.FindField(unqualifiedField) < 0)
			{
				message =
					$"Field name {fieldName} does not exist in {sourceTable.Name}. Valid fields are: {GetFieldList(sourceTable)}";
				return false;
			}

			// TODO: Check for illegal characters, reserved names etc.

			message = null;
			return true;
		}

		private static string GetUnqualifiedFieldName([NotNull] IReadOnlyTable sourceTable,
		                                              [NotNull] string fieldName,
		                                              string fieldPrefix = null)
		{
			// Repeating the name of the current table is not necessary for single tables but might be done for clarity

			if (!string.IsNullOrWhiteSpace(sourceTable.Name)
			    && fieldName.StartsWith(sourceTable.Name, StringComparison.InvariantCultureIgnoreCase))
			{
				string remainder = fieldName.Substring(sourceTable.Name.Length);

				return remainder.Length > 1 ? remainder.Substring(1) : null;
			}

			// Also check the prefix that could be used as table-alias.
			if (! string.IsNullOrEmpty(fieldPrefix) &&
			    fieldName.StartsWith(fieldPrefix, StringComparison.InvariantCultureIgnoreCase))
			{
				string remainder = fieldName.Substring(fieldPrefix.Length);

				return remainder.Length > 0 ? remainder : null;
			}

			return fieldName;
		}

		private static string GetFieldList(IReadOnlyTable table)
		{
			var fieldList = DatasetUtils.GetFields(table.Fields)
			                            .Where(f => f.Name != InvolvedRowUtils.BaseRowField)
			                            .Select(f => f.Name).ToList();

			string fieldDisplayList =
				$"{Environment.NewLine}{StringUtils.Concatenate(fieldList, Environment.NewLine)}";

			return fieldDisplayList;
		}

		private static void EnsureBaseRowField(GdbTable inTable)
		{
			if (inTable.FindField(InvolvedRowUtils.BaseRowField) < 0)
			{
				inTable.AddFieldT(FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField));
			}
		}

		private static string GetFieldListMessage(IReadOnlyTable table)
		{
			var fieldList = DatasetUtils.GetFields(table.Fields)
			                            .Where(f => f.Name != InvolvedRowUtils.BaseRowField)
			                            .Select(f => f.Name).ToList();

			string fieldDisplayList = $"List of fields of {table.Name}: " +
			                          $"{Environment.NewLine}{StringUtils.Concatenate(fieldList, Environment.NewLine)}";

			return fieldDisplayList;
		}

		private static string PrefixDuplicate(string sourceTableName, string targetName)
		{
			// TODO: Consider remembering the list of duplicates to also pre-fix the other table's fields

			// Un-qualify the table name
			ModelElementNameUtils.TryUnqualifyName(sourceTableName,
			                                       out string unqualifiedTableName);

			return string.Format(_tablePrefixFormat, unqualifiedTableName, targetName);
		}

		private string MakeUnique([NotNull] string targetName,
		                          [NotNull] GdbTable targetTable)
		{
			// TODO: Consider remembering the list of duplicates to also pre-fix the other table's fields

			string result = PrefixDuplicate(SourceTable.Name, targetName);

			if (targetTable.FindField(result) < 0)
			{
				return result;
			}

			// Typically foreign keys are also prefixed with related table name
			var regex = new Regex(@"[0-9]+$");
			var match = regex.Match(result);

			int number = 0;
			int numberIndex = result.Length;
			if (match.Success)
			{
				int.TryParse(match.Value, out number);
				numberIndex = match.Index - 1;
			}

			while (targetTable.FindField(result) >= 0)
			{
				result = string.Format($"{result.Substring(0, numberIndex)}_{++number}");
			}

			return result;
		}

		private void PrefixConflictingField(GdbTable toOutputClass, int conflictingFieldIndex,
		                                    string targetName)
		{
			IField existingField = toOutputClass.Fields.Field[conflictingFieldIndex];

			if (targetName == toOutputClass.OIDFieldName)
			{
				return;
			}

			if (targetName == toOutputClass.ShapeFieldName)
			{
				return;
			}

			// Make sure there is no reference by name:
			if (CalculatedFields != null &&
			    CalculatedFields.Any(c => c.Name == existingField.Name))
			{
				return;
			}

			string otherSourceTable = null;
			foreach (TransformedTableFields otherFields in PreviouslyAddedFields)
			{
				if (otherFields.AddedFields.Contains(conflictingFieldIndex))
				{
					otherSourceTable = otherFields.SourceTable.Name;
				}
			}

			if (otherSourceTable != null)
			{
				string newName = PrefixDuplicate(otherSourceTable, targetName);

				if (toOutputClass.FindField(newName) >= 0)
				{
					// Rather keep it than make the field names in-transparent to the user:
					return;
				}

				// Rename the existing fields:
				((IFieldEdit) existingField).Name_2 = newName;
			}
		}

		#region Calculated field set up originally from TrSpatialJoin

		private void AddExpressionFields(IList<string> expressionAttributes,
		                                 GdbTable toOutputClass,
		                                 [CanBeNull] IList<string> calculatedFields = null)
		{
			Dictionary<string, string> expressionDict =
				ExpressionUtils.GetFieldDict(expressionAttributes);
			Dictionary<string, string> aliasFieldDict =
				ExpressionUtils.CreateAliases(expressionDict);
			Dictionary<string, string> calcExpressionDict = null;

			if (calculatedFields?.Count > 0)
			{
				calcExpressionDict = ExpressionUtils.GetFieldDict(calculatedFields);
				Dictionary<string, string> calcAliasFieldDict =
					ExpressionUtils.CreateAliases(calcExpressionDict);

				foreach (KeyValuePair<string, string> pair in aliasFieldDict)
				{
					calcAliasFieldDict.Add(pair.Key, pair.Value);
				}

				aliasFieldDict = calcAliasFieldDict;
			}

			TableView =
				TableViewFactory.Create(SourceTable, expressionDict, aliasFieldDict,
				                        AreResultRowsGrouped, calcExpressionDict);

			foreach (string field in expressionDict.Keys)
			{
				AddCalculatedField(toOutputClass, field, TableView);
			}
		}

		private void AddCalculatedField([NotNull] GdbTable toOutputClass,
		                                [NotNull] string field,
		                                [NotNull] TableView tableView)
		{
			IField f =
				FieldUtils.CreateField(
					field, FieldUtils.GetFieldType(tableView.GetColumnType(field)));
			toOutputClass.AddFieldT(f);

			_calculatedFields = _calculatedFields ?? new List<FieldInfo>();

			_calculatedFields.Add(new FieldInfo(field, toOutputClass.FindField(field), -1));
		}

		#endregion
	}
}
