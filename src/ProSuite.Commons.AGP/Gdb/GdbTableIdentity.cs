using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	// todo: daro rename to TableProxy?
	public readonly struct GdbTableIdentity : IEquatable<GdbTableIdentity>, IComparable<GdbTableIdentity>,
	                                          ITableReference
	{
		// TODO make static factory method FromTable(table) -- the ctor(table) suggests we hold on the table, which we don't
		public GdbTableIdentity(Table table)
		{
			Name = table.GetName();
			Id = table.GetID();

			HasGeometry = table is FeatureClass;

			using (Datastore datastore = table.GetDatastore())
			{
				Workspace = new GdbWorkspaceIdentity(datastore);
			}
		}

		public bool HasGeometry { get; }

		public GdbTableIdentity(string name, long id, GdbWorkspaceIdentity workspace)
		{
			Name = name;
			Id = id;
			Workspace = workspace;
			HasGeometry = default;
		}

		[NotNull]
		public string Name { get; }

		public long Id { get; }

		public GdbWorkspaceIdentity Workspace { get; }

		public override string ToString()
		{
			return $"tableId={Id} tableName={Name}";
		}

		#region ITableReference implemetation

		public bool ReferencesTable(Table table)
		{
			using (Datastore datastore = table.GetDatastore())
			{
				if (! Workspace.References(datastore))
				{
					return false;
				}

				long tableId = table.GetID();
				string tableName = table.GetName();

				return ReferencesTable(tableId, tableName);
			}
		}

		public bool ReferencesTable(long otherTableId, string otherTableName)
		{
			if (Id >= 0 && otherTableId >= 0)
			{
				return Id == otherTableId;
			}

			Assert.NotNullOrEmpty(otherTableName,
			                      "For negative table IDs the table name must be provided.");

			return Name.Equals(otherTableName);
		}

		public IDatastoreReference DatastoreReference => Workspace;

		#endregion

		#region IEquatable<GdbTableIdentity> implementation

		public bool Equals(GdbTableIdentity other)
		{
			return ReferencesTable(other.Id, other.Name) &&
			       Workspace.Equals(other.Workspace);
		}

		public override bool Equals(object obj)
		{
			return obj is GdbTableIdentity other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Name != null ? Name.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ Id.GetHashCode();
				hashCode = (hashCode * 397) ^ Workspace.GetHashCode();
				return hashCode;
			}
		}

		#endregion

		public int CompareTo(GdbTableIdentity other)
		{
			int workspaceComparison = Workspace.CompareTo(other.Workspace);

			if (workspaceComparison != 0)
			{
				return workspaceComparison;
			}

			int nameComparison =
				string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
			if (nameComparison != 0)
			{
				return nameComparison;
			}

			return Id.CompareTo(other.Id);
		}
	}
}
