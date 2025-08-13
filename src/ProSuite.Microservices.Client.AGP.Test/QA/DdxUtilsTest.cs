using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using NUnit.Framework;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.AGP.DataModel;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Ddx;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.Test.QA
{
	[TestFixture]
	public class DdxUtilsTest
	{
		[Test]
		public void CanTransformToFromSpecificationMsg()
		{
			// TODO: Remove once Definitions are added
			string serverBinDir = Environment.GetEnvironmentVariable("GOTOP_SERVER_DIR");
			Assert.IsNotNull(serverBinDir, "GOTOP_SERVER_DIR not set");

			PrivateAssemblyUtils.AddCodeBaseDir(serverBinDir);

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

		[Test]
		public void CanTransformFromToDetailedDataset()
		{
			DdxModel model = CreateModel(out Dataset ds0, out Dataset ds1);

			foreach (ObjectDataset errorDataset in model.Datasets.Where(d => d is IErrorDataset)
			                                            .Cast<ObjectDataset>())
			{
				DatasetMsg datasetMsg = ProtoDataQualityUtils.ToDatasetMsg(errorDataset, true);

				Dataset rehydratedDataset = ProtoDataQualityUtils.FromDatasetMsg(datasetMsg,
					(msg) => new BasicDataset(msg.DatasetId, msg.Name));

				ProSuiteGeometryType shapeType = (ProSuiteGeometryType) datasetMsg.GeometryType;

				// Different approach: Create error dataset
				Func<DatasetMsg, Dataset> errorDatasetFactory = (msg) =>
					ProtoDataQualityUtils.CreateErrorDataset(msg.DatasetId, msg.Name, shapeType);

				ObjectDataset rehydratedErrorDataset = (ObjectDataset)
					ProtoDataQualityUtils.FromDatasetMsg(datasetMsg, errorDatasetFactory);

				ProtoDataQualityUtils.AddDetailsToDataset(rehydratedErrorDataset, datasetMsg);

				AssertEqual(errorDataset, rehydratedErrorDataset);
			}
		}

		private static void AssertEqual(ObjectDataset original, ObjectDataset rehydrated)
		{
			Assert.AreEqual(original.Name, rehydrated.Name);
			Assert.AreEqual(original.Id, rehydrated.Id);
			Assert.AreEqual(original.GeometryType, rehydrated.GeometryType);
			Assert.AreEqual(original.Attributes.Count,
			                rehydrated.Attributes.Count);
			Assert.AreEqual(original.ObjectTypes.Count,
			                rehydrated.ObjectTypes.Count);
			Assert.AreEqual(original.TypeDescription,
			                rehydrated.TypeDescription);
			Assert.AreEqual(original.DisplayName, rehydrated.DisplayName);

			// TODO: Add Abbreviation to DatasetMsg
			//Assert.AreEqual(errorDataset.Abbreviation, rehydratedErrorDataset.Abbreviation);
			Assert.AreEqual(original.AliasName, rehydrated.AliasName);
			Assert.AreEqual(original.DatasetCategory,
			                rehydrated.DatasetCategory);

			foreach (ObjectAttribute a1 in original.Attributes)
			{
				ObjectAttribute a2 = rehydrated.GetAttribute(a1.Name);
				Assert.IsNotNull(a2);
				Assert.AreEqual(a1.Name, a2.Name);
				Assert.AreEqual(a1.FieldType, a2.FieldType);
				Assert.AreEqual(a1.Role, a2.Role);
				Assert.AreEqual(a1.ReadOnly, a2.ReadOnly);
				Assert.AreEqual(a1.IsObjectDefining, a2.IsObjectDefining);
			}

			foreach (ObjectType o1 in original.ObjectTypes)
			{
				ObjectType o2 = rehydrated.GetObjectType(o1.SubtypeCode);

				Assert.IsNotNull(o2);
				Assert.AreEqual(o1.Id, o2.Id);
				Assert.AreEqual(o1.Name, o2.Name);
				Assert.AreEqual(o1.SubtypeCode, o2.SubtypeCode);
				Assert.AreEqual(o1.ObjectSubtypes.Count, o2.ObjectSubtypes.Count);

				foreach (ObjectSubtype o1s in o1.ObjectSubtypes)
				{
					ObjectSubtype o2s = o2.ObjectSubtypes.First(
						s => s.SubtypeCode == o1.SubtypeCode && s.Name == o1s.Name);

					Assert.IsNotNull(o2s);
					Assert.AreEqual(o1s.Name, o2s.Name);
					Assert.AreEqual(o1s.SubtypeCode, o2s.SubtypeCode);

					foreach (ObjectSubtypeCriterion c1 in o1s.Criteria)
					{
						ObjectSubtypeCriterion c2 =
							o2s.Criteria.First(c => c.Attribute.Name.Equals(c1.Attribute.Name));

						Assert.IsNotNull(c2);
						Assert.AreEqual(c1.Attribute.Name, c2.Attribute.Name);
						Assert.AreEqual(c1.AttributeValue, c2.AttributeValue);
						Assert.AreEqual(c1.AttributeValueType, c2.AttributeValueType);
					}
				}
			}
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
			CreateModel(out Dataset ds0, out Dataset ds1);

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

		private static DdxModel CreateModel(out Dataset ds0, out Dataset ds1)
		{
			const string dsName0 = "SCHEMA.DATASET0";
			const string dsName1 = "SCHEMA.DATASET1";

			DdxModel model = new BasicModel(123, "Test Model");
			model.SetCloneId(10);

			var geometryTypePolygon = new GeometryTypeShape("Polygon",
			                                                ProSuiteGeometryType.Polygon);
			var geometryTypeLine = new GeometryTypeShape("Polyline",
			                                             ProSuiteGeometryType.Polyline);
			var geometryTypeMultipoint = new GeometryTypeShape("Multipoint",
			                                                   ProSuiteGeometryType.Multipoint);
			var geometryTypeTable = new GeometryTypeNoGeometry("No Geometry");

			// Model datasets:
			ds0 = model.AddDataset(new BasicDataset(33, dsName0, "dsn0", "Dataset 0")
			                       { GeometryType = geometryTypeLine });
			ds1 = model.AddDataset(new BasicDataset(33, dsName1, "dsn1", "Dataset 1")
			                       { GeometryType = geometryTypeLine });

			ds0.SetCloneId(23);
			ds1.SetCloneId(24);

			// Error datasets:
			var errorPolygonDataset =
				new ErrorPolygonDataset("ERRROR_POLYGON", "errPoly", "Error polygons")
				{
					GeometryType = geometryTypePolygon
				};
			errorPolygonDataset.SetCloneId(25);

			var errorLineDataset =
				new ErrorLineDataset("ERRROR_LINE", "errLine", "Error lines")
				{
					GeometryType = geometryTypeLine
				};

			var errorMultipointDataset =
				new ErrorMultipointDataset("ERRROR_MULTIPOINT", "errMulti", "Error multipoints")
				{
					GeometryType = geometryTypeMultipoint
				};

			var errorTableDataset =
				new ErrorTableDataset("ERRROR_TABLE", "errTable", "Error tables")
				{
					GeometryType = geometryTypeTable
				};

			ObjectDataset[] errorDatasets =
			{
				errorPolygonDataset, errorLineDataset, errorMultipointDataset, errorTableDataset
			};

			foreach (var errorDataset in errorDatasets)
			{
				errorDataset.AddAttribute(
					new ObjectAttribute("OBJECTID", FieldType.ObjectID,
					                    new ObjectAttributeType(AttributeRole.ObjectID))
					{
						ReadOnly = true,
						IsObjectDefining = true
					});

				errorDataset.AddAttribute(
					new ObjectAttribute("UUID", FieldType.Guid,
					                    new ObjectAttributeType(AttributeRole.UUID))
					{
						ReadOnly = true,
						IsObjectDefining = true
					});

				errorDataset.AddAttribute(
					new ObjectAttribute("DateOfChange", FieldType.Date,
					                    new ObjectAttributeType(AttributeRole.DateOfChange)));

				errorDataset.AddAttribute(
					new ObjectAttribute("DateOfCreation", FieldType.Date,
					                    new ObjectAttributeType(AttributeRole.DateOfCreation)));

				errorDataset.AddAttribute(
					new ObjectAttribute("OPERATEUR", FieldType.Text,
					                    new ObjectAttributeType(AttributeRole.Operator)));

				errorDataset.AddAttribute(
					new ObjectAttribute("DESCRIPTION", FieldType.Text,
					                    new ObjectAttributeType(AttributeRole.ErrorDescription)));

				ObjectAttribute errorType = errorDataset.AddAttribute(
					new ObjectAttribute("ERROR_TYPE", FieldType.Text,
					                    new ObjectAttributeType(AttributeRole.ErrorErrorType)));

				errorDataset.AddAttribute(
					new ObjectAttribute("CONDITION_ID", FieldType.Integer,
					                    new ObjectAttributeType(AttributeRole.ErrorConditionId)));

				// Just for testing:
				ObjectType objectType1 = errorDataset.AddObjectType(1, "TYPE1");
				ObjectType objectType2 = errorDataset.AddObjectType(2, "TYPE2");

				objectType1.AddObjectSubType("SUB_TYPE", errorType, 200);
			}

			model.AddDataset(errorPolygonDataset);
			model.AddDataset(errorLineDataset);
			model.AddDataset(errorMultipointDataset);
			model.AddDataset(errorTableDataset);

			return model;
		}
	}
}
