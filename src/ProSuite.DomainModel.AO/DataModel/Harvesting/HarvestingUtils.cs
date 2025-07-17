using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.NamedValuesExpressions;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.LegacyTypes;
using FieldType = ProSuite.Commons.GeoDb.FieldType;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	internal static class HarvestingUtils
	{
		[NotNull]
		public static IEnumerable<IDatasetName> GetHarvestableDatasetNames(
			[NotNull] IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			return DatasetUtils.GetDatasetNames(featureWorkspace,
			                                    esriDatasetType.esriDTFeatureClass,
			                                    esriDatasetType.esriDTTable,
			                                    esriDatasetType.esriDTTerrain,
			                                    esriDatasetType.esriDTTopology,
			                                    esriDatasetType.esriDTGeometricNetwork,
			                                    esriDatasetType.esriDTMosaicDataset,
			                                    esriDatasetType.esriDTRasterDataset);
		}

		[NotNull]
		public static DatasetFilter CreateDatasetFilter(
			[CanBeNull] string inclusionCriteria,
			[CanBeNull] string exclusionCriteria)
		{
			var parser = new NamedValuesParser('=',
			                                   new[] { ";", Environment.NewLine },
			                                   new[] { "," },
			                                   " AND ");

			NotificationCollection notifications;
			IList<NamedValuesExpression> inclusionExpressions;
			if (! parser.TryParse(inclusionCriteria,
			                      out inclusionExpressions,
			                      out notifications))
			{
				throw new RuleViolationException(notifications,
				                                 "Error reading dataset inclusion criteria");
			}

			IList<NamedValuesExpression> exclusionExpressions;
			if (! parser.TryParse(exclusionCriteria,
			                      out exclusionExpressions,
			                      out notifications))
			{
				throw new RuleViolationException(notifications,
				                                 "Error reading dataset exclusion criteria");
			}

			DatasetFilter filter = DatasetFilterFactory.TryCreate(inclusionExpressions,
				exclusionExpressions,
				out notifications);
			if (filter == null)
			{
				throw new RuleViolationException(notifications, "Error creating dataset filter");
			}

			return filter;
		}

		public static bool ExistsAssociation(
			[NotNull] IEnumerable<IRelationshipClass> relClasses,
			[NotNull] string associationName)
		{
			Assert.ArgumentNotNull(relClasses, nameof(relClasses));
			Assert.ArgumentNotNull(associationName, nameof(associationName));

			bool nameIsQualified = ModelElementNameUtils.IsQualifiedName(associationName);

			foreach (IRelationshipClass relClass in relClasses)
			{
				string relClassName = nameIsQualified
					                      ? DatasetUtils.GetName(relClass)
					                      : DatasetUtils.GetUnqualifiedName(relClass);

				if (relClassName.Equals(associationName,
				                        StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		public static void RedirectAssociationEnd([NotNull] IRelationshipClass relClass,
		                                          [NotNull] AssociationEnd associationEnd,
		                                          [NotNull] IObjectDataset expectedDataset)
		{
			Assert.ArgumentNotNull(relClass, nameof(relClass));
			Assert.ArgumentNotNull(associationEnd, nameof(associationEnd));
			Assert.ArgumentNotNull(expectedDataset, nameof(expectedDataset));

			if (associationEnd is ForeignKeyAssociationEnd)
			{
				ObjectAttribute originFK = GetOriginFK(relClass, expectedDataset);

				((ForeignKeyAssociationEnd) associationEnd).Redirect(originFK);
			}
			else if (associationEnd is PrimaryKeyAssociationEnd)
			{
				ObjectAttribute originPK = GetOriginPK(relClass, expectedDataset);

				((PrimaryKeyAssociationEnd) associationEnd).Redirect(originPK);
			}
			else if (associationEnd is ManyToManyAssociationEnd)
			{
				var manyToManyEnd = (ManyToManyAssociationEnd) associationEnd;

				ObjectAttribute primaryKey;
				if (manyToManyEnd.IsDestinationEnd)
				{
					primaryKey = GetDestinationPK(relClass, expectedDataset);
				}
				else if (manyToManyEnd.IsOriginEnd)
				{
					primaryKey = GetOriginPK(relClass, expectedDataset);
				}
				else
				{
					throw new InvalidDataException(
						"Many to many end is neither origin nor destination for the association it belongs to");
				}

				manyToManyEnd.Redirect(primaryKey);
			}
			else
			{
				throw new ArgumentException("Unsupported association end type");
			}
		}

		public static AssociationCardinality GetCardinality(
			[NotNull] IRelationshipClass relClass)
		{
			switch (relClass.Cardinality)
			{
				case esriRelCardinality.esriRelCardinalityOneToOne:
					return AssociationCardinality.OneToOne;

				case esriRelCardinality.esriRelCardinalityManyToMany:
					return AssociationCardinality.ManyToMany;

				case esriRelCardinality.esriRelCardinalityOneToMany:
					return AssociationCardinality.OneToMany;

				default:
					throw new NotSupportedException(
						string.Format("Unsupported cardinality: {0}",
						              relClass.Cardinality));
			}
		}

		[NotNull]
		public static Association CreateAssociation(
			[NotNull] IRelationshipClass relClass,
			[NotNull] IObjectDataset destinationDataset,
			[NotNull] IObjectDataset originDataset,
			[NotNull] DdxModel model)
		{
			bool unqualifyDatasetName = ! model.HarvestQualifiedElementNames;

			string relClassName = DatasetUtils.GetName(relClass);

			AssociationCardinality cardinality = GetCardinality(relClass);
			ObjectAttribute originPK = GetOriginPK(relClass, originDataset);

			string associationName = ! unqualifyDatasetName
				                         ? relClassName
				                         : ModelElementNameUtils.GetUnqualifiedName(relClassName);

			if (! relClass.IsAttributed &&
			    relClass.Cardinality != esriRelCardinality.esriRelCardinalityManyToMany)
			{
				ObjectAttribute originFK = GetOriginFK(relClass,
				                                       destinationDataset);

				return new ForeignKeyAssociation(associationName,
				                                 cardinality,
				                                 originFK,
				                                 originPK) { Model = model };
			}

			ObjectAttribute destinationPK = GetDestinationPK(
				relClass, destinationDataset);

			var relTable = (ITable) relClass;
			esriFieldType destinationFKType = DatasetUtils.GetField(
				relTable,
				relClass.DestinationForeignKey).Type;
			esriFieldType originFKType = DatasetUtils.GetField(
				relTable, relClass.OriginForeignKey).Type;

			return new AttributedAssociation(
				       associationName,
				       cardinality,
				       relClass.DestinationForeignKey, (FieldType) destinationFKType,
				       destinationPK,
				       relClass.OriginForeignKey, (FieldType) originFKType,
				       originPK) { Model = model };
		}

		public static void EnsureUnqualifiedNames([NotNull] IModelElement modelElement,
		                                          [NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(modelElement, nameof(modelElement));
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			modelElement.Name = ModelElementNameUtils.GetUnqualifiedName(datasetName.Name);

			var featureDatasetElement = modelElement as IFeatureDatasetElement;

			if (featureDatasetElement == null)
			{
				return;
			}

			IDatasetName featureDatasetName = DatasetUtils.GetFeatureDatasetName(datasetName);
			Assert.NotNull(featureDatasetName,
			               "Unable to determine feature dataset name for {0}",
			               datasetName.Name);
			featureDatasetElement.FeatureDatasetName =
				ModelElementNameUtils.GetUnqualifiedName(featureDatasetName.Name);
		}

		[NotNull]
		private static ObjectAttribute GetOriginPK([NotNull] IRelationshipClass relClass,
		                                           [NotNull] IObjectDataset objectDataset)
		{
			ObjectAttribute originPK = objectDataset.GetAttribute(relClass.OriginPrimaryKey);

			Assert.NotNull(originPK, "origin primary key not found on dataset {0}: {1}",
			               objectDataset.Name,
			               relClass.OriginPrimaryKey);

			return originPK;
		}

		[NotNull]
		private static ObjectAttribute GetDestinationPK(
			[NotNull] IRelationshipClass relClass,
			[NotNull] IObjectDataset objectDataset)
		{
			ObjectAttribute destinationPK =
				objectDataset.GetAttribute(relClass.DestinationPrimaryKey);

			Assert.NotNull(destinationPK,
			               "destination primary key not found on dataset {0}: {1}",
			               objectDataset.Name,
			               relClass.DestinationPrimaryKey);

			return destinationPK;
		}

		[NotNull]
		private static ObjectAttribute GetOriginFK([NotNull] IRelationshipClass relClass,
		                                           [NotNull] IObjectDataset objectDataset)
		{
			ObjectAttribute originFK = objectDataset.GetAttribute(relClass.OriginForeignKey);

			Assert.NotNull(originFK, "origin foreign key not found on dataset {0}: {1}",
			               objectDataset.Name,
			               relClass.OriginForeignKey);

			return originFK;
		}

		public static void UpdateName([NotNull] IModelElement modelElement,
		                              [NotNull] IDatasetName datasetName,
		                              bool useQualifiedName)
		{
			Assert.ArgumentNotNull(modelElement, nameof(modelElement));
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			UpdateName(modelElement, datasetName.Name, useQualifiedName);

			var featureDatasetElement = modelElement as IFeatureDatasetElement;

			if (featureDatasetElement == null)
			{
				return;
			}

			IDatasetName featureDatasetName = DatasetUtils.GetFeatureDatasetName(datasetName);
			Assert.NotNull(featureDatasetName,
			               "Unable to determine feature dataset name for {0}. The dataset type is probably not supported on this plattform.",
			               datasetName.Name);

			featureDatasetElement.FeatureDatasetName =
				useQualifiedName
					? featureDatasetName.Name
					: ModelElementNameUtils.GetUnqualifiedName(featureDatasetName.Name);
		}

		public static void UpdateName([NotNull] IModelElement modelElement,
		                              [NotNull] string gdbDatasetName,
		                              bool useQualifiedName)
		{
			Assert.ArgumentNotNull(modelElement, nameof(modelElement));
			Assert.ArgumentNotNullOrEmpty(gdbDatasetName, nameof(gdbDatasetName));

			modelElement.Name = useQualifiedName
				                    ? gdbDatasetName
				                    : ModelElementNameUtils.GetUnqualifiedName(gdbDatasetName);
		}
	}
}
