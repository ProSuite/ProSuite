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

namespace ProSuite.QA.Tests.Transformers
{
	public class TransformedTableFields
	{
		private readonly IReadOnlyTable _sourceTable;
		//private IList<string> _userDefinedAttributes;

		public TransformedTableFields([NotNull] IReadOnlyTable sourceTable)
		{
			_sourceTable = sourceTable;
		}

		public string OutputFieldPrefix { get; set; }

		public bool ExcludeBaseRowsField { get; set; }

		/// <summary>
		/// The target-source index mapping.
		/// </summary>
		public IDictionary<int, int> CopyIndexMatrix { get; set; } = new Dictionary<int, int>();

		//public IList<string> UserDefinedAttributes
		//{
		//	get => _userDefinedAttributes;
		//	set => _userDefinedAttributes = value;
		//}

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

			if (!ExcludeBaseRowsField)
				EnsureBaseRowField(toOutputClass);
		}

		private static void EnsureBaseRowField(GdbTable inTable)
		{

			if (inTable.FindField(InvolvedRowUtils.BaseRowField) < 0)
			{
				inTable.AddFieldT(FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField));
			}
		}

		public void AddUserDefinedFields(IList<string> userAttributes,
		                                 GdbTable toOutputClass)
		{
			const bool allowExpressions = true;
			if (! ValidateFieldNames(userAttributes, allowExpressions, out string message))
			{
				throw new InvalidOperationException(
					$"Error adding fields to {toOutputClass.Name}: {message}");
			}

			foreach (string userAttribute in userAttributes)
			{
				string inputField =
					ExpressionUtils.GetExpression(userAttribute, out string resultField);

				string unqualifiedInputField =
					GetUnqualifiedFieldName(_sourceTable, inputField);

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

			if (!ExcludeBaseRowsField)
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

				var expressionTokens = ExpressionUtils.GetExpressionTokens(sourceField).ToList();

				if (expressionTokens.Count > 1)
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
			Assert.True(sourceIdx >= 0 && sourceIdx < sourceFields.FieldCount, $"Invalid source index: {sourceIdx}");
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

		private static bool ValidateField(IReadOnlyTable sourceTable, string fieldName,
		                                  out string message)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				message = $"Null or empty field name defined for {sourceTable.Name}";
				return false;
			}

			string unqualifiedField = GetUnqualifiedFieldName(sourceTable, fieldName);

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

		private static string GetUnqualifiedFieldName(IReadOnlyTable sourceTable, string fieldName)
		{
			// Repeating the name of the current table is not necessary for single tables but might be done for clarity

			if (fieldName.StartsWith(sourceTable.Name, StringComparison.InvariantCultureIgnoreCase))
			{
				string remainder = fieldName.Substring(sourceTable.Name.Length);

				return remainder.Length > 1 ? remainder.Substring(1) : null;
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
	}
}
