using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.AO.QA
{
	public abstract class TestParameterDatasetProviderBase :
		ITestParameterDatasetProvider
	{
		[NotNull] private readonly Action<Action> _transactionFunction;
		[NotNull] private readonly IDatasetRepository _datasetRepository;
		[CanBeNull] private readonly IInstanceConfigurationRepository _transformerConfigRepository;
		[CanBeNull] private TransformerConfiguration _excludedTransformer;

		/// <summary>
		/// Initializes a new instance of the <see cref="TestParameterDatasetProviderBase"/> class.
		/// </summary>
		protected TestParameterDatasetProviderBase(
			[NotNull] Action<Action> transactionFunction,
			[NotNull] IDatasetRepository datasetRepository,
			[CanBeNull] IInstanceConfigurationRepository transformerConfigRepository = null)
		{
			Assert.ArgumentNotNull(transactionFunction, nameof(transactionFunction));
			Assert.ArgumentNotNull(datasetRepository, nameof(datasetRepository));

			_transactionFunction = transactionFunction;
			_datasetRepository = datasetRepository;
			_transformerConfigRepository = transformerConfigRepository;
		}

		protected abstract bool IsSelectable([NotNull] Dataset dataset);

		public IEnumerable<Dataset> GetDatasets(TestParameterType validTypes, DdxModel model)
		{
			return GetDatasets(model)
			       .Where(dataset => ! dataset.Deleted &&
			                         IsSelectable(dataset) &&
			                         IsApplicable(dataset.GeometryType, validTypes))
			       .OrderBy(d => d.Name);
		}

		public IEnumerable<TransformerConfiguration> GetTransformers(TestParameterType validType,
			DdxModel model)
		{
			IList<TransformerConfiguration> transformers = null;
			_transactionFunction(() => transformers = GetTransformers(model));

			return transformers.Where(tr => IsTransformerSelectable(tr) &&
			                                IsTransformerApplicable(tr, validType, model));
		}

		public void Exclude(TransformerConfiguration transformer)
		{
			_excludedTransformer = transformer;
		}

		private bool IsTransformerSelectable(TransformerConfiguration transformerConfig)
		{
			if (_excludedTransformer == null)
			{
				return true;
			}

			return transformerConfig.Id != _excludedTransformer.Id;
		}

		private static bool IsTransformerApplicable(TransformerConfiguration transformerConfig,
		                                            TestParameterType validType,
		                                            DdxModel model)
		{
			if (model != null)
			{
				bool anyDatasetIsFromModel = transformerConfig.ParameterValues.Any(
					p => p is DatasetTestParameterValue datasetParameterValue &&
					     datasetParameterValue.DatasetValue != null &&
					     datasetParameterValue.DatasetValue.Model.Equals(model));

				if (! anyDatasetIsFromModel)
				{
					return false;
				}
			}

			// TODO: Instantiate transformer, add method to get result geometry type or at least
			//       whether the result is a vector dataset
			// return IsApplicable(transformerConfig.GetResultGeometryType(), validType);

			return true;
		}

		[NotNull]
		private IEnumerable<Dataset> GetDatasets([CanBeNull] DdxModel model = null)
		{
			IList<Dataset> result = null;

			_transactionFunction(() => result = model == null
				                                    ? _datasetRepository.GetAll<Dataset>()
				                                    : _datasetRepository.Get<Dataset>(model));

			return result;
		}

		[NotNull]
		private IList<TransformerConfiguration> GetTransformers([CanBeNull] DdxModel model = null)
		{
			if (_transformerConfigRepository == null)
			{
				return new List<TransformerConfiguration>(0);
			}

			return _transformerConfigRepository
				.GetInstanceConfigurations<TransformerConfiguration>();
		}

		private static bool IsApplicable([CanBeNull] GeometryType geometryType,
		                                 TestParameterType applicableParameterTypes)
		{
			if (applicableParameterTypes == TestParameterType.Unknown)
			{
				return true;
			}

			if (geometryType == null)
			{
				return false;
			}

			TestParameterType candidateType = GetTestParameterType(geometryType);

			if (candidateType == TestParameterType.VectorDataset)
			{
				// check if all geometry types are allowed
				if ((applicableParameterTypes & TestParameterType.VectorDataset) ==
				    TestParameterType.VectorDataset)
				{
					return true;
				}

				var geometryTypeShape = (GeometryTypeShape) geometryType;

				candidateType = GetTestParameterType(geometryTypeShape.ToEsriGeometryType());

				if ((applicableParameterTypes & candidateType) != 0)
				{
					return true;
				}
			}
			else if ((applicableParameterTypes & candidateType) != 0)
			{
				return true;
			}

			return false;
		}

		private static TestParameterType GetTestParameterType(
			[NotNull] GeometryType geometryType)
		{
			if (geometryType is GeometryTypeNoGeometry)
			{
				return TestParameterType.NonVectorDataset;
			}

			if (geometryType is GeometryTypeShape)
			{
				return TestParameterType.VectorDataset;
			}

			if (geometryType is GeometryTypeTerrain)
			{
				return TestParameterType.TerrainDataset;
			}

			if (geometryType is GeometryTypeTopology)
			{
				return TestParameterType.TopologyDataset;
			}

			if (geometryType is GeometryTypeGeometricNetwork)
			{
				return TestParameterType.GeometricNetworkDataset;
			}

			if (geometryType is GeometryTypeRasterMosaic)
			{
				return TestParameterType.RasterMosaicDataset;
			}

			if (geometryType is GeometryTypeRasterDataset)
			{
				return TestParameterType.RasterDataset;
			}

			throw new NotImplementedException("Unhandled GeometryType " +
			                                  geometryType.GetType());
		}

		private static TestParameterType GetTestParameterType(esriGeometryType geometryType)
		{
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					return TestParameterType.PointDataset;

				case esriGeometryType.esriGeometryMultipoint:
					return TestParameterType.MultipointDataset;

				case esriGeometryType.esriGeometryPolyline:
					return TestParameterType.PolylineDataset;

				case esriGeometryType.esriGeometryPolygon:
					return TestParameterType.PolygonDataset;

				case esriGeometryType.esriGeometryMultiPatch:
					return TestParameterType.MultipatchDataset;

				default:
					throw new NotImplementedException("Unhandled geometry type " +
					                                  geometryType);
			}
		}
	}
}
