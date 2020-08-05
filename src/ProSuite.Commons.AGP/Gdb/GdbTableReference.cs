using System;
using ArcGIS.Core.Data;

namespace ProSuite.Commons.AGP.Gdb
{
	public struct GdbTableReference : IEquatable<GdbTableReference>
	{
		// todo daro: rename to *Description? GdbTableIdentity?
		public GdbTableReference(Table table)
		{
			Name = table.GetName();
			Id = table.GetID();

			using (Datastore datastore = table.GetDatastore())
			{
				WorkspaceReference = new GdbWorkspaceReference(datastore);
			}
		}

		public GdbTableReference(string name, long id, GdbWorkspaceReference workspaceReference)
		{
			Name = name;
			Id = id;
			WorkspaceReference = workspaceReference;
		}

		public string Name { get; }

		public long Id { get; }

		public GdbWorkspaceReference WorkspaceReference { get; }

		public override string ToString()
		{
			return $"tableId={Id} tableName={Name}";
		}

		#region IEquatable<GdbRowReference> implementation

		public bool Equals(GdbTableReference other)
		{
			return string.Equals(Name, other.Name) && Id == other.Id &&
			       WorkspaceReference.Equals(other.WorkspaceReference);
		}

		public override bool Equals(object obj)
		{
			return obj is GdbTableReference other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Name != null ? Name.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ Id.GetHashCode();
				hashCode = (hashCode * 397) ^ WorkspaceReference.GetHashCode();
				return hashCode;
			}
		}

		#endregion
	}
}
