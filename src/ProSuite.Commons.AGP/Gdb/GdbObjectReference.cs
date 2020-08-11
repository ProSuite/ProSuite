using System;

namespace ProSuite.Commons.AG.GDB
{
	// todo daro: rename to GdbRowReference?
	/// <summary>
	///   Represents a lightweight reference to a geodatabase object.
	/// </summary>
	public struct GdbObjectReference : IEquatable<GdbObjectReference>
	{
		public GdbObjectReference(long objectId, string tableName, long tableId, GdbWorkspaceReference workspaceReference)
		{
			TableId = tableId;
			ObjectId = objectId;
			TableName = tableName;
			WorkspaceReference = workspaceReference;
		}
		
		public string TableName { get; }

		public long TableId { get; }

		public long ObjectId { get; }

		public GdbWorkspaceReference WorkspaceReference { get; }

		public override string ToString()
		{
			return $"classId={TableId} oid={ObjectId}";
		}

		#region IEquatable<GdbObjectReference> implementation

		public bool Equals(GdbObjectReference other)
		{
			return string.Equals(TableName, other.TableName) && TableId == other.TableId &&
			       ObjectId == other.ObjectId && WorkspaceReference.Equals(other.WorkspaceReference);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return obj is GdbObjectReference && Equals((GdbObjectReference) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (TableName != null ? TableName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ TableId.GetHashCode();
				hashCode = (hashCode * 397) ^ ObjectId.GetHashCode();
				hashCode = (hashCode * 397) ^ WorkspaceReference.GetHashCode();
				return hashCode;
			}
		}

		#endregion
	}
}
