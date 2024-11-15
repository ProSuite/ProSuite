using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Core;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public class ProtoBasedQualitySpecificationFactory : ProtoBasedQualitySpecificationFactoryBase
	{
		public ProtoBasedQualitySpecificationFactory(
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] ISupportedInstanceDescriptors instanceDescriptors)
			: base(instanceDescriptors)
		{
			ModelsByWorkspaceId = modelsByWorkspaceId;
		}

		#region Overrides of ProtoBasedQualitySpecificationFactoryBase

		protected override IDictionary<string, DdxModel> GetModelsByWorkspaceId(
			ConditionListSpecificationMsg conditionListSpecificationMsg)
		{
			// Not (yet) supported from Pro, must be provided through constructor
			throw new NotImplementedException();
		}

		protected override IInstanceInfo CreateInstanceFactory<T>(T created)
		{
			Assert.ArgumentNotNull(created, nameof(created));

			InstanceDescriptor instanceDescriptor = created.InstanceDescriptor;

			if (instanceDescriptor == null)
			{
				return null;
			}

			return InstanceDescriptorUtils.GetInstanceInfo(instanceDescriptor, true);
		}

		protected override TestParameterValue CreateEmptyTestParameterValue<T>(
			TestParameter testParameter)
		{
			TestParameterValue parameterValue = GetEmptyParameterValue(testParameter);

			return parameterValue;
		}

		protected override void AssertValidDataset(TestParameter testParameter, Dataset dataset)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			if (dataset == null) return;

			bool isDataset = IsDatasetType(testParameter.Type);

			// TODO:
			//Assert.

			//TestParameterType parameterType = GetParameterType(testParameter.Type);

			//switch (parameterType)
			//{
			//	case TestParameterType.Dataset:
			//		return true;

			//	case TestParameterType.ObjectDataset:
			//		return dataset is ObjectDataset;

			//	case TestParameterType.VectorDataset:
			//		return dataset is VectorDataset;

			//	case TestParameterType.TableDataset:
			//		return dataset is TableDataset;

			//	case TestParameterType.TopologyDataset:
			//		return dataset is TopologyDataset;

			//	case TestParameterType.TerrainDataset:
			//		return dataset is ISimpleTerrainDataset;

			//	case TestParameterType.GeometricNetworkDataset:
			//		return dataset is IGeometricNetworkDataset;

			//	case TestParameterType.RasterMosaicDataset:
			//		return dataset is IRasterMosaicDataset;

			//	case TestParameterType.RasterDataset:
			//		return dataset is RasterDataset;

			//	default:
			//		throw new ArgumentException(
			//			string.Format("Unsupported parameter type: {0}",
			//			              Enum.GetName(typeof(TestParameterType), parameterType)));
			//}
		}

		#endregion

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

		public static bool IsDatasetType([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (type.IsValueType)
			{
				return false;
			}

			string typeName = type.Name;

			//ToDo: Use only typeof without legacy types
			bool isDataset =
				typeName == "IReadOnlyFeatureClass" ||
				typeName == "IReadOnlyTable" ||
				typeName == "IFeatureClass" ||
				typeName == "ITable" ||
				typeName == "IObjectClass" ||
				typeName == "ITopology" ||
				typeName == "IRasterDataset" ||
				typeName == "IRasterDataset2" ||
				typeName == "IMosaicDataset" ||
				typeName == "TerrainReference" ||
				typeName == "TopologyReference" ||
				typeName == "SimpleRasterMosaic" ||
				typeName == "MosaicRasterReference" ||
				typeName == "RasterDatasetReference" ||
				typeName == "IMosaicLayer" ||
				typeName == "ITerrain" ||
				typeName == "IGeometricNetwork" ||
				typeName == "IFeatureClassSchemaDef" ||
				typeName == "ITableSchemaDef" ||
				typeName == "ITopologyDef" ||
				typeName == "IRasterDatasetDef" ||
				typeName == "IMosaicRasterDatasetDef" ||
				typeName == "ITerrainDef";

			return isDataset;
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
