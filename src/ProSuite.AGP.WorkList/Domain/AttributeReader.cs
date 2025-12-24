using System;
using System.Collections.Generic;
using System.Windows.Media;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.AGP.WorkList.Domain;

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

	public string GetName(Attributes attribute)
	{
		return _fieldNameByIssueAttribute.TryGetValue(attribute, out string fieldName)
			       ? fieldName
			       : null;
	}

	public void ReadAttributes(Row fromRow, IWorkItem item, ISourceClass source)
	{
		IIssueItem forItem = (IIssueItem) item;

		try
		{
			forItem.IssueCode = GetValue<string>(fromRow, Attributes.IssueCode);
			forItem.IssueCodeDescription =
				GetValue<string>(fromRow, Attributes.IssueCodeDescription);
			forItem.IssueDescription =
				GetValue<string>(fromRow, Attributes.IssueDescription);

			forItem.InvolvedObjects = GetValue<string>(fromRow, Attributes.InvolvedObjects);

			forItem.QualityCondition =
				GetValue<string>(fromRow, Attributes.QualityConditionName);
			forItem.TestName = GetValue<string>(fromRow, Attributes.TestName);
			forItem.TestDescription = GetValue<string>(fromRow, Attributes.TestDescription);
			forItem.TestType = GetValue<string>(fromRow, Attributes.TestType);
			forItem.IssueSeverity = GetValue<string>(fromRow, Attributes.IssueSeverity);
			forItem.StopCondition = GetValue<string>(fromRow, Attributes.IsStopCondition);
			forItem.Category = GetValue<string>(fromRow, Attributes.Category);
			forItem.AffectedComponent =
				GetValue<string>(fromRow, Attributes.AffectedComponent);
			forItem.Url = GetValue<string>(fromRow, Attributes.Url);
			forItem.DoubleValue1 = GetValue<double?>(fromRow, Attributes.DoubleValue1);
			forItem.DoubleValue2 = GetValue<double?>(fromRow, Attributes.DoubleValue2);
			forItem.TextValue = GetValue<string>(fromRow, Attributes.TextValue);
			forItem.IssueAssignment = GetValue<string>(fromRow, Attributes.IssueAssignment);
			forItem.QualityConditionUuid =
				GetValue<string>(fromRow, Attributes.QualityConditionUuid);
			forItem.QualityConditionVersionUuid =
				GetValue<string>(fromRow, Attributes.QualityConditionVersionUuid);
			forItem.ExceptionStatus = GetValue<string>(fromRow, Attributes.ExceptionStatus);
			forItem.ExceptionNotes = GetValue<string>(fromRow, Attributes.ExceptionNotes);
			forItem.ExceptionCategory =
				GetValue<string>(fromRow, Attributes.ExceptionCategory);
			forItem.ExceptionOrigin = GetValue<string>(fromRow, Attributes.ExceptionOrigin);
			forItem.ExceptionDefinedDate =
				GetValue<string>(fromRow, Attributes.ExceptionDefinedDate);
			forItem.ExceptionLastRevisionDate =
				GetValue<string>(fromRow, Attributes.ExceptionLastRevisionDate);
			forItem.ExceptionRetirementDate =
				GetValue<string>(fromRow, Attributes.ExceptionRetirementDate);
			forItem.ExceptionShapeMatchCriterion =
				GetValue<string>(fromRow, Attributes.ExceptionShapeMatchCriterion);
			forItem.Status = ((DatabaseSourceClass) source).GetStatus(fromRow);
		}
		catch (Exception e)
		{
			_msg.Warn($"An error occurred reading issue item properties from {forItem}", e);
		}

		if (forItem.InvolvedObjects != null)
		{
			try
			{
				// todo daro: use source class to determine whether involved tables have geoemtry?
				forItem.InvolvedTables =
					IssueUtils.ParseInvolvedTables(forItem.InvolvedObjects,
					                               source.HasGeometry);
			}
			catch (Exception e)
			{
				_msg.Warn(
					$"An error occurred parsing involved tables from issue item {forItem}", e);
			}
		}
	}

	public IList<InvolvedTable> ParseInvolved(string involvedString, bool hasGeometry)
	{
		return IssueUtils.ParseInvolvedTables(involvedString, hasGeometry);
	}

	[CanBeNull]
	public T GetValue<T>([NotNull] Row row, Attributes attribute)
	{
		if (! _fieldIndexByAttribute.TryGetValue(attribute, out int fieldIndex))
		{
			return default;
		}

		object value = row[fieldIndex];

		if (attribute == Attributes.IssueSeverity)
		{
			value = GetSeverity(row);
		}

		return value == null ? default : (T) value;
	}

	[NotNull]
	public Brush GetSeverityBackColor([CanBeNull] string severity)
	{
		if (Enum.TryParse(severity, out IssueType type))
		{
			switch (type)
			{
				case IssueType.Error:
					return Brushes.Salmon;
				case IssueType.Warning:
					return Brushes.Yellow;
				default:
					throw new ArgumentOutOfRangeException(
						$"Unexpected issue severity type: {type}");
			}
		}

		_msg.DebugFormat("Unexpected issue severity: {0}",
		                 string.IsNullOrEmpty(severity)
			                 ? "undefined"
			                 : severity);

		return Brushes.White;
	}

	[NotNull]
	public string GetSeverity(Row fromRow)
	{
		var attribute = Attributes.IssueSeverity;

		if (! _fieldIndexByAttribute.TryGetValue(attribute, out int fieldIndex))
		{
			_msg.Debug($"table does not contain {attribute} field");
			return "unkown";
		}

		object value = null;

		try
		{
			value = fromRow[fieldIndex];

			if (value is not string severity)
			{
				_msg.Debug($"Invalid issue type: {value}");
				return "unkown";
			}

			if (Enum.TryParse(severity, out IssueType type))
			{
				return type.ToString();
			}

			_msg.Debug($"Invalid issue type: {value}");
			return "unkown";
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
			return $"Invalid issue type: {value}";
		}
	}
}
