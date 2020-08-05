using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	// todo daro: rename GdbRowIdentity
	/// <summary>
	///     Represents a lightweight reference to a geodatabase object.
	/// </summary>
	public struct GdbRowReference : IEquatable<GdbRowReference>
	{
		public GdbRowReference([NotNull] Row row)
		{
			// todo daro: GetTable() might be a performance issue?
			using (Table table = row.GetTable())
			{
				Table = new GdbTableReference(table);
			}

			ObjectId = row.GetObjectID();
		}

		public GdbRowReference(long objectId, long tableId, [NotNull] string tableName,
		                       GdbWorkspaceReference workspaceReference = default)
		{
			ObjectId = objectId;
			Table = new GdbTableReference(tableName, tableId, workspaceReference);
		}

		public long ObjectId { get; }

		public GdbTableReference Table { get; }

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

		[Pure]
		[CanBeNull]
		public Row GetRow()
		{
			using (Geodatabase geodatabase = Table.WorkspaceReference.OpenGeodatabase())
			{
				return GetRow(geodatabase);
			}
		}

		//[Pure]
		//public bool References([NotNull] Row row)
		//{
		//	Assert.ArgumentNotNull(row, nameof(row));

		//	return Equals(new GdbRowReference(row));
		//}

		//[Pure]
		//public bool References([NotNull] Table table)
		//{
		//	Assert.ArgumentNotNull(table, nameof(table));

		//	var other = new GdbWorkspaceReference(table.GetDatastore());
		//	return Equals(WorkspaceReference, other);
		//}

		public override string ToString()
		{
			return $"tableId={Table.Id} tableName={Table.Name} oid={ObjectId}";
		}

		#region IEquatable<GdbRowReference> implementation

		public bool Equals(GdbRowReference other)
		{
			return ObjectId == other.ObjectId && Table.Equals(other.Table);
		}

		public override bool Equals(object obj)
		{
			return obj is GdbRowReference other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (ObjectId.GetHashCode() * 397) ^ Table.GetHashCode();
			}
		}

		#endregion
	}
}
