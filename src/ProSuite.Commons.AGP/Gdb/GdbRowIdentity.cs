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
	public readonly struct GdbRowIdentity : IEquatable<GdbRowIdentity>, IComparable<GdbRowIdentity>,
	                                        IRowReference
	{
		public GdbRowIdentity([NotNull] Row row)
		{
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

		#region Implementation of IRowReference

		public bool References(Row row)
		{
			return ObjectId == row.GetObjectID() &&
			       Table.ReferencesTable(row.GetTable());
		}

		public bool References(Table table, long objectId)
		{
			return ObjectId == objectId &&
			       Table.ReferencesTable(table);
		}

		public ITableReference TableReference => Table;

		#endregion

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
