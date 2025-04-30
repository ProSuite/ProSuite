using System;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	// TODO: rename DbSourceClass
	public class DatabaseSourceClass : SourceClass
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly int _statusFieldIndex;

		public DatabaseSourceClass(GdbTableIdentity tableIdentity,
		                           [NotNull] DbSourceClassSchema schema,
		                           [CanBeNull] IAttributeReader attributeReader,
		                           [CanBeNull] string definitionQuery)
			: base(tableIdentity, schema, attributeReader)
		{
			Assert.ArgumentNotNull(schema, nameof(schema));
			_statusFieldIndex = schema.StatusFieldIndex;
			StatusField = schema.StatusField;
			TodoValue = schema.TodoValue;
			DoneValue = schema.DoneValue;

			DefinitionQuery = definitionQuery;
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

		#region Overrides of SourceClass

		public override long GetUniqueTableId()
		{
			// NOTE: Currently DatabaseSourceClasses are supposed to all reside in the same
			//       workspace (which is certainly the case for Issue Worklists).
			//       Therefore, we can use the table ID as a unique identifier.
			return ArcGISTableId;
		}

		protected override string CreateWhereClauseCore(WorkItemStatus? statusFilter)
		{
			string result = string.Empty;

			if (statusFilter != null)
			{
				object value = GetValue(statusFilter.Value);

				result = value.Equals(TodoValue)
					         ? $"{StatusField} = {value} OR {StatusField} IS NULL"
					         : $"{StatusField} = {value}";
			}

			if (DefinitionQuery == null)
			{
				return result;
			}

			if (! string.IsNullOrEmpty(result))
			{
				result += " AND ";
			}

			result += DefinitionQuery;

			return result;
		}

		protected override string GetRelevantSubFieldsCore(string subFields)
		{
			return string.IsNullOrEmpty(StatusField)
				       ? subFields
				       : $"{subFields},{StatusField}";
		}

		#endregion
	}
}
