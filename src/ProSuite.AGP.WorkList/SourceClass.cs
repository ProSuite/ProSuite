using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.AGP.WorkList
{
	public abstract class SourceClass : ISourceClass
	{
		private readonly GdbTableIdentity _tableIdentity;
		[NotNull] private readonly string _oidField;
		[CanBeNull] private readonly string _shapeField;

		protected SourceClass(GdbTableIdentity tableIdentity,
		                      [NotNull] SourceClassSchema schema,
		                      IAttributeReader attributeReader = null)
		{
			_oidField = schema.OIDField;
			_shapeField = schema.ShapeField;

			_tableIdentity = tableIdentity;
			AttributeReader = attributeReader;
		}

		public GdbTableIdentity TableIdentity => _tableIdentity;

		public bool HasGeometry => _tableIdentity.HasGeometry;

		public long ArcGISTableId => _tableIdentity.Id;

		public string Name => _tableIdentity.Name;

		public IAttributeReader AttributeReader { get; set; }

		public string DefaultDefinitionQuery { get; protected set; }

		private string GetRelevantSubFields(bool excludeGeometry = false)
		{
			string subFields = $"{_oidField}";

			if (HasGeometry && ! excludeGeometry)
			{
				Assert.NotNullOrEmpty(_shapeField);
				subFields = $"{subFields},{_shapeField}";
			}

			return GetRelevantSubFieldsCore(subFields);
		}

		protected virtual string GetRelevantSubFieldsCore(string subFields)
		{
			return subFields;
		}

		/// <summary>
		/// Ensures the filter is valid with the correct subfields. This method is called by Pro and we cannot
		/// control whether it's called with a SpatialQueryFilter or a QueryFilter. Querying a table with a
		/// spatial filter throws an exception, so we clone the filter to the correct type.
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="statusFilter"></param>
		/// <param name="excludeGeometry"></param>
		public void EnsureValidFilter([CanBeNull] ref QueryFilter filter,
		                              WorkItemStatus? statusFilter,
		                              bool excludeGeometry)
		{
			QueryFilter result;

			string relevantSubFields = GetRelevantSubFields(excludeGeometry);

			List<string> subfields =
				StringUtils.SplitAndTrim(relevantSubFields, ",");

			// safety net
			if (HasGeometry)
			{
				result = GdbQueryUtils.CloneFilter<SpatialQueryFilter>(filter);
			}
			else
			{
				Assert.False(subfields.Contains("SHAPE"), "Should not contain shape field");
				result = GdbQueryUtils.CloneFilter<QueryFilter>(filter);
			}

			if (GdbQueryUtils.EnsureSubFields(subfields, result.SubFields, out string newSubFields))
			{
				result.SubFields = newSubFields;
			}
			else
			{
				// filter.Subfields.Equals("*")
				result.SubFields = relevantSubFields;
			}

			EnsureValidFilterCore(ref result, statusFilter);
			filter = result;
		}

		public bool Uses(ITableReference tableReference)
		{
			return tableReference.ReferencesTable(_tableIdentity.Id, _tableIdentity.Name);
		}

		public T OpenDataset<T>() where T : Table
		{
			Datastore datastore = TableIdentity.Workspace.OpenDatastore();

			if (datastore is Geodatabase geodatabase)
			{
				return geodatabase.OpenDataset<T>(_tableIdentity.Name);
			}

			if (datastore is FileSystemDatastore fsDatastore)
			{
				return fsDatastore.OpenDataset<T>(_tableIdentity.Name);
			}

			if (datastore is PluginDatastore plugin)
			{
				return (T) plugin.OpenTable(_tableIdentity.Name);
			}

			throw new NotSupportedException(
				$"Datastore {datastore} type is not supported ");
		}

		public abstract long GetUniqueTableId();

		public virtual string CreateWhereClause(WorkItemStatus? statusFilter)
		{
			return null;
		}

		protected virtual void EnsureValidFilterCore(ref QueryFilter filter,
		                                             WorkItemStatus? statusFilter) { }

		public override string ToString()
		{
			return string.IsNullOrEmpty(DefaultDefinitionQuery) ? Name : $"{Name}, {DefaultDefinitionQuery}";
		}
	}
}
