using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Tests.Transformers
{
	public class TransformedTableFields
	{
		private readonly IReadOnlyTable _sourceTable;

		public TransformedTableFields([NotNull] IReadOnlyTable sourceTable)
		{
			_sourceTable = sourceTable;
		}

		public string OutputFieldPrefix { get; set; }

		public bool ExcludeBaseRowsField { get; set; }

		public bool AreResultRowsGrouped { get; set; }

		/// <summary>
		/// The target-source index mapping.
		/// </summary>
		public IDictionary<int, int> CopyIndexMatrix { get; set; } = new Dictionary<int, int>();

		public List<FieldInfo> CalculatedFields { get; private set; }
		public TableView TableView { get; private set; }

		/// <summary>
		/// Adds all fields from the source table to the specified output table in the exact same
		/// order. If no fields have been previously added, the field indexes are identical and the
		/// the <see cref="CopyIndexMatrix"/> is not needed for target->source index look-ups.
		/// </summary>
		/// <param name="toOutputClass"></param>
		/// <param name="skipIfAlreadyExist"></param>
		public void AddAllFields(GdbTable toOutputClass,
		                         bool skipIfAlreadyExist = false)
		{
			IFields sourceFields = _sourceTable.Fields;
			for (int sourceIdx = 0; sourceIdx < sourceFields.FieldCount; sourceIdx++)
			{
				if (skipIfAlreadyExist)
				{
					IField sourceField = _sourceTable.Fields.Field[sourceIdx];

					if (toOutputClass.FindField(sourceField.Name) >= 0)
					{
						continue;
					}
				}

				CopyField(sourceIdx, toOutputClass, sourceFields);
			}

			if (! ExcludeBaseRowsField)
				EnsureBaseRowField(toOutputClass);
		}

		private static void EnsureBaseRowField(GdbTable inTable)
		{
			if (inTable.FindField(InvolvedRowUtils.BaseRowField) < 0)
			{
				inTable.AddFieldT(FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField));
			}
		}

		public void AddUserDefinedFields([NotNull] IList<string> userAttributes,
		                                 [NotNull] GdbTable toOutputClass,
		                                 [CanBeNull] IList<string> calculatedFields = null)
		{
			Assert.NotNull(userAttributes, nameof(userAttributes));

			const bool allowExpressions = true;
			if (! ValidateFieldNames(userAttributes, allowExpressions, out string message))
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

				string unqualifiedInputField = GetUnqualifiedFieldName(_sourceTable, inputField);

				int sourceIndex = _sourceTable.FindField(unqualifiedInputField);

				if (resultField.Equals(inputField, StringComparison.InvariantCultureIgnoreCase))
				{
					resultField = unqualifiedInputField;
				}

				//if (resultField.Contains('.'))
				//{
				//	throw new InvalidOperationException(
				//		$"Error adding fields to {toOutputClass.Name}: Result field {resultField} should be unqualified name");
				//}

				CopyField(sourceIndex, toOutputClass, _sourceTable.Fields, resultField);
			}

			AddExpressionFields(expressionAttributes, toOutputClass, calculatedFields);

			if (! ExcludeBaseRowsField)
				EnsureBaseRowField(toOutputClass);
		}

		public void AddOIDField([NotNull] GdbTable toOutputClass)
		{
			toOutputClass.AddFieldT(FieldUtils.CreateOIDField());
		}

		public void AddShapeField([NotNull] GdbFeatureClass toOutputClass,
		                          esriGeometryType newGeometryType,
		                          [NotNull] IGeometryDef geometryDef)
		{
			IField shapeField = FieldUtils.CreateShapeField(
				newGeometryType,
				geometryDef.SpatialReference, geometryDef.GridSize[0], geometryDef.HasZ,
				geometryDef.HasM);

			toOutputClass.AddFieldT(shapeField);
		}
		//public void AddMinimumFields([NotNull] GdbTable toOutputClass,
		//                             [CanBeNull] IGeometryDef geometryDef = null)
		//{
		//	AddOIDField(toOutputClass);

		//	if (geometryDef != null && toOutputClass is GdbFeatureClass featureClass)
		//	{
		//		AddShapeField(featureClass, );
		//	}

		//}

		public bool ValidateFieldNames([NotNull] IList<string> userAttributes,
		                               bool allowExpressions,
		                               out string message)
		{
			message = null;

			HashSet<string> resultFieldNames = new HashSet<string>();

			foreach (string userAttribute in userAttributes)
			{
				string sourceField =
					ExpressionUtils.GetExpression(userAttribute, out string resultField);

				if (resultFieldNames.Contains(resultField))
				{
					message = $"The field name '{resultField}' is defined multiple times.";
					return false;
				}

				resultFieldNames.Add(resultField);

				if (IsExpression(sourceField, out List<string> expressionTokens))
				{
					if (! ValidateExpression(allowExpressions, expressionTokens, sourceField,
					                         out message))
					{
						return false;
					}
				}
				else
				{
					if (! ValidateField(_sourceTable, sourceField, out message))
					{
						return false;
					}
				}
			}

			return true;
		}

		private bool IsExpression([NotNull] string sourceField,
		                          out List<string> expressionTokens)
		{
			expressionTokens = ExpressionUtils.GetExpressionTokens(sourceField).ToList();

			return expressionTokens.Count > 1;
		}

		private bool ValidateExpression(bool allowExpressions,
		                                [NotNull] List<string> expressionTokens,
		                                string inputField,
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
				if (ValidateField(_sourceTable, candidate, out message))
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

		private void CopyField(int sourceIdx,
		                       [NotNull] GdbTable toOutputClass,
		                       [NotNull] IFields sourceFields,
		                       [CanBeNull] string resultFieldName = null)
		{
			Assert.True(sourceIdx >= 0 && sourceIdx < sourceFields.FieldCount,
			            $"Invalid source index: {sourceIdx}");

			IField field = sourceFields.Field[sourceIdx];

			string targetName = resultFieldName ?? field.Name;

			if (OutputFieldPrefix != null)
			{
				targetName = OutputFieldPrefix + targetName;
			}

			if (field.Name != targetName)
			{
				IField clone = (IField) ((IClone) field).Clone();
				((IFieldEdit) clone).Name_2 = targetName;
				field = clone;
			}

			int targetIdx = toOutputClass.AddFieldT(field);

			CopyIndexMatrix.Add(targetIdx, sourceIdx);
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
			                                                  OutputFieldPrefix);

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

			if (fieldName.StartsWith(sourceTable.Name, StringComparison.InvariantCultureIgnoreCase))
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
				TableViewFactory.Create(_sourceTable, expressionDict, aliasFieldDict,
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

			if (CalculatedFields == null)
			{
				CalculatedFields = new List<FieldInfo>();
			}

			CalculatedFields.Add(new FieldInfo(field, toOutputClass.FindField(field), -1));
		}

		#endregion
	}
}
