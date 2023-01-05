using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public abstract class QualitySpecificationFactoryBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected IVerifiedModelFactory ModelFactory { get; }
		[NotNull] private readonly IOpenDataset _datasetOpener;

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlBasedQualitySpecificationFactory"/> class.
		/// </summary>
		/// <param name="modelFactory">The model builder.</param>
		/// <param name="datasetOpener"></param>
		protected QualitySpecificationFactoryBase(
			[NotNull] IVerifiedModelFactory modelFactory,
			[NotNull] IOpenDataset datasetOpener)
		{
			Assert.ArgumentNotNull(modelFactory, nameof(modelFactory));
			Assert.ArgumentNotNull(datasetOpener, nameof(datasetOpener));

			ModelFactory = modelFactory;
			_datasetOpener = datasetOpener;
		}

		protected static void HandleNoConditionCreated(
			[CanBeNull] string conditionName,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			bool ignoreConditionsForUnknownDatasets,
			[NotNull] ICollection<DatasetTestParameterRecord> unknownDatasetParameters)
		{
			Assert.True(ignoreConditionsForUnknownDatasets,
			            "ignoreConditionsForUnknownDatasets");
			Assert.True(unknownDatasetParameters.Count > 0,
			            "Unexpected number of unknown datasets");

			_msg.WarnFormat(
				unknownDatasetParameters.Count == 1
					? "Quality condition '{0}' is ignored because the following dataset is not found: {1}"
					: "Quality condition '{0}' is ignored because the following datasets are not found: {1}",
				conditionName,
				XmlDataQualityUtils.ConcatenateUnknownDatasetNames(
					unknownDatasetParameters,
					modelsByWorkspaceId,
					DataSource.AnonymousId));
		}

		protected ISpatialReference GetMainSpatialReference(
			Model model, IEnumerable<Dataset> referencedDatasets)
		{
			var spatialDatasetReferenceCount =
				new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			foreach (Dataset dataset in referencedDatasets)
			{
				if (! (dataset is ISpatialDataset))
				{
					continue;
				}

				if (! spatialDatasetReferenceCount.ContainsKey(dataset.Name))
				{
					spatialDatasetReferenceCount.Add(dataset.Name, 1);
				}
				else
				{
					spatialDatasetReferenceCount[dataset.Name]++;
				}
			}

			foreach (KeyValuePair<string, int> pair in
			         spatialDatasetReferenceCount.OrderByDescending(kvp => kvp.Value))
			{
				string datasetName = pair.Key;

				Dataset maxDataset = model.GetDatasetByModelName(datasetName);

				if (maxDataset == null)
				{
					continue;
				}

				ISpatialReference spatialReference = GetSpatialReference(maxDataset);

				if (spatialReference != null)
				{
					return spatialReference;
				}
			}

			return null;
		}

		[CanBeNull]
		private ISpatialReference GetSpatialReference([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IGeoDataset geoDataset = _datasetOpener.OpenDataset(dataset) as IGeoDataset;

			return geoDataset?.SpatialReference;
		}
	}
}
