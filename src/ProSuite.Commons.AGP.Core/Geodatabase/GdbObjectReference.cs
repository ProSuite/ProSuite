using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

/// <summary>
/// Represents a lightweight reference to a geodatabase object that can be
/// created without access to the actual object.
/// </summary>
public readonly struct GdbObjectReference : IEquatable<GdbObjectReference>, IRowReference
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="GdbObjectReference"/> struct.
	/// </summary>
	/// <param name="row">The gdb object to create the reference for.</param>
	public GdbObjectReference([NotNull] Row row)
		: this(row.GetTable().GetID(), row.GetObjectID()) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="GdbObjectReference"/> class.
	/// </summary>
	/// <param name="classId">The object class id.</param>
	/// <param name="objectId">The object id (OID field value).</param>
	public GdbObjectReference(long classId, long objectId)
	{
		ClassId = classId;
		ObjectId = objectId;
	}

	#endregion

	/// <summary>
	/// Gets the object class id of the referenced object.
	/// </summary>
	/// <value>The class id.</value>
	public long ClassId { get; }

	/// <summary>
	/// Gets the object id of the referenced object.
	/// </summary>
	/// <value>The object id.</value>
	public long ObjectId { get; }

	/// <summary>
	/// Returns a value indicating if the reference points to a given row.
	/// </summary>
	/// <param name="row">The row.</param>
	/// <returns><c>true</c> if the reference points to the object, 
	/// <c>false</c> otherwise.</returns>
	/// <remarks>Only considers the table id and object id. 
	/// Disregards difference in version.</remarks>
	[Pure]
	public bool References(Row row)
	{
		Assert.ArgumentNotNull(row, nameof(row));

		return row.GetObjectID() == ObjectId && ReferencesTable(row.GetTable());
	}

	/// <summary>
	/// Returns a value indicating if the reference points to a given row represented by the
	/// specified table and object id.
	/// </summary>
	/// <param name="table">The table.</param>
	/// <param name="objectId">The object id.</param>
	/// <returns><c>true</c> if the reference points to the object, 
	/// <c>false</c> otherwise.</returns>
	/// <remarks>Only considers the table id and object id. 
	/// Disregards difference in version.</remarks>
	public bool References(Table table, long objectId)
	{
		return objectId == ObjectId && ReferencesTable(table);
	}

	/// <summary>
	/// The table reference is not available for lightweight object references. All compared
	/// objects are expected to exist in the same workspace and have a unique table id that
	/// is sufficient for the comparison.
	/// </summary>
	public ITableReference TableReference => null;

	/// <summary>
	/// Returns a value indicating if the reference points to an object in a given object class.
	/// </summary>
	/// <param name="table">The object class.</param>
	/// <returns><c>true</c> if the reference points to an object in the given class, 
	/// <c>false</c> otherwise.</returns>
	/// <remarks>Only considers object class id. Disregards difference in version.</remarks>
	[Pure]
	public bool ReferencesTable([NotNull] Table table)
	{
		Assert.ArgumentNotNull(table, nameof(table));

		return ClassId == table.GetID();
	}

	#region Object overrides

	public override bool Equals(object obj)
	{
		if (! (obj is GdbObjectReference))
		{
			return false;
		}

		var gdbObjectReference = (GdbObjectReference) obj;
		return ClassId == gdbObjectReference.ClassId &&
		       ObjectId == gdbObjectReference.ObjectId;
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return ClassId.GetHashCode() * 397 ^ ObjectId.GetHashCode();
		}
	}

	///<summary>
	/// Returns a <see cref="string"></see> that represents the 
	/// current <see cref="GdbObjectReference"></see>.
	///</summary>
	///
	///<returns>
	/// A <see cref="string"></see> that represents the 
	/// current <see cref="GdbObjectReference"></see>.
	///</returns>
	public override string ToString()
	{
		return $"classId={ClassId} oid={ObjectId}";
	}

	#endregion

	#region IEquatable<GdbObjectReference> implementation

	///<summary>
	///Indicates whether the current object is equal to another object of the same type.
	///</summary>
	///<returns>
	///true if the current object is equal to the other parameter; otherwise, false.
	///</returns>
	///<param name="other">An object to compare with this object.</param>
	public bool Equals(GdbObjectReference other)
	{
		return ClassId == other.ClassId && ObjectId == other.ObjectId;
	}

	#endregion
}
