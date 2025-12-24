using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel;

public static class AttributeUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

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
	                                [CanBeNull] string fieldName,
	                                [CanBeNull] AttributeRole role,
	                                [CanBeNull] FieldIndexCache fieldIndexCache = null)
	{
		// TODO search first based on name, or first based on role?

		TableDefinition tableDefinition;

		if (Equals(role, AttributeRole.Shape))
		{
			if (table is FeatureClass featureClass)
			{
				return GetShapeFieldIndex(featureClass, fieldIndexCache);
			}
		}
		else if (Equals(role, AttributeRole.ShapeArea))
		{
			if (table is FeatureClass featureClass)
			{
				FeatureClassDefinition definition = featureClass.GetDefinition();

				string areaField = DatasetUtils.GetAreaFieldName(definition);

				if (areaField != null)
				{
					return fieldIndexCache?.GetFieldIndex(featureClass, areaField) ??
					       definition.FindField(areaField);
				}
			}
		}
		else if (Equals(role, AttributeRole.ShapeLength))
		{
			if (table is FeatureClass featureClass)
			{
				FeatureClassDefinition definition = featureClass.GetDefinition();

				string lengthField = DatasetUtils.GetLengthFieldName(definition);

				if (lengthField != null)
				{
					return fieldIndexCache?.GetFieldIndex(featureClass, lengthField) ??
					       definition.FindField(lengthField);
				}
			}
		}
		else if (Equals(role, AttributeRole.ObjectID))
		{
			tableDefinition = table.GetDefinition();

			string oidField = tableDefinition.GetObjectIDField();

			return fieldIndexCache?.GetFieldIndex(table, oidField) ??
			       tableDefinition.FindField(oidField);
		}

		tableDefinition = table.GetDefinition();

		return tableDefinition.FindField(fieldName);
	}

	// todo daro to Utils?
	private static int GetShapeFieldIndex([NotNull] FeatureClass featureClass,
	                                      [CanBeNull] FieldIndexCache fieldIndexCache)
	{
		FeatureClassDefinition definition = featureClass.GetDefinition();

		string shapeField = definition.GetShapeField();

		return fieldIndexCache?.GetFieldIndex(featureClass, shapeField) ??
		       definition.FindField(shapeField);
	}

	[CanBeNull]
	public static ObjectAttribute GetAttribute([NotNull] IObjectDataset dataset,
	                                           [NotNull] AttributeRole role)
	{
		ObjectAttribute attribute = dataset.GetAttribute(role);

		if (attribute != null)
		{
			return attribute;
		}

		_msg.Debug($"Attribute role {role} does not exist in {dataset.Name}");
		return null;
	}
}
