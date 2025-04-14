using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.QA.Standalone.RuleBased
{
	public static class VerifiedModelUtils
	{
		[NotNull]
		public static Model CreateModel([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			IWorkspace workspace = DatasetUtils.GetWorkspace(objectClass);

			var featureClass = objectClass as IFeatureClass;
			return featureClass == null
				       ? CreateModel(workspace)
				       : CreateModel(workspace, ((IGeoDataset) featureClass).SpatialReference);
		}

		[NotNull]
		public static Model CreateModel([NotNull] IWorkspace workspace,
		                                [CanBeNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			Model model = CreateModel(workspace);

			if (spatialReference != null)
			{
				model.SpatialReferenceDescriptor =
					SpatialReferenceDescriptorExtensions.CreateFrom(spatialReference);
			}

			return model;
		}

		[NotNull]
		public static Dataset GetDataset([NotNull] IObjectClass objectClass,
		                                 [NotNull] Model model)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(model, nameof(model));

			Dataset result = model.GetDatasetByModelName(GetDatasetModelName(objectClass));

			return result ?? CreateDataset(objectClass, model);
		}

		[NotNull]
		public static Dataset CreateDataset([NotNull] IObjectClass objectClass,
		                                    [NotNull] Model model)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(model, nameof(model));

			var featureClass = objectClass as IFeatureClass;
			return featureClass != null
				       ? (Dataset) CreateVectorDataset(featureClass, model)
				       : CreateTableDataset(objectClass, model);
		}

		[NotNull]
		public static TableDataset CreateTableDataset(
			[NotNull] IObjectClass objectClass,
			[NotNull] Model model)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(model, nameof(model));

			var dataset = new VerifiedTableDataset(GetDatasetModelName(objectClass))
			              {
				              AliasName = objectClass.AliasName
			              };

			model.AddDataset(dataset);

			// no harvesting of attributes/object types

			return dataset;
		}

		[NotNull]
		public static VectorDataset CreateVectorDataset(
			[NotNull] IFeatureClass featureClass,
			[NotNull] Model model)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(model, nameof(model));

			var dataset = new VerifiedVectorDataset(GetDatasetModelName(featureClass))
			              {
				              AliasName = featureClass.AliasName
			              };

			model.AddDataset(dataset);

			// no harvesting of attributes/object types

			return dataset;
		}

		[NotNull]
		private static string GetDatasetModelName([NotNull] IObjectClass objectClass)
		{
			return DatasetUtils.GetName(objectClass);
		}

		[NotNull]
		private static Model CreateModel([NotNull] IWorkspace workspace)
		{
			string modelName = WorkspaceUtils.GetWorkspaceDisplayText(workspace);

			// no need to specify databaseName/schemaOwner, as dataset names will be fully qualified
			return new VerifiedModel(modelName, workspace,
			                         new MasterDatabaseWorkspaceContextFactory());
		}
	}
}
