using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DomainModel.AO.DataModel
{
	/// <summary>
	/// Provides ArcObjects-specific functionality for domain attributes.
	/// </summary>
	public static class AttributeUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static int GetFieldIndex([NotNull] IObjectClass objectClass,
		                                [NotNull] ObjectAttribute objectAttribute,
		                                [CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			return GetFieldIndex((ITable) objectClass, objectAttribute, fieldIndexCache);
		}

		public static int GetFieldIndex([NotNull] ITable table,
		                                [NotNull] Attribute attribute,
		                                [CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			AttributeRole attributeRole = (attribute as ObjectAttribute)?.Role;

			return
				fieldIndexCache?.GetFieldIndex(table, attribute.Name, attributeRole) ??
				GetFieldIndex(table, attribute.Name, attributeRole);
		}

		public static int GetFieldIndex([NotNull] ITable table,
		                                [NotNull] ObjectAttribute objectAttribute,
		                                [CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			return
				fieldIndexCache?.GetFieldIndex(table, objectAttribute.Name, objectAttribute.Role) ??
				GetFieldIndex(table, objectAttribute.Name, objectAttribute.Role);
		}

		public static int GetFieldIndex([NotNull] ITable table,
		                                [NotNull] AssociationAttribute associationAttribute,
		                                [CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			return
				fieldIndexCache?.GetFieldIndex(table, associationAttribute.Name, null) ??
				GetFieldIndex(table, associationAttribute.Name, null);
		}

		public static int GetFieldIndex([NotNull] ITable table,
		                                [NotNull] string fieldName,
		                                [CanBeNull] AttributeRole attributeRole)
		{
			// TODO search first based on name, or first based on role?

			if (Equals(attributeRole, AttributeRole.Shape))
			{
				var featureClass = table as IFeatureClass;
				if (featureClass != null)
				{
					return table.FindField(featureClass.ShapeFieldName);
				}
			}
			else if (Equals(attributeRole, AttributeRole.ShapeArea))
			{
				var featureClass = table as IFeatureClass;
				if (featureClass != null)
				{
					IField areaField = DatasetUtils.GetAreaField(featureClass);
					if (areaField != null)
					{
						return table.FindField(areaField.Name);
					}
				}
			}
			else if (Equals(attributeRole, AttributeRole.ShapeLength))
			{
				var featureClass = table as IFeatureClass;
				if (featureClass != null)
				{
					IField lengthField = DatasetUtils.GetLengthField(featureClass);
					if (lengthField != null)
					{
						return table.FindField(lengthField.Name);
					}
				}
			}
			else if (Equals(attributeRole, AttributeRole.ObjectID))
			{
				return table.FindField(table.OIDFieldName);
			}

			return table.FindField(fieldName);
		}

		public static int GetRequiredFieldIndex(
			[NotNull] IRow row,
			[NotNull] Attribute attribute,
			[CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			return GetRequiredFieldIndex(row.Table, attribute, fieldIndexCache);
		}

		public static int GetRequiredFieldIndex(
			[NotNull] ITable table,
			[NotNull] Attribute attribute,
			[CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			int fieldIndex = GetFieldIndex(table, attribute, fieldIndexCache);

			if (fieldIndex < 0)
			{
				_msg.DebugFormat("Missing attribute: {0}", attribute);

				throw new InvalidOperationException(
					string.Format("Field {0} not found in table {1}",
					              attribute.Name, DatasetUtils.GetName(table)));
			}

			return fieldIndex;
		}

		public static object GetValueFor([NotNull] IRow row,
		                                 [NotNull] Attribute attribute,
		                                 [CanBeNull] IFieldIndexCache fieldIndexCache)
		{
			return row.Value[GetRequiredFieldIndex(row, attribute, fieldIndexCache)];
		}

		public static T? GetValueFor<T>([NotNull] IRow row,
		                                [NotNull] Attribute attribute,
		                                [CanBeNull] IFieldIndexCache fieldIndexCache)
			where T : struct
		{
			var fieldIndex = GetRequiredFieldIndex(row, attribute, fieldIndexCache);

			return GdbObjectUtils.ConvertRowValue<T>(row, fieldIndex);
		}
	}
}
