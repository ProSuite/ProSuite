using System;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public class DatabaseSourceClass : SourceClass
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly WorkListStatusSchema _statusSchema;

		public DatabaseSourceClass(GdbTableIdentity identity,
		                           [NotNull] WorkListStatusSchema statusSchema,
		                           [CanBeNull] IAttributeReader attributeReader,
		                           [CanBeNull] string definitionQuery)
			: base(identity, attributeReader)
		{
			Assert.ArgumentNotNull(statusSchema, nameof(statusSchema));

			_statusSchema = statusSchema;
			DefinitionQuery = definitionQuery;
		}

		[NotNull]
		public string StatusFieldName => _statusSchema.FieldName;

		public WorkItemStatus GetStatus([NotNull] Row row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			try
			{
				object value = row[_statusSchema.FieldIndex];

				return GetStatus(value);
			}
			catch (Exception e)
			{
				_msg.Error($"Error get value from row {row} with index {_statusSchema.FieldIndex}",
				           e);

				return WorkItemStatus.Todo;
			}
		}

		public WorkItemStatus GetStatus([CanBeNull] object value)
		{
			if (_statusSchema.TodoValue.Equals(value))
			{
				return WorkItemStatus.Todo;
			}

			if (_statusSchema.DoneValue.Equals(value))
			{
				return WorkItemStatus.Done;
			}

			_msg.VerboseDebug(() => $"Unknown {nameof(WorkItemStatus)} value {value}, " +
			                        $"return {nameof(WorkItemStatus.Todo)}");

			return WorkItemStatus.Todo;
		}

		public virtual object GetValue(WorkItemStatus status)
		{
			switch (status)
			{
				case WorkItemStatus.Done:
					return _statusSchema.DoneValue;

				case WorkItemStatus.Todo:
					return _statusSchema.TodoValue;

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
				result = $"{StatusFieldName} = {GetValue(statusFilter.Value)}";
			}

			if (DefinitionQuery != null)
			{
				if (! string.IsNullOrEmpty(result))
				{
					result += " AND ";
				}

				result += DefinitionQuery;
			}

			return result;
		}

		#endregion
	}
}
