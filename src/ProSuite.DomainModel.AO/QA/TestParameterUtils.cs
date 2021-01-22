using System;
using ESRI.ArcGIS.DatasourcesRaster;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public static class TestParameter_Utils
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

		[Obsolete("TODO: Handle Topology / Terrain / ...")]
		public static TestParameterType GetParameterType([NotNull] Type dataType)
		{
			Assert.ArgumentNotNull(dataType, nameof(dataType));

			// NOTE: test more specific types first, base types last

			if (typeof(IFeatureClass).IsAssignableFrom(dataType))
				return TestParameterType.VectorDataset;
			if (typeof(ITable).IsAssignableFrom(dataType))
				return TestParameterType.ObjectDataset;
			if (typeof(IObjectClass).IsAssignableFrom(dataType))
				return TestParameterType.ObjectDataset;

			if (typeof(IMosaicDataset).IsAssignableFrom(dataType))
				return TestParameterType.RasterMosaicDataset;
			if (typeof(IRasterDataset).IsAssignableFrom(dataType))
				return TestParameterType.RasterDataset;

			if (typeof(IDataset).IsAssignableFrom(dataType))
			{
				// Topology, Terrain, Geometric Network, Mosaic
				return TestParameterType.Unknown;
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

		[Obsolete("TODO: Handle Topology / Terrain / ...")]
		public static bool IsDatasetType([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (type.IsValueType)
			{
				return false;
			}

			return typeof(IDataset).IsAssignableFrom(type);

			return typeof(IFeatureClass).IsAssignableFrom(type) ||
			       typeof(ITable).IsAssignableFrom(type) ||
			       typeof(IObjectClass).IsAssignableFrom(type) ||
			       //typeof(ITopology).IsAssignableFrom(type) ||
			       //typeof(IGeometricNetwork).IsAssignableFrom(type) ||
			       //			       typeof(ITerrain).IsAssignableFrom(type) ||
			       typeof(IRasterDataset).IsAssignableFrom(type) ||
			       typeof(IRasterDataset2).IsAssignableFrom(type)
				//		       typeof(IMosaicDataset).IsAssignableFrom(type) ||
				//	       typeof(IMosaicLayer).IsAssignableFrom(type)
				;
		}

		[Obsolete("TODO: Handle Topology / Terrain / ...")]
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

				// TODO: Handle Topology / Terrain / ...
				//case TestParameterType.TopologyDataset:
				//	return dataset is TopologyDataset;

				//case TestParameterType.TerrainDataset:
				//	return dataset is TerrainDataset;

				//case TestParameterType.GeometricNetworkDataset:
				//	return dataset is GeometricNetworkDataset;

				//case TestParameterType.RasterMosaicDataset:
				//	return dataset is RasterMosaicDataset;

				//case TestParameterType.RasterDataset:
				//	return dataset is RasterDataset;

				default:
					throw new ArgumentException(
						string.Format("Unsupported parameter type: {0}",
						              Enum.GetName(typeof(TestParameterType), parameterType)));
			}
		}

		[Obsolete("TODO: Handle Topology / Terrain / ...")]
		[CLSCompliant(false)]
		[CanBeNull]
		public static object OpenDataset([NotNull] Dataset dataset,
		                                 //		                                 [NotNull] IDatasetContextEx datasetContext,
		                                 [NotNull] IDatasetContext datasetContext,
		                                 [NotNull] Type dataType)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(dataType, nameof(dataType));

			if (typeof(IFeatureClass) == dataType)
				return datasetContext.OpenFeatureClass((IVectorDataset) dataset);

			if (typeof(ITable) == dataType)
				return datasetContext.OpenTable((IObjectDataset) dataset);

			//if (typeof(ITopology) == dataType)
			//	return datasetContext.OpenTopology((ITopologyDataset) dataset);

			//if (typeof(ITerrain) == dataType)
			//	return datasetContext.OpenTerrain((ITerrainDataset) dataset);

			//if (typeof(IGeometricNetwork) == dataType)
			//	return datasetContext.OpenGeometricNetwork((IGeometricNetworkDataset) dataset);

			//if (typeof(IMosaicDataset) == dataType)
			//	return (IMosaicDataset) datasetContext.OpenRasterDataset(
			//		(IDdxRasterDataset) dataset);

			if (typeof(IRasterDataset) == dataType)
				return datasetContext.OpenRasterDataset((IDdxRasterDataset) dataset);

			if (typeof(IRasterDataset2) == dataType)
				return (IRasterDataset2) datasetContext.OpenRasterDataset(
					(IDdxRasterDataset) dataset);

			//if (typeof(IMosaicLayer) == dataType)
			//	return datasetContext.OpenMosaicLayer((IRasterMosaicDataset) dataset);

			throw new ArgumentException($"Unsupported data type {dataType}");
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
		private static object GetDefault([NotNull] Type type)
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
