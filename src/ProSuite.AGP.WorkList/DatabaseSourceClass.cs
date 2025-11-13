using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public class DatabaseSourceClass : SourceClass
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly int _statusFieldIndex;

		[NotNull] private readonly List<WorkListFilterDefinitionExpression> _expressions = new();

		public DatabaseSourceClass(
			GdbTableIdentity tableIdentity,
			[NotNull] DbSourceClassSchema schema,
			[CanBeNull] IAttributeReader attributeReader,
			[CanBeNull] string definitionQuery,
			WorkspaceDbType dbType = WorkspaceDbType.Unknown)
			: base(tableIdentity, schema, attributeReader)
		{
			Assert.ArgumentNotNull(schema, nameof(schema));

			_statusFieldIndex = schema.StatusFieldIndex;
			StatusField = schema.StatusField;
			TodoValue = schema.TodoValue;
			DoneValue = schema.DoneValue;

			DefaultDefinitionQuery = definitionQuery;

			WorkspaceDbType = dbType;
		}

		public WorkItemStatus GetStatus([NotNull] Row row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			try
			{
				object value = row[_statusFieldIndex];

				return GetStatus(value);
			}
			catch (Exception e)
			{
				_msg.Error($"Error get value from row {row} with index {_statusFieldIndex}",
				           e);

				return WorkItemStatus.Todo;
			}
		}

		public WorkItemStatus GetStatus([CanBeNull] object value)
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

		protected override void EnsureValidFilterCore(ref QueryFilter filter,
		                                              WorkItemStatus? statusFilter)
		{
			string result = string.Empty;

			if (statusFilter != null)
			{
				object value = GetValue(statusFilter.Value);

				result = value.Equals(TodoValue)
					         ? $"({StatusField} = {value} OR {StatusField} IS NULL)"
					         : $"{StatusField} = {value}";
			}

			if (DefaultDefinitionQuery == null)
			{
				return;
			}

			if (! string.IsNullOrEmpty(result))
			{
				result += " AND ";
			}

			result += DefaultDefinitionQuery;

			filter.WhereClause = result;
		}

		protected override string GetRelevantSubFieldsCore(string subFields)
		{
			return string.IsNullOrEmpty(StatusField)
				       ? subFields
				       : $"{subFields},{StatusField}";
		}
	}
}
