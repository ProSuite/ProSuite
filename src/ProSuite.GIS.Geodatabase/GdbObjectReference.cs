using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase
{
	/// <summary>
	/// Represents a lightweight reference to a geodatabase object.
	/// </summary>
	public readonly struct GdbObjectReference : IEquatable<GdbObjectReference>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="GdbObjectReference"/> struct.
		/// </summary>
		/// <param name="obj">The gdb object to create the reference for.</param>
		public GdbObjectReference([NotNull] IObject obj)
			: this(obj.Class.ObjectClassID, obj.OID) { }

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

		///// <summary>
		///// Gets the referenced object, from a workspace.
		///// </summary>
		///// <param name="workspace">The workspace.</param>
		///// <returns></returns>
		//[CanBeNull]
		//[Pure]
		//public IObject GetObject([NotNull] IFeatureWorkspace workspace)
		//{
		//	Assert.ArgumentNotNull(workspace, nameof(workspace));

		//	IObjectClass objectClass = DatasetUtils.OpenObjectClass(workspace, ClassId);

		//	return GdbQueryUtils.GetObject(objectClass, ObjectId);
		//}

		/// <summary>
		/// Returns a value indicating if the reference points to a given object.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns><c>true</c> if the reference points to the object, 
		/// <c>false</c> otherwise.</returns>
		/// <remarks>Only considers object class id and object id. 
		/// Disregards difference in version.</remarks>
		[Pure]
		public bool References([NotNull] IObject obj)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			return obj.OID == ObjectId && References(obj.Class);
		}

		/// <summary>
		/// Returns a value indicating if the reference points to an object in a given object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns><c>true</c> if the reference points to an object in the given class, 
		/// <c>false</c> otherwise.</returns>
		/// <remarks>Only considers object class id. Disregards difference in version.</remarks>
		[Pure]
		public bool References([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return ClassId == objectClass.ObjectClassID;
		}

		/// <summary>
		/// Returns a value indicating if the reference points to a row in a given table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns><c>true</c> if the reference points to an object in the given table, 
		/// <c>false</c> otherwise.</returns>
		/// <remarks>Only considers table object class id. Disregards difference in version.</remarks>
		[Pure]
		public bool References([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var objectClass = table as IObjectClass;

			return objectClass != null && References(objectClass);
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
				return (ClassId.GetHashCode() * 397) ^ ObjectId.GetHashCode();
			}
		}

		///<summary>
		/// Returns a <see cref="System.String"></see> that represents the 
		/// current <see cref="GdbObjectReference"></see>.
		///</summary>
		///
		///<returns>
		/// A <see cref="System.String"></see> that represents the 
		/// current <see cref="GdbObjectReference"></see>.
		///</returns>
		public override string ToString()
		{
			return string.Format("classId={0} oid={1}", ClassId, ObjectId);
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
}
