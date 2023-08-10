using System.Collections.Generic;
using Google.Protobuf.Collections;
using NUnit.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.DomainModel.AGP.DataModel;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.Test.QA
{
	[TestFixture]
	public class DdxUtilsTest
	{
		[Test]
		public void CanTransformToFromSpecificationMsg()
		{
			QualitySpecification qualitySpecification = CreateQualitySpecification();

			//
			// Pack:
			ConditionListSpecificationMsg specificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					qualitySpecification, null, out var modelsById);

			GetSpecificationResponse getSpecificationResponse = new GetSpecificationResponse()
			                                                    {
				                                                    Specification = specificationMsg
			                                                    };

			getSpecificationResponse.ReferencedInstanceDescriptors.AddRange(
				ProtoDataQualityUtils.GetInstanceDescriptorMsgs(qualitySpecification));

			RepeatedField<DatasetMsg> referencedDatasets =
				getSpecificationResponse.ReferencedDatasets;

			foreach (DdxModel model in modelsById.Values)
			{
				ModelMsg modelMsg = ToModelMsg(model, referencedDatasets);
				getSpecificationResponse.ReferencedModels.Add(modelMsg);
			}

			//
			// Unpack:
			QualitySpecification rehydratedSpecification =
				DdxUtils.CreateQualitySpecification(getSpecificationResponse);

			Assert.AreEqual(qualitySpecification.Name, rehydratedSpecification.Name);

			Assert.AreEqual(qualitySpecification.Elements.Count,
			                rehydratedSpecification.Elements.Count);

			Assert.AreEqual(0, qualitySpecification.Compare(rehydratedSpecification));
		}

		private static ModelMsg ToModelMsg(DdxModel model,
		                                   ICollection<DatasetMsg> referencedDatasetMsgs)
		{
			SpatialReferenceMsg srWkId = new SpatialReferenceMsg()
			                             {
				                             SpatialReferenceWkid = 2056
			                             };

			ModelMsg modelMsg =
				ProtoDataQualityUtils.ToDdxModelMsg(model, srWkId, referencedDatasetMsgs);

			return modelMsg;
		}

		private static QualitySpecification CreateQualitySpecification()
		{
			const string dsName0 = "SCHEMA.TLM_DATASET0";
			const string dsName1 = "SCHEMA.TLM_DATASET1";

			DdxModel m = new BasicModel(123, "Test Model");
			m.SetCloneId(10);

			GeometryType geometryTypePolyline =
				new GeometryTypeShape("Polyline", ProSuiteGeometryType.Polyline);
			Dataset ds0 = m.AddDataset(new BasicDataset(33, dsName0, "dsn0", "Dataset 0")
			                           { GeometryType = geometryTypePolyline });
			Dataset ds1 = m.AddDataset(new BasicDataset(33, dsName1, "dsn1", "Dataset 1")
			                           { GeometryType = geometryTypePolyline });

			((IEntityTest) ds0).SetId(23);
			((IEntityTest) ds0).SetId(24);

			// spec 0: Contains both a direct dataset reference and a transformer
			var spec0 = new QualitySpecification("spec0");

			var qaMinLength = new TestDescriptor(
				"name",
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaMinLength",
					"ProSuite.QA.Tests"), 0, false, false);

			var transformerDescriptor = new TransformerDescriptor(
				"transformer",
				new ClassDescriptor("ProSuite.QA.Tests.Transformers.TrFootprint",
				                    "ProSuite.QA.Tests"), 0);

			var transformerConfig1 =
				new TransformerConfiguration("footprint1", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig1, "multipatchClass",
			                                             ds0);

			var transformerConfig2 =
				new TransformerConfiguration("footprint2", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig2, "multipatchClass",
			                                             transformerConfig1);

			var transformerConfig3 =
				new TransformerConfiguration("footprint3", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig3, "multipatchClass",
			                                             transformerConfig2);

			var transformerConfig4 =
				new TransformerConfiguration("footprint4", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig4, "multipatchClass",
			                                             transformerConfig3);

			var cond0 = new QualityCondition("cond0", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond0, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond0, "featureClass", transformerConfig4);

			var cond1 = new QualityCondition("cond1", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond1, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond1, "featureClass", ds1);

			spec0.AddElement(cond0);
			spec0.AddElement(cond1);
			return spec0;
		}
	}
}
