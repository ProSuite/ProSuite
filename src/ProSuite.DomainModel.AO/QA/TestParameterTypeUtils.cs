#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.LegacyTypes;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using RasterDataset = ProSuite.DomainModel.Core.DataModel.RasterDataset;

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

		public static void AssertValidDataset([NotNull] Type testParameterType,
		                                      [CanBeNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(testParameterType, nameof(testParameterType));

			if (dataset == null) return;

			TestParameterType parameterType = GetParameterType(testParameterType);

			Assert.True(IsValidDataset(parameterType, dataset),
			            "Invalid dataset for test parameter type {0}: {1}",
			            Enum.GetName(typeof(TestParameterType), parameterType), dataset);
		}

		public static TestParameterType GetParameterType([NotNull] Type dataType)
		{
			Assert.ArgumentNotNull(dataType, nameof(dataType));

			// NOTE: test more specific types first, base types last

			// Platform independent definition Types:
			if (typeof(IFeatureClassSchemaDef).IsAssignableFrom(dataType))
				return TestParameterType.VectorDataset;
			if (typeof(ITableSchemaDef).IsAssignableFrom(dataType))
				return TestParameterType.ObjectDataset;
			if (typeof(IMosaicRasterDatasetDef).IsAssignableFrom(dataType))
				return TestParameterType.RasterMosaicDataset;
			if (typeof(IRasterDatasetDef).IsAssignableFrom(dataType))
				return TestParameterType.RasterDataset;
			if (typeof(ITerrainDef).IsAssignableFrom(dataType))
				return TestParameterType.TerrainDataset;
			if (typeof(ITopologyDef).IsAssignableFrom(dataType))
				return TestParameterType.TopologyDataset;

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
			if (typeof(TopologyReference).IsAssignableFrom(dataType))
				return TestParameterType.TopologyDataset;

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

		public static bool IsDatasetType([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (type.IsValueType)
			{
				return false;
			}

			if (typeof(IFeatureClassSchemaDef).IsAssignableFrom(type) ||
			    typeof(ITableSchemaDef).IsAssignableFrom(type) ||
			    typeof(IRasterDatasetDef).IsAssignableFrom(type) ||
			    typeof(ITerrainDef).IsAssignableFrom(type) ||
			    typeof(ITopologyDef).IsAssignableFrom(type))
			{
				return true;
			}

			// Legacy types:
			return typeof(IReadOnlyFeatureClass).IsAssignableFrom(type) ||
			       typeof(IReadOnlyTable).IsAssignableFrom(type) ||
			       typeof(IFeatureClass).IsAssignableFrom(type) ||
			       typeof(ITable).IsAssignableFrom(type) ||
			       typeof(IObjectClass).IsAssignableFrom(type) ||
			       typeof(ITopology).IsAssignableFrom(type) ||
			       typeof(IRasterDataset).IsAssignableFrom(type) ||
			       typeof(IRasterDataset2).IsAssignableFrom(type) ||
			       typeof(IMosaicDataset).IsAssignableFrom(type) ||

			       // Remove once all 3d and GdbNetwork tests are officially de-supported:
#if ArcGIS // 10.x:
			       type.Name == "IMosaicLayer" ||
			       type.Name == "ITerrain" ||
			       type.Name == "IGeometricNetwork" ||
#endif
			       typeof(TerrainReference).IsAssignableFrom(type) ||
			       typeof(SimpleRasterMosaic).IsAssignableFrom(type) ||
			       typeof(TopologyReference).IsAssignableFrom(type);
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
					return dataset is TopologyDataset;

				case TestParameterType.TerrainDataset:
					return dataset is ISimpleTerrainDataset;

				case TestParameterType.GeometricNetworkDataset:
					return dataset is IGeometricNetworkDataset;

				case TestParameterType.RasterMosaicDataset:
					return dataset is IRasterMosaicDataset;

				case TestParameterType.RasterDataset:
					return dataset is RasterDataset;

				default:
					throw new ArgumentException(
						string.Format("Unsupported parameter type: {0}",
						              Enum.GetName(typeof(TestParameterType), parameterType)));
			}
		}

		[NotNull]
		public static TestParameterValue GetEmptyParameterValue(
			[NotNull] TestParameter testParameter)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			if (IsDatasetType(testParameter.Type))
			{
				return new DatasetTestParameterValue(testParameter);
			}

			if (testParameter.Type == typeof(double) ||
			    testParameter.Type == typeof(int) ||
			    testParameter.Type == typeof(bool) ||
			    testParameter.Type == typeof(string) ||
			    testParameter.Type == typeof(DateTime) ||
			    testParameter.Type.IsEnum)
			{
				return new ScalarTestParameterValue(testParameter,
				                                    $"{testParameter.DefaultValue ?? GetDefault(testParameter.Type)}");
			}

			return new ScalarTestParameterValue(testParameter,
			                                    $"{testParameter.DefaultValue ?? GetDefault(testParameter.Type)}");
			//throw new ArgumentException("Unhandled type " + _type);
		}

		[CanBeNull]
		public static object GetDefault([NotNull] Type type)
		{
			if (! type.IsValueType)
			{
				return null;
			}

			object defaultValue = Activator.CreateInstance(type);

			if (type.IsEnum)
			{
				// Ensure valid value for enums: if default value (0) is not in list, return the first enum item value
				if (! Enum.IsDefined(type, defaultValue))
				{
					string[] values = Enum.GetNames(type);
					if (values.Length > 0)
					{
						return values[0];
					}
				}
			}

			return defaultValue;
		}
	}
}
