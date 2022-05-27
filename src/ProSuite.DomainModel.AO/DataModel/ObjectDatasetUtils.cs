using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DomainModel.AO.DataModel
{
	/// <summary>
	/// Provides ArcObjects-specific functionality for domain datasets
	/// </summary>
	public static class ObjectDatasetUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private const int _minimumExpectedOperatorFieldLength = 10;

		[NotNull]
		public static ObjectAttribute GetExistingAttribute([NotNull] IObjectDataset objectDataset,
		                                                   [NotNull] AttributeRole attributeRole)
		{
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));
			Assert.ArgumentNotNull(attributeRole, nameof(attributeRole));

			ObjectAttribute attribute = objectDataset.GetAttribute(attributeRole);

			if (attribute == null)
			{
				throw new ArgumentException(
					string.Format(
						"Attribute for role {0} not defined in dataset {1}",
						attributeRole, objectDataset));
			}

			return attribute;
		}

		[CanBeNull]
		public static ObjectCategory GetObjectCategory([NotNull] IObjectDataset objectDataset,
		                                               [NotNull] IObject ofObject)
		{
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));

			int subtypeFieldIndex = GdbObjectUtils.GetSubtypeFieldIndex(ofObject);

			if (subtypeFieldIndex < 0)
			{
				if (objectDataset.ObjectTypes.Count == 1)
				{
					return objectDataset.ObjectTypes[0];
				}

				if (objectDataset.ObjectTypes.Count > 1)
				{
					_msg.WarnFormat(
						"More than one object type for dataset without subtypes: {0}",
						objectDataset.AliasName);

					return null;
				}

				// no default object type; return null
				return null;
			}

			object value = ofObject.Value[subtypeFieldIndex];
			if (value is DBNull || value == null)
			{
				return null;
			}

			ObjectType objectType = objectDataset.GetObjectType((int) value);

			if (objectType == null)
			{
				return null;
			}

			ObjectSubtype objectSubtype =
				ObjectCategoryUtils.TryIdentifyObjectSubtype(objectType, ofObject);

			return objectSubtype ?? (ObjectCategory) objectType;
		}

		[NotNull]
		public static IEnumerable<int> GetObjectDefiningFieldIndexes(
			[NotNull] IObjectDataset objectDataset,
			[NotNull] IDatasetContext datasetContext,
			[CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));

			IObjectClass objectClass = datasetContext.OpenObjectClass(objectDataset);
			Assert.NotNull(objectClass, "dataset does not exist in context: {0}",
			               objectDataset.Name);

			return GetObjectDefiningFieldIndexes(objectDataset, objectClass, fieldIndexCache);
		}

		[NotNull]
		public static IEnumerable<int> GetObjectDefiningFieldIndexes(
			[NotNull] IObjectDataset objectDataset,
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			foreach (ObjectAttribute attribute in GetObjectDefiningAttributes(
				         objectDataset, objectClass, fieldIndexCache))
			{
				int fieldIndex =
					AttributeUtils.GetFieldIndex(objectClass, attribute, fieldIndexCache);

				if (fieldIndex >= 0)
				{
					yield return fieldIndex;
				}
			}
		}

		[NotNull]
		public static IEnumerable<ObjectAttribute> GetObjectDefiningAttributes(
			[NotNull] IObjectDataset objectDataset,
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IFieldIndexCache fieldIndexCache)
		{
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypeHandled = false;

			int subtypeFieldIndex = fieldIndexCache?.GetSubtypeFieldIndex(objectClass) ??
			                        DatasetUtils.GetSubtypeFieldIndex(objectClass);

			foreach (ObjectAttribute attribute in objectDataset.GetAttributes())
			{
				if (attribute.IsObjectDefining)
				{
					yield return attribute;
				}
				else if (! subtypeHandled)
				{
					if (subtypeFieldIndex < 0)
					{
						// class has no subtype
						subtypeHandled = true;
					}
					else
					{
						int fieldIndex =
							AttributeUtils.GetFieldIndex(objectClass, attribute, fieldIndexCache);

						if (fieldIndex == subtypeFieldIndex)
						{
							// attribute is subtype field; these are always object defining
							yield return attribute;

							subtypeHandled = true;
						}
					}
				}
			}
		}

		#region Object attribute values

		public static object GetValue([NotNull] IObject obj,
		                              [NotNull] AttributeRole attributeRole,
		                              [NotNull] IObjectDataset objectDataset,
		                              [CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			ObjectAttribute attribute = GetExistingAttribute(objectDataset, attributeRole);

			return AttributeUtils.GetValueFor(obj, attribute, fieldIndexCache);
		}

		public static bool SetValue([NotNull] IObject obj,
		                            [NotNull] AttributeRole attributeRole,
		                            [CanBeNull] object value,
		                            [NotNull] IObjectDataset objectDataset,
		                            [CanBeNull] IFieldIndexCache fieldIndexCache = null,
		                            bool writeOnlyIfNull = false,
		                            bool allowMissingRole = false)
		{
			ObjectAttribute attribute = objectDataset.GetAttribute(attributeRole);

			if (attribute == null)
			{
				if (! allowMissingRole)
				{
					throw new ArgumentException(
						string.Format(
							"Attribute for role {0} not defined in dataset {1}",
							attributeRole, objectDataset));
				}

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat(
						"Attribute for role {0} not defined in dataset {1}. Value not written",
						attributeRole, objectDataset);
				}

				return false;
			}

			int fieldIndex = AttributeUtils.GetFieldIndex(obj.Class, attribute, fieldIndexCache);

			if (fieldIndex >= 0)
			{
				if (! writeOnlyIfNull || GdbObjectUtils.IsNullOrEmpty(obj, fieldIndex))
				{
					SetValueCore(obj, attribute, fieldIndex, value);
				}
			}
			else
			{
				throw new InvalidConfigurationException(
					string.Format("Field not found for attribute {0}", attribute));
			}

			return true;
		}

		public static void SetOperator([NotNull] IObject obj, string operatorName,
		                               [NotNull] IObjectDataset objectDataset,
		                               [CanBeNull] IFieldIndexCache fieldIndexCache)
		{
			AttributeRole role = AttributeRole.Operator;

			// special treatment for long operator names (assume that up to 10 chars are guaranteed to fit into field)
			ObjectAttribute attribute = objectDataset.GetAttribute(role);
			Assert.NotNull(attribute, "No operator attribute defined for {0}", objectDataset.Name);

			int fieldIndex = AttributeUtils.GetFieldIndex(obj.Class, attribute, fieldIndexCache);
			if (fieldIndex < 0)
			{
				throw new InvalidOperationException(
					string.Format("Operator field {0} not found in current context for dataset {1}",
					              attribute.Name,
					              DatasetUtils.GetName(obj.Class)));
			}

			if (operatorName.Length > _minimumExpectedOperatorFieldLength)
			{
				int fieldLength = obj.Fields.Field[fieldIndex].Length;

				if (operatorName.Length > fieldLength)
				{
					string truncated = operatorName.Substring(0, fieldLength);

					_msg.WarnFormat("Truncating long operator name: {0} -> {1}",
					                operatorName, truncated);

					operatorName = truncated;
				}
			}

			SetValueCore(obj, attribute, fieldIndex, operatorName);
		}

		/// <summary>
		/// Gets the UUID for an object
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="objectDataset"></param>
		/// <param name="fieldIndexCache">The optional field index cache.</param>
		/// <returns>
		/// a guid string, or an empty string if the object does not have a uuid attribute or value.
		/// </returns>
		public static string GetUuid([NotNull] IObject obj,
		                             [NotNull] IObjectDataset objectDataset,
		                             [CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			ObjectAttribute uuidAttribute = objectDataset.GetAttribute(AttributeRole.UUID);

			if (uuidAttribute == null)
			{
				return string.Empty;
			}

			int fieldIndex =
				AttributeUtils.GetFieldIndex(obj.Class, uuidAttribute, fieldIndexCache);
			if (fieldIndex < 0)
			{
				return string.Empty;
			}

			// write a UID, read a string...
			var guidString = obj.Value[fieldIndex] as string;

			// TODO revise...
			return guidString ?? string.Empty;
		}

		private static void SetValueCore([NotNull] IObject obj,
		                                 [NotNull] Attribute attribute,
		                                 int fieldIndex,
		                                 [CanBeNull] object value)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			if (_msg.IsDebugEnabled)
			{
				var uid = value as IUID;
				_msg.DebugFormat("Set {0}={1}", attribute.Name, uid != null
					                                                ? uid.Value
					                                                : value);
			}

			// If writing null, the value 0 is actually written in case of numeric fields.
			// needs to be DBNull.Value.
			object valueToWrite = value ?? DBNull.Value;

			obj.Value[fieldIndex] = valueToWrite;
		}

		#endregion
	}
}
