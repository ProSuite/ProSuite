using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase;

namespace ProSuite.DomainModel.Core.DataModel.GIS
{
	public static class AttributeUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static int GetFieldIndex([NotNull] IObjectClass objectClass,
		                                [NotNull] ObjectAttribute objectAttribute)
		{
			return GetFieldIndex((ITable) objectClass, objectAttribute.Name, objectAttribute.Role);
		}

		public static int GetFieldIndex(ITable table,
		                                string fieldName,
		                                AttributeRole role)
		{
			if (Equals(role, AttributeRole.Shape))
			{
				if (table is IFeatureClass featureClass)
				{
					return table.FindField(featureClass.ShapeFieldName);
				}
			}
			else if (Equals(role, AttributeRole.ShapeArea))
			{
				if (table is IFeatureClass featureClass)
				{
					IField areaField = DatasetUtils.GetAreaField(featureClass);

					if (areaField != null)
					{
						return table.FindField(areaField.Name);
					}
				}
			}
			else if (Equals(role, AttributeRole.ShapeLength))
			{
				if (table is IFeatureClass featureClass)
				{
					IField lengthField = DatasetUtils.GetLengthField(featureClass);
					if (lengthField != null)
					{
						return table.FindField(lengthField.Name);
					}
				}
			}
			else if (Equals(role, AttributeRole.ObjectID))
			{
				table.FindField(table.OIDFieldName);
			}

			return table.FindField(fieldName);
		}

		public static int GetRequiredFieldIndex(
			[NotNull] IRow row,
			[NotNull] Attribute attribute)
		{
			return GetRequiredFieldIndex(row.Table, attribute);
		}

		public static int GetRequiredFieldIndex(
			[NotNull] ITable table,
			[NotNull] Attribute attribute)
		{
			AttributeRole attributeRole = (attribute as ObjectAttribute)?.Role;

			int fieldIndex = GetFieldIndex(table, attribute.Name, attributeRole);

			if (fieldIndex < 0)
			{
				_msg.DebugFormat("Missing attribute: {0}", attribute);

				throw new InvalidOperationException(
					$"Field {attribute.Name} not found in table {DatasetUtils.GetName(table)}");
			}

			return fieldIndex;
		}

		public static object GetValueFor([NotNull] IRow row,
		                                 [NotNull] Attribute attribute)
		{
			int fieldIndex = GetRequiredFieldIndex(row, attribute);

			return row.get_Value(fieldIndex);
		}

		public static T? GetValueFor<T>([NotNull] IRow row,
		                                [NotNull] Attribute attribute)
			where T : struct
		{
			var fieldIndex = GetRequiredFieldIndex(row, attribute);

			return GdbObjectUtils.ConvertRowValue<T>(row, fieldIndex);
		}
	}
}
