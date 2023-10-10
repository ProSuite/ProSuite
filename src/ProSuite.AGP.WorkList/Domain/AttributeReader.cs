using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: extract super class?

	public class AttributeReader : IAttributeReader
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IDictionary<Attributes, int> _fieldIndexByAttribute =
			new Dictionary<Attributes, int>();

		[NotNull] private readonly IDictionary<Attributes, string> _fieldNameByIssueAttribute =
			new Dictionary<Attributes, string>();

		[NotNull] private readonly Dictionary<string, int> _fieldIndexByName =
			new Dictionary<string, int>();

		public AttributeReader(TableDefinition definition, params Attributes[] attributes)
		{
			// todo daro: add all
			// todo daro: does FindField works with qualified field names?

			// todo daro: remove, not needed anymore
			//_fieldNameByIssueAttribute.Add(Attributes.ObjectID, definition.GetObjectIDField());

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
			_fieldNameByIssueAttribute.Add(Attributes.QualityConditionVersionUuid,
			                               "QualityConditionVersionUuid");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionStatus, "ExceptionStatus");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionNotes, "ExceptionNotes");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionCategory, "ExceptionCategory");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionOrigin, "ExceptionOrigin");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionDefinedDate, "ExceptionDefinedDate");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionLastRevisionDate,
			                               "ExceptionLastRevisionDate");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionRetirementDate,
			                               "ExceptionRetirementDate");
			_fieldNameByIssueAttribute.Add(Attributes.ExceptionShapeMatchCriterion,
			                               "ExceptionShapeMatchCriterion");
			_fieldNameByIssueAttribute.Add(Attributes.IssueDescription, "Description");

			foreach (Attributes attribute in attributes)
			{
				string fieldName = GetName(attribute);

				int index = -1;
				if (fieldName != null)
				{
					index = definition.FindField(fieldName);
				}
				else
				{
					_msg.Debug($"Field {attribute} is not registered");
				}

				if (fieldName != null && index > 0)
				{
					_fieldIndexByAttribute.Add(attribute, index);
					_fieldIndexByName.Add(fieldName, index);
				}
				else
				{
					_msg.Debug($"Cannot find field {fieldName}");
				}
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
			if (! _fieldIndexByAttribute.TryGetValue(attribute, out int fieldIndex))
			{
				return default;
			}

			object value = row[fieldIndex];

			return value == null ? default : (T) value;
		}

		public AttributeReader AddValue(Dictionary<string, object> attributes,
		                                object value,
		                                Attributes attribute)
		{
			Assert.ArgumentNotNull(attributes, nameof(attributes));
			Assert.ArgumentNotNull(value, nameof(value));

			var fieldName = attribute.ToString();

			Assert.True(_fieldIndexByAttribute.ContainsKey(attribute),
			            $"No field index for attribute {fieldName}");

			Assert.True(_fieldNameByIssueAttribute.ContainsKey(attribute),
			            $"No field name for attribute {fieldName}");

			string upperCaseFieldName = fieldName.ToUpper();

			Assert.True(_fieldIndexByName.ContainsKey(upperCaseFieldName),
			            $"No field index for field name {upperCaseFieldName}");

			Assert.False(attributes.ContainsKey(upperCaseFieldName),
			             $"Field {upperCaseFieldName} already added to attributes dictionary");

			attributes.Add(upperCaseFieldName, value);

			return this;
		}
	}
}
