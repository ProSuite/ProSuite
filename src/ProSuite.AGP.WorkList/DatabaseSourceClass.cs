using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.AGP.WorkList;

public class DatabaseSourceClass : SourceClass
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly FilterHelper _filterHelper;

	[NotNull] private readonly List<WorkListFilterDefinitionExpression> _expressions = new();
	[NotNull] private readonly Dictionary<string, int> _subFields;
	[NotNull] private readonly string _subFieldNames;

	public DatabaseSourceClass(
		GdbTableIdentity tableIdentity,
		[NotNull] DbSourceClassSchema schema,
		[CanBeNull] IAttributeReader attributeReader,
		[CanBeNull] string definitionQuery,
		[NotNull] FilterHelper filterHelper,
		WorkspaceDbType dbType = WorkspaceDbType.Unknown)
		: base(tableIdentity, attributeReader)
	{
		Assert.ArgumentNotNull(schema, nameof(schema));

		_filterHelper = filterHelper;
		_subFields = schema.SubFields;
		_subFieldNames = StringUtils.Concatenate(schema.SubFields.Keys, ",");
		Assert.NotNullOrEmpty(_subFieldNames);

		StatusField = schema.StatusField;
		TodoValue = schema.TodoValue;
		DoneValue = schema.DoneValue;

		DefaultDefinitionQuery = definitionQuery;

		WorkspaceDbType = dbType;
	}

	public override bool Contains(Row row)
	{
		return _filterHelper.Check(row);
	}

	[CanBeNull]
	protected object GetValue([NotNull] Row row, [NotNull] string fieldName)
	{
		bool exists = _subFields.TryGetValue(fieldName, out int index);
		Assert.True(exists, $"{fieldName} is not a subfield");

		return row[index];
	}

	public WorkItemStatus GetStatus([NotNull] Row row)
	{
		Assert.ArgumentNotNull(row, nameof(row));

		try
		{
			object value = GetValue(row, StatusField);

			return GetStatus(value);
		}
		catch (Exception ex)
		{
			_msg.ErrorFormat("{0} error getting value from field {1}. {2}", StatusField,
			                 GdbObjectUtils.GetDisplayValue(row), ex.Message);

			return WorkItemStatus.Todo;
		}
	}

	private WorkItemStatus GetStatus([CanBeNull] object value)
	{
		if (TodoValue.Equals(value))
		{
			return WorkItemStatus.Todo;
		}

		if (DoneValue.Equals(value))
		{
			return WorkItemStatus.Done;
		}

		_msg.VerboseDebug(() => $"Unknown {nameof(WorkItemStatus)} value {value}, " +
		                        $"return {nameof(WorkItemStatus.Todo)}");

		return WorkItemStatus.Todo;
	}

	public object DoneValue { get; }

	public object TodoValue { get; }

	public string StatusField { get; }

	public WorkspaceDbType WorkspaceDbType { get; }

	public object GetValue(WorkItemStatus status)
	{
		switch (status)
		{
			case WorkItemStatus.Done:
				return DoneValue;

			case WorkItemStatus.Todo:
				return TodoValue;

			case WorkItemStatus.Unknown:
				return DBNull.Value;

			default:
				throw new ArgumentException(
					$@"Illegal status value: {status}", nameof(status));
		}
	}

	public void UpdateDefinitionFilterExpressions(
		[NotNull] IEnumerable<WorkListFilterDefinitionExpression> definitionExpressions)
	{
		Assert.ArgumentNotNull(definitionExpressions, nameof(definitionExpressions));

		foreach (WorkListFilterDefinitionExpression newExpression in definitionExpressions)
		{
			foreach (WorkListFilterDefinitionExpression expression in _expressions)
			{
				if (Equals(expression.FilterDefinition.Name,
				           newExpression.FilterDefinition.Name))
				{
					expression.Expression = newExpression.Expression;
					break;
				}
			}

			// If it was not equal to any expression, it is a new expression
			if (! _expressions.Contains(newExpression))
			{
				_expressions.Add(newExpression);
			}
		}
	}

	[CanBeNull]
	public WorkListFilterDefinitionExpression GetExpression(
		[CanBeNull] WorkListFilterDefinition definition)
	{
		return definition == null
			       ? null
			       : _expressions.FirstOrDefault(
				       exp => exp.FilterDefinition.Equals(definition));
	}

	public override long GetUniqueTableId()
	{
		// NOTE: Currently DatabaseSourceClasses are supposed to all reside in the same
		//       workspace (which is certainly the case for Issue Worklists).
		//       Therefore, we can use the table ID as a unique identifier.
		return ArcGISTableId;
	}

	public override T CreateWorkItem<T>(Row row)
	{
		var item = base.CreateWorkItem<T>(row);
		item.Status = GetStatus(row);
		return item;
	}

	protected override string GetRelevantSubFieldsCore()
	{
		return _subFieldNames;
	}
}
