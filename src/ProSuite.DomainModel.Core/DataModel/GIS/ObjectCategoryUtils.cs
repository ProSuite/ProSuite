using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase;

namespace ProSuite.DomainModel.Core.DataModel.GIS
{
	public static class ObjectCategoryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void ApplyObjectCategoryValuesTo(
			[NotNull] ObjectCategory objectCategory,
			[NotNull] IObject obj)
		{
			Assert.ArgumentNotNull(objectCategory, nameof(objectCategory));
			Assert.ArgumentNotNull(obj, nameof(obj));

			if (objectCategory is ObjectSubtype objectSubtype)
			{
				ApplyObjectSubtypeValuesTo(objectSubtype, obj);
			}
			else if (objectCategory is ObjectType objectType)
			{
				ApplyObjectTypeValueTo(objectType, obj);
			}
			else
			{
				throw new NotSupportedException($"Unhandled type: {objectCategory.GetType()}");
			}
		}

		/// <summary>
		/// Gets the object subtype that corresponds to a given object.
		/// </summary>
		/// <param name="objectType">The (known) object type.</param>
		/// <param name="fromObject">The object whose attribute values shall be analyzed.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"> The object type does not apply to the object.</exception>
		[CanBeNull]
		public static ObjectSubtype TryIdentifyObjectSubtype(
			[NotNull] ObjectType objectType,
			[NotNull] IObject fromObject)
		{
			Assert.ArgumentNotNull(objectType, nameof(objectType));
			Assert.ArgumentNotNull(fromObject, nameof(fromObject));

			int maxMatchingFieldCount = -1;
			ObjectSubtype bestMatchingObjectSubtype = null;

			if (! ObjectTypeAppliesTo(objectType, fromObject))
			{
				throw new ArgumentException(
					string.Format("Object type {0} does not apply to object {1}",
					              objectType, GdbObjectUtils.ToString(fromObject)));
			}

			foreach (ObjectSubtype objectSubtype in objectType.ObjectSubtypes)
			{
				if (! ObjectSubtypeAppliesTo(objectSubtype, fromObject))
				{
					continue;
				}

				int matchingFieldCount = objectSubtype.Criteria.Count;

				if (matchingFieldCount > maxMatchingFieldCount)
				{
					maxMatchingFieldCount = matchingFieldCount;
					bestMatchingObjectSubtype = objectSubtype;
				}
			}

			return bestMatchingObjectSubtype;
		}

		/// <summary>
		/// Returns a value indicating if this object type applies to a given object.
		/// </summary>
		/// <param name="objectType"></param>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		private static bool ObjectTypeAppliesTo([NotNull] ObjectType objectType,
		                                        [NotNull] IObject obj)
		{
			int subtypeFieldIndex = DatasetUtils.GetSubtypeFieldIndex(obj.Class);

			if (subtypeFieldIndex < 0)
			{
				return false;
			}

			object value = obj.get_Value(subtypeFieldIndex);

			if (value is DBNull || value == null)
			{
				return false;
			}

			return (int) value == objectType.SubtypeCode;
		}

		private static bool ObjectSubtypeAppliesTo(
			[NotNull] ObjectSubtype objectSubtype,
			[NotNull] IObject obj)
		{
			foreach (ObjectSubtypeCriterion criterion in objectSubtype.Criteria)
			{
				ObjectAttribute attribute = criterion.Attribute;

				int fieldIndex =
					AttributeUtils.GetFieldIndex(obj.Class, attribute);
				if (fieldIndex < 0)
				{
					// the criterion field is missing, no match
					return false;
				}

				object value = obj.get_Value(fieldIndex);
				if (! Equals(value, criterion.AttributeValue))
				{
					return false;
				}
			}

			return true;
		}

		private static void ApplyObjectTypeValueTo([NotNull] ObjectType objectType,
		                                           [NotNull] IObject obj)
		{
			int subtypeFieldIndex = DatasetUtils.GetSubtypeFieldIndex(obj.Class);

			if (subtypeFieldIndex < 0)
			{
				return;
			}

			object value = obj.get_Value(subtypeFieldIndex);

			if (value != null && ! (value is DBNull) && (int) value == objectType.SubtypeCode)
			{
				return;
			}

			_msg.DebugFormat("Changing subtype value to {0} (current value: {1})",
			                 objectType.SubtypeCode, value);

			obj.set_Value(subtypeFieldIndex, objectType.SubtypeCode);
		}

		private static void ApplyObjectSubtypeValuesTo([NotNull] ObjectSubtype objectSubtype,
		                                               [NotNull] IObject obj)
		{
			ApplyObjectTypeValueTo(objectSubtype.ObjectType, obj);

			foreach (ObjectSubtypeCriterion criterion in objectSubtype.Criteria)
			{
				ObjectAttribute attribute = criterion.Attribute;

				int fieldIndex =
					AttributeUtils.GetFieldIndex(obj.Class, attribute);
				Assert.True(fieldIndex >= 0,
				            "Object subtype criterion attribute not found in object class {0}: {1}",
				            DatasetUtils.GetName(obj.Class), attribute.Name);

				obj.set_Value(fieldIndex, criterion.AttributeValue);
			}
		}
	}
}
