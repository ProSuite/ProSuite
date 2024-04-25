using System;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public abstract class SourceClass : ISourceClass
	{
		private GdbTableIdentity _identity;

		protected SourceClass(GdbTableIdentity identity,
		                      IAttributeReader attributeReader)
		{
			_identity = identity;
			AttributeReader = attributeReader;
		}

		public bool HasGeometry => _identity.HasGeometry;

		public long Id => _identity.Id;

		[NotNull]
		public string Name => _identity.Name;

		public IAttributeReader AttributeReader { get; set; }

		public string DefinitionQuery { get; protected set; }

		public bool Uses(GdbTableIdentity table)
		{
			return _identity.Equals(table);
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

		protected virtual string CreateWhereClauseCore(WorkItemStatus? statusFilter)
		{
			return null;
		}
	}
}
