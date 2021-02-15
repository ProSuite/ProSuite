using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.QA.TestFactories
{
	internal static class ConfiguratorUtils
	{
		[NotNull]
		public static IObjectClass OpenFromDefaultDatabase(
			[NotNull] IObjectDataset objectDataset)
		{
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));

			IWorkspaceContext masterDbContext =
				ModelElementUtils.GetMasterDatabaseWorkspaceContext(objectDataset);

			Assert.NotNull(masterDbContext,
			               "The model master database for dataset {0} is not accessible",
			               objectDataset.Name);

			IObjectClass result = masterDbContext.OpenObjectClass(objectDataset);

			Assert.NotNull(result, "Object class {0} not found in master database",
			               objectDataset.Name);

			return result;
		}

		[NotNull]
		public static IFeatureClass OpenFromDefaultDatabase([NotNull] IVectorDataset dataset)
		{
			return (IFeatureClass) OpenFromDefaultDatabase((IObjectDataset) dataset);
		}

		[CanBeNull]
		public static T GetDataset<T>([NotNull] string datasetName,
		                              [NotNull] IEnumerable<Dataset> datasets) where T : Dataset
		{
			Assert.ArgumentNotNullOrEmpty(datasetName, "name");
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			foreach (Dataset dataset in datasets)
			{
				var typedDataset = dataset as T;
				if (typedDataset == null)
				{
					continue;
				}

				string modelDatasetName = dataset.Model.ElementNamesAreQualified
					                          ? datasetName // matches only if also qualified
					                          : ModelElementNameUtils.GetUnqualifiedName(
						                          datasetName);

				if (string.Equals(dataset.Name, modelDatasetName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return typedDataset;
				}
			}

			return null;
		}
	}
}
