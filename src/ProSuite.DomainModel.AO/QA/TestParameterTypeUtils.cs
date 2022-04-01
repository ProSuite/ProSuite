#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.DomainModel.AO.QA
{
	public static class TestParameterTypeUtils
	{
		public static void AssertValidDataset([NotNull] TestParameter testParameter,
		                                      [CanBeNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			if (dataset == null) return;

			TestParameterType parameterType = GetParameterType(testParameter.Type);

			Assert.True(IsValidDataset(parameterType, dataset),
			            "Invalid dataset for test parameter type {0}: {1} ({2})",
			            Enum.GetName(typeof(TestParameterType), parameterType), dataset,
			            testParameter.Name);
		}

		public static TestParameterType GetParameterType([NotNull] Type dataType)
		{
			Assert.ArgumentNotNull(dataType, nameof(dataType));

			// NOTE: test more specific types first, base types last

			if (typeof(IReadOnlyFeatureClass).IsAssignableFrom(dataType))
				return TestParameterType.VectorDataset;
			if (typeof(IFeatureClass).IsAssignableFrom(dataType))
				return TestParameterType.VectorDataset;

			if (typeof(IReadOnlyTable).IsAssignableFrom(dataType))
				return TestParameterType.ObjectDataset;
			if (typeof(ITable).IsAssignableFrom(dataType))
				return TestParameterType.ObjectDataset;
			if (typeof(IObjectClass).IsAssignableFrom(dataType))
				return TestParameterType.ObjectDataset;

			if (typeof(IMosaicDataset).IsAssignableFrom(dataType))
				return TestParameterType.RasterMosaicDataset;
			if (typeof(IRasterDataset).IsAssignableFrom(dataType))
				return TestParameterType.RasterDataset;
			if (typeof(SimpleRasterMosaic).IsAssignableFrom(dataType))
				return TestParameterType.RasterMosaicDataset;
			if (typeof(TerrainReference).IsAssignableFrom(dataType))
				return TestParameterType.TerrainDataset;

			// The following types cannot be loaded in the Enterprise SDK:
			if (dataType.Name == "IMosaicLayer")
			{
				return TestParameterType.RasterMosaicDataset;
			}

			if (dataType.Name == "ITopology")
			{
				return TestParameterType.TopologyDataset;
			}

			if (dataType.Name == "IGeometricNetwork")
			{
				return TestParameterType.GeometricNetworkDataset;
			}

			if (dataType.Name == "ITerrain")
			{
				return TestParameterType.TerrainDataset;
			}

			if (dataType == typeof(double))
				return TestParameterType.Double;
			if (dataType == typeof(int))
				return TestParameterType.Integer;
			if (dataType == typeof(bool))
				return TestParameterType.Boolean;
			if (dataType == typeof(string))
				return TestParameterType.String;
			if (dataType == typeof(DateTime))
				return TestParameterType.DateTime;
			if (dataType.IsEnum)
				return TestParameterType.Integer;

			return TestParameterType.CustomScalar;
		}

		public static bool IsValidDataset(TestParameterType parameterType,
		                                  [NotNull] Dataset dataset)
		{
			switch (parameterType)
			{
				case TestParameterType.Dataset:
					return true;

				case TestParameterType.ObjectDataset:
					return dataset is ObjectDataset;

				case TestParameterType.VectorDataset:
					return dataset is VectorDataset;

				case TestParameterType.TableDataset:
					return dataset is TableDataset;

				case TestParameterType.TopologyDataset:
					return dataset.GeometryType is GeometryTypeTopology;

				case TestParameterType.TerrainDataset:
					return dataset.GeometryType is GeometryTypeTerrain;

				case TestParameterType.GeometricNetworkDataset:
					return dataset.GeometryType is GeometryTypeGeometricNetwork;

				case TestParameterType.RasterMosaicDataset:
					return dataset.GeometryType is GeometryTypeRasterMosaic;

				case TestParameterType.RasterDataset:
					return dataset.GeometryType is GeometryTypeRasterDataset;

				default:
					throw new ArgumentException(
						string.Format("Unsupported parameter type: {0}",
						              Enum.GetName(typeof(TestParameterType), parameterType)));
			}
		}
	}
}
