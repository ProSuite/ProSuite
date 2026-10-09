using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geodatabase;

namespace ProSuite.Microservices.Server.AO.Test.Geodatabase
{
	[TestFixture]
	public class RemoteDatasetTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void FromQueryTableMsg_schema_and_search_use_qualified_shape_field_name_exactly()
		{
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			ObjectClassMsg objectClassMsg = CreateQueryFeatureClassMsg(spatialReference);

			DataRequest capturedRequest = null;
			RemoteDataset remoteDataset = null;

			Func<ITable, BackingDataset> createBackingDataset = t =>
			{
				remoteDataset = new RemoteDataset(
					t,
					response =>
					{
						capturedRequest = response.DataRequest;

						return new DataVerificationRequest
						       {
							       Data = new GdbData
							              {
								              GdbColumnarData = new ColumnarGdbObjects { RowCount = 0 }
							              }
						       };
					},
					classDefinition: null,
					queryDefinition: CreateRelationshipQuery());

				return remoteDataset;
			};

			GdbTable table = ProtobufConversionUtils.FromQueryTableMsg(
				objectClassMsg, new WorkspaceMock(), createBackingDataset,
				new List<IReadOnlyTable>());

			var featureClass = (GdbFeatureClass) table;

			// Schema contract: the shape field keeps its fully qualified name, exactly.
			Assert.AreEqual("TABLE_A.SHAPE", featureClass.ShapeFieldName);
			Assert.GreaterOrEqual(featureClass.FindField("TABLE_A.SHAPE"), 0);

			var filter = new AoTableFilter
			             {
				             SubFields = "TABLE_A.OBJECTID,TABLE_A.SHAPE"
			             };

			List<VirtualRow> rows = remoteDataset.Search(filter, recycling: false).ToList();

			Assert.AreEqual(0, rows.Count);
			Assert.NotNull(capturedRequest);

			List<string> requestedFields =
				capturedRequest.SubFields.Split(',').ToList();

			CollectionAssert.AreEquivalent(
				new[] { "TABLE_A.OBJECTID", "TABLE_A.SHAPE" }, requestedFields);
			Assert.AreEqual(1, requestedFields.Count(f => f == "TABLE_A.SHAPE"),
			                "The shape field must not be duplicated in the outgoing request.");
		}

		[Test]
		public void Search_maps_columnar_response_with_exact_qualified_shape_field_name()
		{
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			ObjectClassMsg objectClassMsg = CreateQueryFeatureClassMsg(spatialReference);

			IPoint point = GeometryFactory.CreatePoint(2600000, 1200000, spatialReference);

			ShapeMsg shapeMsg = ProtobufGeometryUtils.ToShapeMsg(
				point, ShapeMsg.FormatOneofCase.EsriShape,
				SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml);

			var columnarData = new ColumnarGdbObjects { RowCount = 1 };
			columnarData.Columns.Add(new Column
			                         {
				                         Name = "TABLE_A.OBJECTID",
				                         Type = WireFieldType.FieldTypeInt32,
				                         IntValues = new Int32Column { Values = { 1 } }
			                         });
			columnarData.Columns.Add(new Column
			                         {
				                         Name = "TABLE_A.NAME",
				                         Type = WireFieldType.FieldTypeString,
				                         StringValues = new StringColumn { Values = { "Foo" } }
			                         });
			columnarData.Columns.Add(new Column
			                         {
				                         Name = "TABLE_A.SHAPE",
				                         Type = WireFieldType.FieldTypeGeometry,
				                         Geometries = new GeometryColumn { Shapes = { shapeMsg } }
			                         });

			Func<ITable, BackingDataset> createBackingDataset = t =>
				new RemoteDataset(
					t,
					response => new DataVerificationRequest
					            {
						            Data = new GdbData
						                   {
							                   GdbColumnarData = columnarData,
							                   GdbObjectCount = 1
						                   }
					            },
					classDefinition: null,
					queryDefinition: CreateRelationshipQuery());

			GdbTable table = ProtobufConversionUtils.FromQueryTableMsg(
				objectClassMsg, new WorkspaceMock(), createBackingDataset,
				new List<IReadOnlyTable>());

			var remoteDataset = (RemoteDataset) ((GdbFeatureClass) table).BackingDataset;

			var filter = new AoTableFilter { SubFields = "*" };

			List<VirtualRow> rows = remoteDataset.Search(filter, recycling: false).ToList();

			Assert.AreEqual(1, rows.Count);

			var feature = (GdbFeature) rows[0];

			Assert.NotNull(feature.Shape);
			Assert.IsTrue(GeometryUtils.AreEqual(point, feature.Shape));
		}

		private static ObjectClassMsg CreateQueryFeatureClassMsg(
			ISpatialReference spatialReference)
		{
			var objectClassMsg = new ObjectClassMsg
			                     {
				                     ClassHandle = 456,
				                     Name = "TABLE_A_TABLE_B_JOIN",
				                     GeometryType = (int) esriGeometryType.esriGeometryPoint,
				                     SpatialReference = ProtobufGeometryUtils.ToSpatialReferenceMsg(
					                     spatialReference,
					                     SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml)
			                     };

			objectClassMsg.Fields.Add(new FieldMsg
			                          {
				                          Name = "TABLE_A.OBJECTID",
				                          Type = (int) esriFieldType.esriFieldTypeOID
			                          });
			objectClassMsg.Fields.Add(new FieldMsg
			                          {
				                          Name = "TABLE_A.NAME",
				                          Type = (int) esriFieldType.esriFieldTypeString,
				                          Length = 50
			                          });
			objectClassMsg.Fields.Add(new FieldMsg
			                          {
				                          Name = "TABLE_B.OBJECTID",
				                          Type = (int) esriFieldType.esriFieldTypeInteger
			                          });
			objectClassMsg.Fields.Add(new FieldMsg
			                          {
				                          Name = "TABLE_B.KIND",
				                          Type = (int) esriFieldType.esriFieldTypeInteger
			                          });
			objectClassMsg.Fields.Add(new FieldMsg
			                          {
				                          Name = "TABLE_A.SHAPE",
				                          Type = (int) esriFieldType.esriFieldTypeGeometry
			                          });

			return objectClassMsg;
		}

		private static RelationshipClassQuery CreateRelationshipQuery()
		{
			return new RelationshipClassQuery
			       {
				       RelationshipClassName = "TOPGIS_TLM.TABLE_A_TABLE_B",
				       WorkspaceHandle = 2000030
			       };
		}
	}
}
