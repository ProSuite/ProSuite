using System;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public abstract class SourceClass : ISourceClass
	{
		private readonly GdbTableIdentity _identity;

		protected SourceClass(GdbTableIdentity identity,
		                      IAttributeReader attributeReader)
		{
			_identity = identity;
			AttributeReader = attributeReader;
		}

		protected GdbTableIdentity Identity => _identity;

		public bool HasGeometry => _identity.HasGeometry;

		public long ArcGISTableId => _identity.Id;

		[NotNull]
		public string Name => _identity.Name;

		public IAttributeReader AttributeReader { get; set; }

		public string DefinitionQuery { get; protected set; }

		public bool Uses(ITableReference tableReference)
		{
			return tableReference.ReferencesTable(_identity.Id, _identity.Name);
		}

		public T OpenDataset<T>() where T : Table
		{
			GdbWorkspaceIdentity workspaceIdentity = _identity.Workspace;

			using (Datastore datastore = workspaceIdentity.OpenDatastore())
			{
				if (datastore is Geodatabase geodatabase)
				{
					return geodatabase.OpenDataset<T>(_identity.Name);
				}

				if (datastore is FileSystemDatastore fsDatastore)
				{
					return fsDatastore.OpenDataset<T>(_identity.Name);
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
