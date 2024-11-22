using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	/// <summary>
	///     Represents a lightweight reference to a geodatabase object.
	/// </summary>
	public struct GdbRowIdentity : IEquatable<GdbRowIdentity>, IComparable<GdbRowIdentity>
	{
		public GdbRowIdentity([NotNull] Row row)
		{
			// todo daro: GetTable() might be a performance issue?
			using (Table table = row.GetTable())
			{
				Table = new GdbTableIdentity(table);
			}

			ObjectId = row.GetObjectID();
		}

		public GdbRowIdentity(long objectId, long tableId, [NotNull] string tableName,
		                      GdbWorkspaceIdentity workspaceIdentity) : this(
			objectId, new GdbTableIdentity(tableName, tableId, workspaceIdentity))
		{
			ObjectId = objectId;
			Table = new GdbTableIdentity(tableName, tableId, workspaceIdentity);
		}

		public GdbRowIdentity(long objectId, GdbTableIdentity tableId)
		{
			ObjectId = objectId;
			Table = tableId;
		}

		public long ObjectId { get; }

		public GdbTableIdentity Table { get; }

		[Pure]
		[CanBeNull]
		public Row GetRow([NotNull] Geodatabase geodatabase)
		{
			Assert.ArgumentNotNull(geodatabase, nameof(geodatabase));

			using (var table = geodatabase.OpenDataset<Table>(Table.Name))
			{
				return GdbQueryUtils.GetRow(table, ObjectId);
			}
		}

		[Obsolete("Only used in unit test")]
		[Pure]
		[CanBeNull]
		public Row GetRow()
		{
			using (Geodatabase geodatabase = Table.Workspace.OpenGeodatabase())
			{
				return GetRow(geodatabase);
			}
		}

		//[Pure]
		//public bool References([NotNull] Row row)
		//{
		//	Assert.ArgumentNotNull(row, nameof(row));

		//	return Equals(new GdbRowIdentity(row));
		//}

		//[Pure]
		//public bool References([NotNull] Table table)
		//{
		//	Assert.ArgumentNotNull(table, nameof(table));

		//	var other = new GdbWorkspaceIdentity(table.GetDatastore());
		//	return Equals(Workspace, other);
		//}

		public override string ToString()
		{
			return $"tableId={Table.Id} tableName={Table.Name} oid={ObjectId}";
		}

		#region IEquatable<GdbRowIdentity> implementation

		public bool Equals(GdbRowIdentity other)
		{
			return ObjectId == other.ObjectId && Table.Equals(other.Table);
		}

		public override bool Equals(object obj)
		{
			return obj is GdbRowIdentity other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (ObjectId.GetHashCode() * 397) ^ Table.GetHashCode();
			}
		}

		#endregion

		public int CompareTo(GdbRowIdentity other)
		{
			int oidComparison = ObjectId.CompareTo(other.ObjectId);
			if (oidComparison != 0)
				return oidComparison;

			return Table.CompareTo(other.Table);
		}
	}
}
