using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Exceptions;
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

		/// <summary>
		/// Reproduces the Topgis QA server failure of 28.10.2026 (GC_BEDROCK, 135729 rows): the
		/// GoTop client splits a large result into several batches, but the Topgis
		/// BackgroundVerificationInputs.SetRemoteDataAccess drops the function that reads the
		/// next batch, so the remote dataset fails after the first batch.
		/// </summary>
		[Test]
		public void Search_without_read_next_batch_function_fails_on_second_client_batch()
		{
			RemoteDataset remoteDataset = CreateTwoBatchRemoteDataset();

			remoteDataset.ReadNextBatch = null;

			var exception = Assert.Throws<Commons.Essentials.Assertions.AssertionException>(
				() => remoteDataset.Search(new AoTableFilter(), recycling: false).ToList());

			StringAssert.Contains("The client signalled more data but no read-next-batch",
			                      exception.Message);
		}

		[Test]
		public void Search_with_read_next_batch_function_yields_rows_of_all_client_batches()
		{
			RemoteDataset remoteDataset = CreateTwoBatchRemoteDataset();

			List<VirtualRow> rows =
				remoteDataset.Search(new AoTableFilter(), recycling: false).ToList();

			CollectionAssert.AreEqual(new long[] { 1, 2 }, rows.Select(r => r.OID).ToList());
		}

		[Test]
		public void Search_yields_rows_of_three_client_batches()
		{
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			var batchesInFlight = new Queue<DataVerificationRequest>();
			batchesInFlight.Enqueue(CreateBatch(spatialReference, true, 2));
			batchesInFlight.Enqueue(CreateBatch(spatialReference, false, 3));

			RemoteDataset remoteDataset = CreateRemoteDataset(
				spatialReference,
				response => CreateBatch(spatialReference, true, 1),
				() => batchesInFlight.Dequeue());

			List<VirtualRow> rows =
				remoteDataset.Search(new AoTableFilter(), recycling: false).ToList();

			CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, rows.Select(r => r.OID).ToList());
			Assert.AreEqual(0, batchesInFlight.Count);
		}

		[Test]
		public void Search_accepts_empty_last_columnar_batch()
		{
			// The columnar client encoder always ends with a batch that can have 0 rows. Its
			// columns are present but contain no values.
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			var batchesInFlight = new Queue<DataVerificationRequest>();
			batchesInFlight.Enqueue(CreateBatch(spatialReference, false));

			RemoteDataset remoteDataset = CreateRemoteDataset(
				spatialReference,
				response => CreateBatch(spatialReference, true, 1, 2),
				() => batchesInFlight.Dequeue());

			List<VirtualRow> rows =
				remoteDataset.Search(new AoTableFilter(), recycling: false).ToList();

			CollectionAssert.AreEqual(new long[] { 1, 2 }, rows.Select(r => r.OID).ToList());
		}

		[Test]
		public void Search_accepts_empty_last_row_wise_batch()
		{
			// The row-wise client encoder ends with a new GdbData without objects.
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			var batchesInFlight = new Queue<DataVerificationRequest>();
			batchesInFlight.Enqueue(new DataVerificationRequest { Data = new GdbData() });

			RemoteDataset remoteDataset = CreateRemoteDataset(
				spatialReference,
				response => CreateBatch(spatialReference, true, 1),
				() => batchesInFlight.Dequeue());

			List<VirtualRow> rows =
				remoteDataset.Search(new AoTableFilter(), recycling: false).ToList();

			CollectionAssert.AreEqual(new long[] { 1 }, rows.Select(r => r.OID).ToList());
		}

		[Test]
		public void Search_client_error_after_first_batch_names_table()
		{
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			const string clientErrorMessage = "Out of memory reading the second batch.";

			RemoteDataset remoteDataset = CreateRemoteDataset(
				spatialReference,
				response => CreateBatch(spatialReference, true, 1),
				() => new DataVerificationRequest { ErrorMessage = clientErrorMessage });

			var exception = Assert.Throws<DataAccessException>(
				() => remoteDataset.Search(new AoTableFilter(), recycling: false).ToList());

			Console.WriteLine(exception.Message);

			StringAssert.Contains("TABLE_A_TABLE_B_JOIN", exception.Message);
			StringAssert.Contains(clientErrorMessage, exception.Message);
		}

		[Test]
		public void Search_missing_next_batch_throws_clear_error()
		{
			// The read-next-batch function returns null when the client stream ends or fails
			// (see QualityVerificationGrpcImpl.ReadMoreDataAsync).
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			RemoteDataset remoteDataset = CreateRemoteDataset(
				spatialReference,
				response => CreateBatch(spatialReference, true, 1),
				() => null);

			var exception = Assert.Throws<IOException>(
				() => remoteDataset.Search(new AoTableFilter(), recycling: false).ToList());

			Console.WriteLine(exception.Message);

			StringAssert.Contains("TABLE_A_TABLE_B_JOIN", exception.Message);
		}

		[Test]
		public void Search_after_stopped_search_yields_rows_of_its_own_answer()
		{
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			// Simulates the request stream: the client answers each data request by writing
			// all its batches at once, the server reads them one after the other.
			var requestStream = new Queue<DataVerificationRequest>();
			var requestCount = 0;

			RemoteDataset remoteDataset = CreateRemoteDataset(
				spatialReference,
				response =>
				{
					requestCount++;
					int firstObjectId = requestCount * 10 + 1;

					requestStream.Enqueue(
						CreateBatch(spatialReference, true, firstObjectId));
					requestStream.Enqueue(
						CreateBatch(spatialReference, false, firstObjectId + 1));

					return requestStream.Dequeue();
				},
				() => requestStream.Dequeue());

			// The consumer stops during the first batch of the first answer:
			VirtualRow firstRow =
				remoteDataset.Search(new AoTableFilter(), recycling: false).First();

			Assert.AreEqual(11, firstRow.OID);

			List<VirtualRow> rows =
				remoteDataset.Search(new AoTableFilter(), recycling: false).ToList();

			CollectionAssert.AreEqual(new long[] { 21, 22 }, rows.Select(r => r.OID).ToList());
		}

		private static RemoteDataset CreateTwoBatchRemoteDataset()
		{
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			// The client writes all batches in one go; the server requests only the first one
			// and reads the following ones from the stream.
			var batchesInFlight = new Queue<DataVerificationRequest>();
			batchesInFlight.Enqueue(CreateBatch(spatialReference, false, 2));

			return CreateRemoteDataset(
				spatialReference,
				response => CreateBatch(spatialReference, true, 1),
				() => batchesInFlight.Dequeue());
		}

		private static RemoteDataset CreateRemoteDataset(
			ISpatialReference spatialReference,
			Func<DataVerificationResponse, DataVerificationRequest> getRemoteDataFunc,
			Func<DataVerificationRequest> readNextBatchFunc)
		{
			ObjectClassMsg objectClassMsg = CreateQueryFeatureClassMsg(spatialReference);

			Func<ITable, BackingDataset> createBackingDataset = t =>
				new RemoteDataset(
					t,
					getRemoteDataFunc,
					classDefinition: null,
					queryDefinition: CreateRelationshipQuery(),
					readNextBatchFunc: readNextBatchFunc);

			GdbTable table = ProtobufConversionUtils.FromQueryTableMsg(
				objectClassMsg, new WorkspaceMock(), createBackingDataset,
				new List<IReadOnlyTable>());

			return (RemoteDataset) ((GdbFeatureClass) table).BackingDataset;
		}

		private static DataVerificationRequest CreateBatch(ISpatialReference spatialReference,
		                                                   bool hasMoreData,
		                                                   params int[] objectIds)
		{
			var shapeMsgs = objectIds.Select(
				objectId => ProtobufGeometryUtils.ToShapeMsg(
					GeometryFactory.CreatePoint(2600000 + objectId, 1200000, spatialReference),
					ShapeMsg.FormatOneofCase.EsriShape,
					SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml));

			var columnarData = new ColumnarGdbObjects { RowCount = objectIds.Length };
			columnarData.Columns.Add(new Column
			                         {
				                         Name = "TABLE_A.OBJECTID",
				                         Type = WireFieldType.FieldTypeInt32,
				                         IntValues = new Int32Column { Values = { objectIds } }
			                         });
			columnarData.Columns.Add(new Column
			                         {
				                         Name = "TABLE_A.SHAPE",
				                         Type = WireFieldType.FieldTypeGeometry,
				                         Geometries = new GeometryColumn { Shapes = { shapeMsgs } }
			                         });

			return new DataVerificationRequest
			       {
				       Data = new GdbData
				              {
					              GdbColumnarData = columnarData,
					              GdbObjectCount = objectIds.Length,
					              HasMoreData = hasMoreData
				              }
			       };
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

		[Test]
		public void Search_client_error_names_table_and_client_message()
		{
			// Issue C of the QA server issue list: the server message lacked the table name
			// and doubled the period ("Specified cast is not valid..").
			const string clientErrorMessage =
				"Specified cast is not valid.\r\n" +
				"Client exception: System.InvalidCastException: Specified cast is not valid.";

			RemoteDataset remoteDataset = null;

			var errorTable = new GdbTable(
				417, "TOPGIS_TLM.TLM_ERRORS_ROW", null,
				t =>
				{
					remoteDataset = new RemoteDataset(
						t,
						response => new DataVerificationRequest
						            {
							            ErrorMessage = clientErrorMessage
						            },
						new ClassDef
						{
							ClassHandle = 417,
							WorkspaceHandle = 2000030
						});

					return remoteDataset;
				});

			errorTable.AddField(FieldUtils.CreateOIDField());

			Assert.NotNull(remoteDataset);

			var filter = new AoTableFilter
			             {
				             WhereClause = "FEHLERART = 300",
				             SubFields = "*"
			             };

			var exception = Assert.Throws<DataAccessException>(
				() => remoteDataset.Search(filter, recycling: false).ToList());

			string message = exception.Message;
			Console.WriteLine(message);

			StringAssert.Contains("TOPGIS_TLM.TLM_ERRORS_ROW", message);
			StringAssert.Contains(clientErrorMessage, message);
			StringAssert.Contains("FEHLERART = 300", message);
			StringAssert.DoesNotContain("valid..", message);
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
