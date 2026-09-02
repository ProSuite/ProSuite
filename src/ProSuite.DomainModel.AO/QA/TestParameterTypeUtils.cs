#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public static class TestParameterTypeUtils
	{
		private static bool _registered;

		static TestParameterTypeUtils()
		{
			// Belt-and-braces: also register on first access to this type. The authoritative
			// registration is EnsureRegistered, called from the composition roots that load
			// DomainModel.AO (DDX Editor launcher, microservices server startup), so that the
			// legacy ArcObjects mapping is present before any core TestParameterTypes call.
			EnsureRegistered();
		}

		/// <summary>
		/// Registers the ArcObjects-specific (legacy) type mapping as a fallback for the
		/// platform-independent mapping in <see cref="TestParameterTypes"/>. Idempotent: safe to
		/// call multiple times and from multiple composition roots. Must be called early during
		/// startup so that core mappings resolve legacy AO dataset types correctly regardless of
		/// which code path first touches <see cref="TestParameterTypes"/>.
		/// </summary>
		public static void EnsureRegistered()
		{
			if (_registered)
			{
				return;
			}

			TestParameterTypes.RegisterTypeMapping(GetLegacyArcObjectsParameterType);
			_registered = true;
		}

		[CanBeNull]
		private static TestParameterType? GetLegacyArcObjectsParameterType(
			[NotNull] Type dataType)
		{
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
			if (typeof(PointCloudReference).IsAssignableFrom(dataType))
				return TestParameterType.PointCloudDataset;
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

			return null;
		}

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
			// Delegates to the AO-free core mapping (schema-def interfaces + scalars), which in
			// turn falls back to GetLegacyArcObjectsParameterType for the ArcObjects-specific types.
			return TestParameterTypes.GetParameterType(dataType);
		}

		/// <summary>
		/// Returns true if the two types are compatible for test parameter assignment,
		/// i.e. they represent the same logical parameter type category. This handles the case
		/// where a stored parameter value uses a legacy AO type (e.g. <see cref="IReadOnlyTable"/>)
		/// but the current instance info uses an equivalent definition type (e.g.
		/// <see cref="ITableSchemaDef"/>), which can occur when a test is instantiated via an
		/// AlgorithmDefinition instead of the original ClassDescriptor.
		/// </summary>
		public static bool AreCompatibleParameterTypes([NotNull] Type type1, [NotNull] Type type2)
		{
			if (type1 == type2)
				return true;

			if (! IsDatasetType(type1) || ! IsDatasetType(type2))
				return false;

			return GetParameterType(type1) == GetParameterType(type2);
		}

		public static bool IsDatasetType([NotNull] Type type)
		{
			// Delegates to the AO-free core mapping, which resolves legacy ArcObjects dataset
			// types (feature class, table, raster, topology, terrain, ...) via the fallback
			// registered by EnsureRegistered. Deriving both this and GetParameterType from the
			// same resolver guarantees they cannot disagree.
			return TestParameterTypes.IsDatasetType(type);
		}

		public static bool IsValidDataset(TestParameterType parameterType,
		                                  [NotNull] Dataset dataset)
		{
			return TestParameterTypes.IsValidDataset(parameterType, dataset);
		}

		[NotNull]
		public static TestParameterValue GetEmptyParameterValue(
			[NotNull] TestParameter testParameter)
		{
			return TestParameterTypes.CreateEmptyParameterValue(testParameter);
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

		public static SqlExpressionAttribute GetSqlExpressionAttribute(
			[NotNull] Type instanceType,
			[NotNull] TestParameter testParameter)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			PropertyInfo propertyInfo =
				instanceType.GetProperties()
				            .FirstOrDefault(p => p.Name.Equals(
					                            testParameter.Name,
					                            StringComparison.InvariantCultureIgnoreCase));

			if (propertyInfo == null)
			{
				return null;
			}

			return ReflectionUtils.GetAttribute<SqlExpressionAttribute>(propertyInfo);
		}
	}
}
