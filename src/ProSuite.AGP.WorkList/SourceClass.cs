using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public abstract class SourceClass : ISourceClass
	{
		private readonly GdbTableIdentity _tableIdentity;

		protected SourceClass(GdbTableIdentity tableIdentity,
		                      IAttributeReader attributeReader)
		{
			_tableIdentity = tableIdentity;
			AttributeReader = attributeReader;
		}

		public GdbTableIdentity TableIdentity => _tableIdentity;

		public bool HasGeometry => _tableIdentity.HasGeometry;

		public long ArcGISTableId => _tableIdentity.Id;

		[NotNull]
		public string Name => _tableIdentity.Name;

		public IAttributeReader AttributeReader { get; set; }

		public string DefinitionQuery { get; protected set; }

		public bool Uses(ITableReference tableReference)
		{
			return tableReference.ReferencesTable(_tableIdentity.Id, _tableIdentity.Name);
		}

		public T OpenDataset<T>() where T : Table
		{
			GdbWorkspaceIdentity workspaceIdentity = _tableIdentity.Workspace;

			using (Datastore datastore = workspaceIdentity.OpenDatastore())
			{
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
					$"Datastore type is not supported ({workspaceIdentity})");
			}
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
	}
}
