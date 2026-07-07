using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

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
		TableDefinition tableDefinition = null;

		try
		{
			if (Equals(role, AttributeRole.Shape))
			{
				if (table is FeatureClass featureClass)
				{
					FeatureClassDefinition definition = featureClass.GetDefinition();

					string shapeField = definition.GetShapeField();

					return fieldIndexCache?.GetFieldIndex(featureClass, shapeField) ??
					       definition.FindField(shapeField);
				}
			}
			else if (Equals(role, AttributeRole.ShapeArea))
			{
				if (table is FeatureClass featureClass)
				{
					using FeatureClassDefinition definition = featureClass.GetDefinition();

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
					using FeatureClassDefinition definition = featureClass.GetDefinition();

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
		finally
		{
			tableDefinition?.Dispose();
		}
	}

	public static int GetFieldIndex([NotNull] TableDefinition definition,
	                                [NotNull] ObjectAttribute objectAttribute)
	{
		return GetFieldIndex(definition, objectAttribute.Name, objectAttribute.Role);
	}

	public static int GetFieldIndex([NotNull] TableDefinition definition,
	                                [CanBeNull] string fieldName,
	                                [CanBeNull] AttributeRole role)
	{
		// TODO search first based on name, or first based on role?

		if (Equals(role, AttributeRole.Shape))
		{
			if (definition is FeatureClassDefinition featureClassDef)
			{
				return featureClassDef.FindField(featureClassDef.GetShapeField());
			}
		}
		else if (Equals(role, AttributeRole.ShapeArea))
		{
			if (definition is FeatureClassDefinition featureClassDef)
			{
				string areaField = DatasetUtils.GetAreaFieldName(featureClassDef);

				if (areaField != null)
				{
					return definition.FindField(areaField);
				}
			}
		}
		else if (Equals(role, AttributeRole.ShapeLength))
		{
			if (definition is FeatureClassDefinition featureClassDef)
			{
				string lengthField = DatasetUtils.GetLengthFieldName(featureClassDef);

				if (lengthField != null)
				{
					return definition.FindField(lengthField);
				}
			}
		}
		else if (Equals(role, AttributeRole.ObjectID))
		{
			string oidField = definition.GetObjectIDField();

			return definition.FindField(oidField);
		}

		return definition.FindField(fieldName);
	}

	public static KeyValuePair<string, int> GetField([NotNull] TableDefinition definition,
	                                                 [NotNull] IObjectDataset dataset,
	                                                 [NotNull] AttributeRole role)
	{
		int fieldIndex;
		string fieldName = null;

		try
		{
			ObjectAttribute statusAttribute = dataset.GetAttribute(role);

			fieldName = Assert.NotNull(statusAttribute).Name;
			fieldIndex = GetFieldIndex(definition, statusAttribute);

			if (fieldIndex < 0)
			{
				throw new ArgumentException($"No field {fieldName}");
			}
		}
		catch (Exception e)
		{
			_msg.Error($"Field {fieldName} does not exist in {definition.GetName()}", e);
			throw;
		}

		return KeyValuePair.Create(fieldName, fieldIndex);
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

		_msg.VerboseDebug(() => "Attribute role {role} does not exist in {dataset.Name}");
		return null;
	}
}
