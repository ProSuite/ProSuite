using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel
{
	public static class AttributeUtils
	{
		public static int GetFieldIndex([NotNull] Table table,
		                                [NotNull] ObjectAttribute objectAttribute)
		{
			return GetFieldIndex(table, objectAttribute.Name, objectAttribute.Role);
		}

		public static int GetFieldIndex([NotNull] Table table,
		                                [NotNull] Attribute attribute,
		                                [CanBeNull] FieldIndexCache fieldIndexCache = null)
		{
			AttributeRole attributeRole = (attribute as ObjectAttribute)?.Role;

			return fieldIndexCache?.GetFieldIndex(table, attribute.Name, attributeRole) ??
			       GetFieldIndex(table, attribute.Name, attributeRole);
		}

		public static int GetFieldIndex([NotNull] Table table,
		                                [NotNull] string fieldName,
		                                [CanBeNull] AttributeRole attributeRole)
		{
			// TODO search first based on name, or first based on role?

			TableDefinition tableDefinition;

			if (Equals(attributeRole, AttributeRole.Shape))
			{
				var featureClass = table as FeatureClass;
				if (featureClass != null)
				{
					FeatureClassDefinition featureClassDefinition = featureClass.GetDefinition();

					return featureClassDefinition.FindField(featureClassDefinition.GetShapeField());
				}
			}
			else if (Equals(attributeRole, AttributeRole.ShapeArea))
			{
				var featureClass = table as FeatureClass;
				if (featureClass != null)
				{
					FeatureClassDefinition featureClassDefinition = featureClass.GetDefinition();

					string areaField = DatasetUtils.GetAreaFieldName(featureClassDefinition);

					if (areaField != null)
					{
						return featureClassDefinition.FindField(areaField);
					}
				}
			}
			else if (Equals(attributeRole, AttributeRole.ShapeLength))
			{
				var featureClass = table as FeatureClass;
				if (featureClass != null)
				{
					FeatureClassDefinition featureClassDefinition = featureClass.GetDefinition();

					string lengthField = DatasetUtils.GetLengthFieldName(featureClassDefinition);

					if (lengthField != null)
					{
						return featureClassDefinition.FindField(lengthField);
					}
				}
			}
			else if (Equals(attributeRole, AttributeRole.ObjectID))
			{
				tableDefinition = table.GetDefinition();

				return tableDefinition.FindField(tableDefinition.GetObjectIDField());
			}

			tableDefinition = table.GetDefinition();

			return tableDefinition.FindField(fieldName);
		}
	}
}
