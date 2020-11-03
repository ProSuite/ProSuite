using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: extract super class?

	public class AttributeReader : IAttributeReader
	{
		[NotNull] private readonly IDictionary<Attributes, int> _fieldIndexByAttribute =
			new Dictionary<Attributes, int>();

		[NotNull] private readonly IDictionary<Attributes, string> _fieldNameByIssueAttribute =
			new Dictionary<Attributes, string>();

		public AttributeReader(TableDefinition definition, params Attributes[] attributes)
		{
			// todo daro: add all
			// todo daro: does FindField works with qualified field names?
			_fieldNameByIssueAttribute.Add(Attributes.ObjectID, definition.GetObjectIDField());

			_fieldNameByIssueAttribute.Add(Attributes.IssueCode, "Code");
			_fieldNameByIssueAttribute.Add(Attributes.IssueCodeDescription, "CodeDescription");
			_fieldNameByIssueAttribute.Add(Attributes.InvolvedObjects, "InvolvedObjects");
			_fieldNameByIssueAttribute.Add(Attributes.QualityConditionName, "QualityCondition");
			_fieldNameByIssueAttribute.Add(Attributes.TestName, "TestName");
			_fieldNameByIssueAttribute.Add(Attributes.TestDescription, "TestDescription");
			_fieldNameByIssueAttribute.Add(Attributes.TestType, "TestType");
			_fieldNameByIssueAttribute.Add(Attributes.IssueSeverity, "IssueType");
			_fieldNameByIssueAttribute.Add(Attributes.IsStopCondition, "StopCondition");
			_fieldNameByIssueAttribute.Add(Attributes.Category, "Category");
			_fieldNameByIssueAttribute.Add(Attributes.AffectedComponent, "AffectedComponent");
			_fieldNameByIssueAttribute.Add(Attributes.Url, "Url");
			_fieldNameByIssueAttribute.Add(Attributes.DoubleValue1, "DblValue1");
			_fieldNameByIssueAttribute.Add(Attributes.DoubleValue2, "DblValue2");
			_fieldNameByIssueAttribute.Add(Attributes.TextValue, "TextValue");
			_fieldNameByIssueAttribute.Add(Attributes.IssueAssignment, "IssueAssignment");
			_fieldNameByIssueAttribute.Add(Attributes.QualityConditionUuid, "QualityConditionUuid");
			_fieldNameByIssueAttribute.Add(Attributes.QualityConditionVersionUuid, "QualityConditionVersionUuid");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionStatus, "ExceptionStatus");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionNotes, "ExceptionNotes");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionCategory, "ExceptionCategory");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionOrigin, "ExceptionOrigin");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionDefinedDate, "ExceptionDefinedDate");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionLastRevisionDate, "ExceptionLastRevisionDate");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionRetirementDate, "ExceptionRetirementDate");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionShapeMatchCriterion, "ExceptionShapeMatchCriterion");

			foreach (Attributes attribute in attributes)
			{
				// todo daro: inline
				int fieldIndex = definition.FindField(GetName(attribute));
				_fieldIndexByAttribute.Add(attribute, fieldIndex);
			}
		}

		[CanBeNull]
		private string GetName(Attributes attribute)
		{
			return _fieldNameByIssueAttribute.TryGetValue(attribute, out string fieldName)
				       ? fieldName
				       : null;
		}

		[CanBeNull]
		public T GetValue<T>([NotNull] Row row, Attributes attribute)
		{
			if (_fieldIndexByAttribute.TryGetValue(attribute, out int fieldIndex))
			{
				object value = row[fieldIndex];
				return (T) value;
			}

			return default;
		}
	}
}
