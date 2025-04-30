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

		[NotNull]
		public string Name => _tableIdentity.Name;

		[CanBeNull]
		public IAttributeReader AttributeReader { get; set; }

		public string DefinitionQuery { get; protected set; }

		public string GetRelevantSubFields(bool excludeGeometry = false)
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

		public QueryFilter EnsureValidFilter([CanBeNull] QueryFilter filter, bool excludeGeometry)
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
				// todo: (daro) drop!
				Assert.False(subfields.Contains("SHAPE"), "Should not containe shape field");
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

			return result;
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

		public string CreateWhereClause(WorkItemStatus? statusFilter)
		{
			return CreateWhereClauseCore(statusFilter);
		}

		public abstract long GetUniqueTableId();

		protected virtual string CreateWhereClauseCore(WorkItemStatus? statusFilter)
		{
			return null;
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(DefinitionQuery) ? Name : $"{Name}, {DefinitionQuery}";
		}
	}
}
