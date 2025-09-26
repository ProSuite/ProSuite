using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;
using FieldType = ProSuite.Commons.GeoDb.FieldType;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	/// <summary>
	/// Provides functionality to add/update/delete attributes for domain classes that derive
	/// from IAttributes while separating the ArcObjects dependencies from the domain classes.
	/// </summary>
	public static class AttributeHarvestingUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// TODO: Is this a static Utils class? Or does each IAttributes implementation have
		// its specific AttributeHarvester? Or will there be a DatasetHarvester/ModelHarvester
		// that can create instances of this class (for the right IAttributes implementation)
		// in order to more loosely couple them with the domain classes?

		//private readonly IAttributes _attributeContainer;

		//public AttributeHarvester(IAttributes attributeContainer)
		//{
		//	_attributeContainer = attributeContainer;
		//}

		#region AttributedAssociation Attribute harvesting

		public static void HarvestAttributes(
			[NotNull] AttributedAssociation attributedAssociation,
			[NotNull] IWorkspaceContext workspaceContext)
		{
			// TODO: support for configurator?

			attributedAssociation.ClearAttributeMaps();

			using (_msg.IncrementIndentation(
				       "Harvesting attributes for attributed association {0}",
				       attributedAssociation.Name))
			{
				//const bool allowAlways = true;
				//IRelationshipClass relationshipClass =
				//	ModelElementUtils.TryOpenFromMasterDatabase(attributedAssociation, allowAlways);
				IRelationshipClass relationshipClass =
					workspaceContext.OpenRelationshipClass(attributedAssociation);
				Assert.NotNull(relationshipClass,
				               "Relationship class not found in model master database: {0}",
				               attributedAssociation.Name);
				var table = (ITable) relationshipClass;

				IList<IField> fields = DatasetUtils.GetFields(table);

				for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
				{
					IField field = fields[fieldIndex];

					AddOrUpdateAttribute(attributedAssociation, field, fieldIndex);
				}

				DeleteAttributesNotInList(attributedAssociation, fields);

				attributedAssociation.ClearAttributeMaps();
			}
		}

		private static void AddOrUpdateAttribute(
			[NotNull] AttributedAssociation attributedAssociation,
			[NotNull] IField field,
			int fieldIndex)
		{
			Assert.ArgumentNotNull(field, nameof(field));

			const bool includeDeleted = true;
			AssociationAttribute attribute =
				attributedAssociation.GetAttribute(field.Name, includeDeleted);

			if (attribute == null)
			{
				_msg.InfoFormat("Adding attribute {0}", field.Name);

				attribute = attributedAssociation.AddAttribute(field.Name, (FieldType) field.Type);
			}
			else
			{
				// attribute already registered
				if (attribute.Deleted)
				{
					_msg.WarnFormat(
						"Attribute {0} was registered as deleted, but exists now. " +
						"Resurrecting it...", attribute.Name);

					attribute.RegisterExisting();
				}

				attribute.FieldType = (FieldType) field.Type;
			}

			// configure the attribute
			attribute.SortOrder = fieldIndex;
		}

		#endregion

		#region ObjectDataset Attribute harvesting

		public static void HarvestAttributes(
			[NotNull] ObjectDataset objectDataset,
			[CanBeNull] IWorkspaceContext workspaceContext = null,
			[CanBeNull] IAttributeConfigurator configurator = null)
		{
			if (workspaceContext == null)
			{
				workspaceContext =
					ModelElementUtils.GetAccessibleMasterDatabaseWorkspaceContext(objectDataset);
			}

			IObjectClass objectClass =
				Assert.NotNull(workspaceContext).OpenObjectClass(objectDataset);

			Assert.NotNull(objectClass, "Unable to open object class {0}", objectDataset.Name);

			HarvestAttributes(objectDataset, configurator, objectClass);
		}

		public static void HarvestAttributes([NotNull] ObjectDataset objectDataset,
		                                     [CanBeNull] IAttributeConfigurator configurator,
		                                     [NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			objectDataset.ClearAttributeMaps();

			IList<IField> fields = DatasetUtils.GetFields(objectClass);

			for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
			{
				AddOrUpdateAttribute(objectDataset, fields[fieldIndex], fieldIndex);
			}

			if (configurator != null)
			{
				configurator.Configure(objectDataset, objectClass);

				// Clear _attributesByRole cache
				objectDataset.ClearAttributeMaps();
			}

			DeleteAttributesNotInList(objectDataset, fields);

			objectDataset.ClearAttributeMaps();
		}

		public static void HarvestGeometryType([NotNull] ObjectDataset objectDataset,
		                                       [NotNull]
		                                       IGeometryTypeConfigurator geometryTypeConfigurator,
		                                       [NotNull] IObjectClass objectClass)
		{
			if (! (objectDataset is VectorDataset))
			{
				return;
			}

			if (! (objectClass is IFeatureClass featureClass))
			{
				return;
			}

			GeometryTypeShape correctGeometryType =
				geometryTypeConfigurator.GetGeometryType(featureClass.ShapeType);

			if (correctGeometryType?.Id != objectDataset.GeometryType?.Id)
			{
				_msg.InfoFormat("Dataset {0} has changed geometry type. Previous: {1}, New: {2}",
				                objectDataset.Name, objectDataset.GeometryType?.Name ?? "<none>",
				                correctGeometryType?.Name);

				objectDataset.GeometryType = correctGeometryType;
			}
		}

		private static void AddOrUpdateAttribute([NotNull] ObjectDataset objectDataset,
		                                         [NotNull] IField field,
		                                         int fieldIndex)
		{
			Assert.ArgumentNotNull(field, nameof(field));

			ObjectAttribute attribute = objectDataset.Attributes.FirstOrDefault(
				att => string.Equals(att.Name, field.Name,
				                     StringComparison
					                     .OrdinalIgnoreCase));

			if (attribute == null)
			{
				attribute = objectDataset.CreateObjectAttribute(field.Name, (FieldType) field.Type);
				attribute.FieldLength = field.Length;

				_msg.InfoFormat("Adding attribute {0}", attribute.Name);

				objectDataset.AddAttribute(attribute);
			}
			else
			{
				if (attribute.Deleted)
				{
					_msg.WarnFormat(
						"Attribute {0} was registered as deleted, but exists now. " +
						"Resurrecting it...", attribute.Name);

					attribute.RegisterExisting();
				}

				attribute.FieldType = (FieldType) field.Type;
				attribute.FieldLength = field.Length;
			}

			// configure the attribute, no matter if it existed or was new
			attribute.SortOrder = fieldIndex;
		}

		#endregion

		private static void DeleteAttributesNotInList([NotNull] IAttributes attributeContainer,
		                                              [NotNull] ICollection<IField> fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			foreach (Attribute attribute in attributeContainer.Attributes)
			{
				if (ExistsField(fields, attribute.Name) || attribute.Deleted)
				{
					continue;
				}

				_msg.WarnFormat("Registering attribute {0} as deleted",
				                attribute.Name);

				attribute.RegisterDeleted();
			}
		}

		private static bool ExistsField([NotNull] IEnumerable<IField> fields,
		                                [NotNull] string name)
		{
			foreach (IField field in fields)
			{
				// don't do this...
				//if (Equals(field.Name, name))
				// .. instead, use same string comparision flavor as in AttributesByName
				if (string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
