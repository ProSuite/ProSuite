using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	/// <summary>
	///     Represents a lightweight reference to a geodatabase object.
	/// </summary>
	[CLSCompliant(false)]
	public struct GdbRowReference : IEquatable<GdbRowReference>
	{
		public GdbRowReference([NotNull] Row row)
		{
			using (Table table = row.GetTable())
			{
				TableId = table.GetID();
				TableName = table.GetName();
				WorkspaceReference = new GdbWorkspaceReference(table.GetDatastore());
			}

			ObjectId = row.GetObjectID();
		}

		public long ObjectId { get; }

		public long TableId { get; }

		[NotNull]
		public string TableName { get; }

		public GdbWorkspaceReference WorkspaceReference { get; }

		[Pure]
		[CanBeNull]
		public Row GetRow([NotNull] Geodatabase geodatabase)
		{
			Assert.ArgumentNotNull(geodatabase, nameof(geodatabase));

			using (var table = geodatabase.OpenDataset<Table>(TableName))
			{
				return GdbRowUtils.GetRow(table, ObjectId);
			}
		}

		[Pure]
		[CanBeNull]
		public Row GetRow()
		{
			using (Geodatabase geodatabase = WorkspaceReference.OpenGeodatabase())
			{
				return GetRow(geodatabase);
			}
		}

		[Pure]
		public bool References([NotNull] Row row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			return Equals(new GdbRowReference(row));
		}

		[Pure]
		public bool References([NotNull] Table table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var other = new GdbWorkspaceReference(table.GetDatastore());
			return Equals(WorkspaceReference, other);
		}

		public override string ToString()
		{
			return $"tableId={TableId} tableName={TableName} oid={ObjectId}";
		}

		#region IEquatable<GdbRowReference> implementation

		#endregion

		public bool Equals(GdbRowReference other)
		{
			return ObjectId == other.ObjectId && TableId == other.TableId &&
			       string.Equals(TableName, other.TableName) &&
			       WorkspaceReference.Equals(other.WorkspaceReference);
		}

		public override bool Equals(object obj)
		{
			return obj is GdbRowReference other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = ObjectId.GetHashCode();
				hashCode = (hashCode * 397) ^ TableId.GetHashCode();
				hashCode = (hashCode * 397) ^ TableName.GetHashCode();
				hashCode = (hashCode * 397) ^ WorkspaceReference.GetHashCode();
				return hashCode;
			}
		}
	}
}
