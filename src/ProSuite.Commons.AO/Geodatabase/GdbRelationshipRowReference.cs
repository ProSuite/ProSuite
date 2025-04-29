using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Represents a lightweight reference to a geodatabase relationship row that represents an m:n relationship.
	/// Only m:n relationshp (attributed relationship classes) are supported. GdbObjectReference cannot be used
	/// for attributed relationships because their IObjectClass implementation returns a ObjectClassID of -1.
	/// </summary>
	public struct GdbRelationshipRowReference : IEquatable<GdbRelationshipRowReference>
	{
		#region Constructors

		public GdbRelationshipRowReference([NotNull] IObject obj)
		{
			IObjectClass objClass = obj.Class;

			Assert.AreEqual(objClass.ObjectClassID, -1,
			                "Object belongs to 'normal' object class and not to an m:n relationship.");

			var relClass = objClass as IRelationshipClass;

			Assert.NotNull(relClass, "Object does not belong to a relationship class.");

			RelationshipClassId = relClass.RelationshipClassID;
			ObjectId = obj.OID;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GdbRelationshipRowReference"/> class.
		/// </summary>
		/// <param name="relationhipClassId">The relationship class id.</param>
		/// <param name="objectId">The object id (OID field value).</param>
		public GdbRelationshipRowReference(int relationhipClassId, long objectId)
		{
			RelationshipClassId = relationhipClassId;
			ObjectId = objectId;
		}

		#endregion

		/// <summary>
		/// Gets the relationship class id of the referenced object.
		/// </summary>
		/// <value>The class id.</value>
		public int RelationshipClassId { get; }

		/// <summary>
		/// Gets the object id of the referenced object.
		/// </summary>
		/// <value>The object id.</value>
		public long ObjectId { get; }

		/// <summary>
		/// Safe way of getting the legacy OID for the 10.x platform. This method must not be used in 11.x
		/// </summary>
		/// <returns></returns>
		public int ObjectId10 => Convert.ToInt32(ObjectId);

		/// <summary>
		/// Gets the referenced object, from a workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <returns></returns>
		[Pure]
		public IObject GetObject([NotNull] IFeatureWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			IObjectClass objectClass = DatasetUtils.OpenObjectClass(workspace,
				RelationshipClassId);

			// TODO consider using ITable.GetRow(oid) and handling exception if not found
			// faster?
			return GdbQueryUtils.GetObject(objectClass, ObjectId);
		}

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

			// make sure it is a relationship class and not just a table
			if (objectClass.ObjectClassID != -1)
			{
				return false;
			}

			var relClass = objectClass as IRelationshipClass;

			if (relClass == null)
			{
				return false;
			}

			return RelationshipClassId == relClass.RelationshipClassID;
		}

		/// <summary>
		/// Returns a value indicating if the reference points to a row in a given table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns><c>true</c> if the reference points to an object in the given table, 
		/// <c>false</c> otherwise.</returns>
		/// <remarks>Only considers table object class id. Disregards difference in version.</remarks>
		[Pure]
		public bool References(ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var objectClass = table as IObjectClass;

			return objectClass != null && References(objectClass);
		}

		public bool Equals(GdbRelationshipRowReference other)
		{
			return ObjectId == other.ObjectId &&
			       RelationshipClassId == other.RelationshipClassId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return obj is GdbRelationshipRowReference &&
			       Equals((GdbRelationshipRowReference) obj);
		}

		public override int GetHashCode()
		{
			return RelationshipClassId + 29 * ObjectId.GetHashCode();
		}

		public override string ToString()
		{
			return $"RelationshipClassId={RelationshipClassId} oid={ObjectId}";
		}
	}
}
