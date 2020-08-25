using System;
using ArcGIS.Core.Data;

namespace ProSuite.Commons.AGP.Gdb
{
	// todo daro: rename to TableProxy?
	public struct GdbTableIdentity : IEquatable<GdbTableIdentity>
	{
		public GdbTableIdentity(Table table)
		{
			Name = table.GetName();
			Id = table.GetID();

			using (Datastore datastore = table.GetDatastore())
			{
				Workspace = new GdbWorkspaceIdentity(datastore);
			}
		}

		public GdbTableIdentity(string name, long id, GdbWorkspaceIdentity workspace)
		{
			Name = name;
			Id = id;
			Workspace = workspace;
		}

		public string Name { get; }

		public long Id { get; }

		public GdbWorkspaceIdentity Workspace { get; }

		public override string ToString()
		{
			return $"tableId={Id} tableName={Name}";
		}

		#region IEquatable<GdbTableIdentity> implementation

		public bool Equals(GdbTableIdentity other)
		{
			return string.Equals(Name, other.Name) && Id == other.Id &&
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
	}
}
