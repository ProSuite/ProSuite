using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Testing;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class TableJoinUtilsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void ReproTestIncorrectFgdbLeftJoin()
		{
			string dbPath = TestDataPreparer.ExtractZip("TableJoinUtilsTest.gdb.zip")
			                                .Overwrite()
			                                .GetPath();

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);
			IFeatureClass baseFeatureClass = workspace.OpenFeatureClass("Streets");

			IQueryDef queryDef = workspace.CreateQueryDef();

			queryDef.Tables =
				"Streets LEFT JOIN Rel_Streets_Routes ON Streets.OBJECTID = Rel_Streets_Routes.STREET_OID LEFT JOIN Routes ON Rel_Streets_Routes.ROUTE_OID = Routes.OBJECTID";
			//queryDef.SubFields =
			//	"Streets.OBJTYPE,Streets.USERNAME,Routes.OBJECTID,Routes.NAME,Routes.OBJTYPE,Rel_Streets_Routes.RID,Rel_Streets_Routes.STREET_OID,Rel_Streets_Routes.ROUTE_OID,Streets.OBJECTID,Streets.SHAPE";
			//queryDef.SubFields = "*";

			var queryName = new FeatureQueryNameClass
			                {
				                ShapeFieldName = "Streets.SHAPE",
				                ShapeType = esriGeometryType.esriGeometryPolyline,
				                QueryDef = queryDef,
				                Name = "join",
				                CopyLocally = false,
				                WorkspaceName = (IWorkspaceName) ((IDataset) workspace).FullName,
				                // not unique, but NOT NULL in result:
				                PrimaryKey = "Streets.OBJECTID"
			                };

			var joinedFeatureClass = (IFeatureClass) queryName.Open();

			//string queryNamePkAfterOpen =
			//	((IQueryName2) ((IDataset) joinedFeatureClass).FullName).PrimaryKey;

			//// side issue (already reported separately): queryname primary key is not used when explicit subfield list is specified
			//// Note: this issue is not related to the 
			//Console.WriteLine($@"QueryName primary key after open: {queryNamePkAfterOpen}");
			//Console.WriteLine(
			//	$@"Original QueryName instance primary key after open: {queryName.PrimaryKey}");
			//Console.WriteLine($@"OID field name: {joinedFeatureClass.OIDFieldName}");

			// Incorrect result: street features with oids 1,2,3 are missing from query result
			Console.WriteLine(ToString((ITable) joinedFeatureClass));

			// expected: all street features are included in the result (LEFT join)
			AssertOidsComplete(baseFeatureClass, (ITable) joinedFeatureClass,
			                   expectedMissingBaseClassRowsCount: 0);
		}

		private static string ToString(ITable table)
		{
			int fieldcount = table.Fields.FieldCount;
			var sb = new StringBuilder();
			for (var i = 0; i < fieldcount; i++)
			{
				sb.Append(table.Fields.Field[i].Name);
				sb.Append(";");
			}

			sb.AppendLine();

			ICursor cursor = table.Search(null, Recycling: true);
			try
			{
				IRow row = cursor.NextRow();

				while (row != null)
				{
					for (var i = 0; i < fieldcount; i++)
					{
						object value = row.Value[i];
						if (value is IGeometry)
						{
							sb.Append("[shape];");
						}
						else
						{
							sb.Append($"{value};");
						}
					}

					sb.AppendLine();

					row = cursor.NextRow();
				}
			}
			finally
			{
				Marshal.ReleaseComObject(cursor);
			}

			return sb.ToString();
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(TestCategory.Repro)]
		public void ReproTestJoinCrash()
		{
			IFeatureWorkspace workspace = OpenTestWorkspace();

			IQueryDef queryDef = workspace.CreateQueryDef();

			// OBSERVATION: if GELAENDENAME.OBJECTID comes FIRST in subfield list, it is used as Primary Key field

			queryDef.Tables = "TOPGIS_TLM.TLM_GELAENDENAME, TOPGIS_TLM.TLM_NAMEN_NAME";
			queryDef.SubFields =
				"TOPGIS_TLM.TLM_NAMEN_NAME.OBJECTID,TOPGIS_TLM.TLM_NAMEN_NAME.UUID,TOPGIS_TLM.TLM_NAMEN_NAME.OPERATEUR,TOPGIS_TLM.TLM_NAMEN_NAME.DATUM_ERSTELLUNG,TOPGIS_TLM.TLM_NAMEN_NAME.DATUM_AENDERUNG,TOPGIS_TLM.TLM_NAMEN_NAME.HERKUNFT,TOPGIS_TLM.TLM_NAMEN_NAME.HERKUNFT_JAHR,TOPGIS_TLM.TLM_NAMEN_NAME.HERKUNFT_MONAT,TOPGIS_TLM.TLM_NAMEN_NAME.ERSTELLUNG_JAHR,TOPGIS_TLM.TLM_NAMEN_NAME.ERSTELLUNG_MONAT,TOPGIS_TLM.TLM_NAMEN_NAME.REVISION_JAHR,TOPGIS_TLM.TLM_NAMEN_NAME.REVISION_MONAT,TOPGIS_TLM.TLM_NAMEN_NAME.GRUND_AENDERUNG,TOPGIS_TLM.TLM_NAMEN_NAME.NAME,TOPGIS_TLM.TLM_NAMEN_NAME.SPRACHCODE,TOPGIS_TLM.TLM_NAMEN_NAME.NAME_KOMPLETT,TOPGIS_TLM.TLM_NAMEN_NAME.NAME_TECHNISCH,TOPGIS_TLM.TLM_NAMEN_NAME.NAMEN_TYP,TOPGIS_TLM.TLM_NAMEN_NAME.QUELLSYSTEM_OBJECT_ID,TOPGIS_TLM.TLM_NAMEN_NAME.RC_ID,TOPGIS_TLM.TLM_NAMEN_NAME.WU_ID,TOPGIS_TLM.TLM_NAMEN_NAME.RC_ID_CREATION,TOPGIS_TLM.TLM_NAMEN_NAME.WU_ID_CREATION,TOPGIS_TLM.TLM_NAMEN_NAME.REVISION_QUALITAET,TOPGIS_TLM.TLM_NAMEN_NAME.ORIGINAL_HERKUNFT,TOPGIS_TLM.TLM_NAMEN_NAME.FELD_BEARBEITUNG,TOPGIS_TLM.TLM_NAMEN_NAME.INTEGRATION_OBJECT_UUID,TOPGIS_TLM.TLM_GELAENDENAME.UUID,TOPGIS_TLM.TLM_GELAENDENAME.OPERATEUR,TOPGIS_TLM.TLM_GELAENDENAME.DATUM_AENDERUNG,TOPGIS_TLM.TLM_GELAENDENAME.DATUM_ERSTELLUNG,TOPGIS_TLM.TLM_GELAENDENAME.ERSTELLUNG_JAHR,TOPGIS_TLM.TLM_GELAENDENAME.ERSTELLUNG_MONAT,TOPGIS_TLM.TLM_GELAENDENAME.REVISION_JAHR,TOPGIS_TLM.TLM_GELAENDENAME.REVISION_MONAT,TOPGIS_TLM.TLM_GELAENDENAME.GRUND_AENDERUNG,TOPGIS_TLM.TLM_GELAENDENAME.HERKUNFT,TOPGIS_TLM.TLM_GELAENDENAME.HERKUNFT_JAHR,TOPGIS_TLM.TLM_GELAENDENAME.HERKUNFT_MONAT,TOPGIS_TLM.TLM_GELAENDENAME.OBJEKTART,TOPGIS_TLM.TLM_GELAENDENAME.TLM_NAMEN_NAME_UUID,TOPGIS_TLM.TLM_GELAENDENAME.MASSSTAB,TOPGIS_TLM.TLM_GELAENDENAME.RC_ID,TOPGIS_TLM.TLM_GELAENDENAME.WU_ID,TOPGIS_TLM.TLM_GELAENDENAME.RC_ID_CREATION,TOPGIS_TLM.TLM_GELAENDENAME.WU_ID_CREATION,TOPGIS_TLM.TLM_GELAENDENAME.REVISION_QUALITAET,TOPGIS_TLM.TLM_GELAENDENAME.ORIGINAL_HERKUNFT,TOPGIS_TLM.TLM_GELAENDENAME.FELD_BEARBEITUNG,TOPGIS_TLM.TLM_GELAENDENAME.INTEGRATION_OBJECT_UUID,TOPGIS_TLM.TLM_GELAENDENAME.OBJECTID,TOPGIS_TLM.TLM_GELAENDENAME.SHAPE";
			queryDef.WhereClause =
				"TOPGIS_TLM.TLM_NAMEN_NAME.UUID (+) = TOPGIS_TLM.TLM_GELAENDENAME.TLM_NAMEN_NAME_UUID";
			//queryDef.WhereClause = "TOPGIS_TLM.TLM_NAMEN_NAME.UUID = TOPGIS_TLM.TLM_GELAENDENAME.TLM_NAMEN_NAME_UUID";

			var queryName = new FeatureQueryNameClass();

			queryName.ShapeFieldName = "TOPGIS_TLM.TLM_GELAENDENAME.SHAPE";
			queryName.ShapeType = esriGeometryType.esriGeometryPolygon;
			queryName.QueryDef = queryDef;
			queryName.Name = "TLM_GELAENDENAME_NAME_JOIN";
			queryName.CopyLocally = false;
			queryName.WorkspaceName = (IWorkspaceName) ((IDataset) workspace).FullName;
			queryName.PrimaryKey = "TOPGIS_TLM.TLM_GELAENDENAME.OBJECTID";
			Console.WriteLine(queryName.PrimaryKey);

			var joinedFeatureClass = (IFeatureClass) queryName.Open();

			// NOTE: QueryName's primary key is not honoured (TOP-5598) -> Use TableJoinUtils.CreateReadOnlyQueryTable
			//       which encapsulates work-around
			string queryNamePkAfterOpen =
				((IQueryName2) ((IDataset) joinedFeatureClass).FullName).PrimaryKey;
			Console.WriteLine($@"QueryName primary key after open: {queryNamePkAfterOpen}");

			Console.WriteLine(@"OID field name: {0}", joinedFeatureClass.OIDFieldName);

			var queryFilter = new SpatialFilterClass();

			queryFilter.SearchOrder = esriSearchOrder.esriSearchOrderSpatial;
			queryFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
			queryFilter.Geometry = GeometryFactory.CreateEnvelope(2655000, 1157000, 2656000,
				1158000);
			// small extent -> crash
			queryFilter.Geometry = GeometryFactory.CreateEnvelope(1655000, 1157000, 3656000,
				2158000);
			queryFilter.WhereClause = string.Empty;

			queryFilter.SubFields = "";
			queryFilter.SubFields = queryNamePkAfterOpen;
			queryFilter.SubFields = "TOPGIS_TLM.TLM_GELAENDENAME.OBJECTID";
			queryFilter.SubFields = joinedFeatureClass.ShapeFieldName;
			// queryFilter.SubFields = featureClass.OIDFieldName;

			// larger extent: 
			// Correct expected rowcount: 5706

			// SubFields = "": 72 sec for rowcount=5706
			// SubFields = featureClass.OIDFieldName: 10 sec for rowCount=1822, != 5706!
			// SubFields = "TOPGIS_TLM.TLM_GELAENDENAME.OBJECTID": 30 sec for rowCount = 5706
			Stopwatch watch = Stopwatch.StartNew();

			Console.WriteLine(queryFilter.SubFields);
			var joinedRowCount = 0; // ((ITable) joinedFeatureClass).RowCount(queryFilter);

			watch.Stop();

			Console.WriteLine(
				$@"Joined row count: {joinedRowCount} ({watch.ElapsedMilliseconds:N0} ms)");

			// does not fail if queryFilter.Subfields = CORRECT oid field
			// also fails with inner join
			// does not fail with queryFilter.Subfields = "*"

			IFeatureClass featureClass =
				workspace.OpenFeatureClass("TOPGIS_TLM.TLM_GELAENDENAME");
			queryFilter.SubFields = featureClass.OIDFieldName; // 25434 ms
			//queryFilter.SubFields = featureClass.ShapeFieldName;  // 25606 ms

			watch.Stop();
			watch.Start();

			long rowCount = ((ITable) featureClass).RowCount(queryFilter);

			watch.Stop();

			Console.WriteLine($@"Row count: {rowCount} ({watch.ElapsedMilliseconds:N0} ms)");
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanQueryFeaturesInnerJoinedToNameParts()
		{
			IFeatureWorkspace workspace = OpenTestWorkspace();

			var relClasses =
				new List<IRelationshipClass>
				{
					DatasetUtils.OpenRelationshipClass(workspace,
					                                   "TOPGIS_TLM.TLM_GEBIETSNAME_NAME"),
					DatasetUtils.OpenRelationshipClass(workspace,
					                                   "TOPGIS_TLM.TLM_NAMEN_NAMENSTEIL_NAME")
				};

			ITable queryTableWithWrongOid = TableJoinUtils.CreateQueryTable(
				relClasses, JoinType.InnerJoin, includeOnlyOIDFields: true);

			// Wrong OID Field: TOPGIS_TLM.TLM_NAMEN_NAME.OBJECTID
			Console.WriteLine(@"Wrong OID Field: {0}", queryTableWithWrongOid.OIDFieldName);

			IReadOnlyTable queryTable = TableJoinUtils.CreateReadOnlyQueryTable(
				relClasses, JoinType.InnerJoin, includeOnlyOIDFields: true);

			// Correct OID field (corrected in RoTable using alternate OID field):
			Assert.AreEqual("TOPGIS_TLM.TLM_GEBIETSNAME.OBJECTID", queryTable.OIDFieldName);

			var featureClass = (IReadOnlyFeatureClass) queryTable;

			var envelope = new EnvelopeClass
			               {
				               XMin = 2480000,
				               YMin = 1262000,
				               XMax = 2900000,
				               YMax = 1350000
			               };

			int rowCount = 0;
			var filter = new AoFeatureClassFilter(
				envelope, esriSpatialRelEnum.esriSpatialRelIntersects);

			foreach (IReadOnlyRow row in featureClass.EnumRows(filter, false))
			{
				rowCount++;
				IFeature gottenFeature = (IFeature) featureClass.GetRow(row.OID);

				Assert.AreEqual(row.OID, gottenFeature.OID);
				Assert.IsTrue(GeometryUtils.AreEqual(gottenFeature.Shape,
				                                     ((IReadOnlyFeature) row).Shape));
			}

			Console.WriteLine(@"Rows: {0}", rowCount);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(TestCategory.Repro)]
		public void ReproTestShapeIntegrityErrorSDE()
		{
			// to reproduce TOP-4851: 
			//
			// 1. run the following statement in sqlplus as TOPGIS_TLM:
			//    execute dbms_stats.gather_table_stats(ownname => 'topgis_tlm', tabname => 'TLM_GEBIETSNAME', estimate_percent => DBMS_STATS.AUTO_SAMPLE_SIZE, cascade => TRUE, no_invalidate => FALSE);
			//
			// 2. run this test TWICE --> exception in second call

			IFeatureWorkspace workspace = OpenTestWorkspace();

			IWorkspaceName2 workspaceName = WorkspaceUtils.GetWorkspaceName(workspace);

			IQueryDef queryDef = workspace.CreateQueryDef();

			// NOTE SOLUTION: the OID from the base feature class must be immediately before the SHAPE field (which may have to be last)
			queryDef.SubFields =
				"TOPGIS_TLM.TLM_NAMEN_NAMENSTEIL.OBJECTID,TOPGIS_TLM.TLM_NAMEN_NAME.OBJECTID,TOPGIS_TLM.TLM_GEBIETSNAME.OBJECTID,TOPGIS_TLM.TLM_GEBIETSNAME.SHAPE";
			queryDef.Tables =
				"TOPGIS_TLM.TLM_NAMEN_NAME INNER JOIN TOPGIS_TLM.TLM_NAMEN_NAMENSTEIL ON TOPGIS_TLM.TLM_NAMEN_NAME.UUID = TOPGIS_TLM.TLM_NAMEN_NAMENSTEIL.TLM_NAMEN_NAME_UUID INNER JOIN TOPGIS_TLM.TLM_GEBIETSNAME ON TOPGIS_TLM.TLM_NAMEN_NAME.UUID = TOPGIS_TLM.TLM_GEBIETSNAME.TLM_NAMEN_NAME_UUID";
			//((IQueryDef2)queryDef).PostfixClause = "ORDER BY TOPGIS_TLM.TLM_GEBIETSNAME.OBJECTID ASC";

			// excluding one feature avoids the exception:
			// queryDef.WhereClause = "TOPGIS_TLM.TLM_GEBIETSNAME.OBJECTID <> 585283";
			IQueryName2 queryName = new FeatureQueryNameClass();

			queryName.QueryDef = queryDef;
			queryName.PrimaryKey = "TOPGIS_TLM.TLM_NAMEN_NAMENSTEIL.OBJECTID";
			queryName.CopyLocally = false;
			((IDatasetName) queryName).WorkspaceName = workspaceName;

			var datasetName = (IDatasetName) queryName;
			datasetName.Name = "JoinedFeatureClass";
			datasetName.WorkspaceName = workspaceName;

			var featureClass = (IFeatureClass) ((IName) queryName).Open();

			var envelope = new EnvelopeClass
			               {
				               XMin = 2480000,
				               YMin = 1062000,
				               XMax = 2900000,
				               YMax = 1350000
			               };

			int rowCount;
			int totalSegmentCount = GetTotalSegmentCount(envelope, featureClass, out rowCount);

			Console.WriteLine(@"Rows: {0}", rowCount);
			Console.WriteLine(@"Total segments: {0}", totalSegmentCount);

			// NOTE: without a spatial filter, the result is correct for SDE
			// NOTE: when ordering on GEBIETSNAME.OBJECTID, the shape integrity error occurs ALWAYS
			// NOTE: when ordering on GEBIETSNAME.OBJECTID DESC, the CRASH occurs ALWAYS
			// NOTE: when ordering on TOPGIS_TLM.TLM_NAMEN_NAMENSTEIL.OBJECTID --> no error/crash, but returned geometry (segment count) is wrong for problem feature

			// TODO compare execution plans for failing/not failing (but incorrect) queries. Maybe the second query after a gather_stats uses a different plan?
		}

		[Test]
		[Ignore("repro case, uses local data")]
		public void ReproTestShapeIntegrityErrorFGDB()
		{
			// NOTE the error does not occur with FGDB

			IFeatureWorkspace workspace =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(
					@"C:\Topgis\Issues\TGS-1100_ShapeIntegrityError\TLM_Names.gdb");

			IWorkspaceName2 workspaceName = WorkspaceUtils.GetWorkspaceName(workspace);

			IQueryDef queryDef = workspace.CreateQueryDef();

			queryDef.SubFields =
				"TLM_NAMEN_NAME.OBJECTID,TLM_GEBIETSNAME.OBJECTID,TLM_NAMEN_NAMENSTEIL.OBJECTID,TLM_GEBIETSNAME.SHAPE";
			queryDef.Tables =
				"TLM_NAMEN_NAME INNER JOIN TLM_NAMEN_NAMENSTEIL ON TLM_NAMEN_NAME.UUID = TLM_NAMEN_NAMENSTEIL.TLM_NAMEN_NAME_UUID INNER JOIN TLM_GEBIETSNAME ON TLM_NAMEN_NAME.UUID = TLM_GEBIETSNAME.TLM_NAMEN_NAME_UUID";

			// excluding one feature avoids the exception:
			// queryDef.WhereClause = "TLM_GEBIETSNAME.OBJECTID <> 585283";
			IQueryName2 queryName = new FeatureQueryNameClass();

			queryName.QueryDef = queryDef;
			queryName.PrimaryKey = "TLM_NAMEN_NAMENSTEIL.OBJECTID";
			queryName.CopyLocally = false;
			((IDatasetName) queryName).WorkspaceName = workspaceName;

			var datasetName = (IDatasetName) queryName;
			datasetName.Name = "JoinedFeatureClass";
			datasetName.WorkspaceName = workspaceName;

			var featureClass = (IFeatureClass) ((IName) queryName).Open();

			var envelope = new EnvelopeClass
			               {
				               XMin = 2480000,
				               YMin = 1062000,
				               XMax = 2900000,
				               YMax = 1350000
			               };

			int totalSegmentCount;
			int rowCount;

			try
			{
				totalSegmentCount = GetTotalSegmentCount(envelope, featureClass, out rowCount);
			}
			catch (Exception exception)
			{
				Console.WriteLine(@"Exception: {0}", exception);

				totalSegmentCount = GetTotalSegmentCount(envelope, featureClass, out rowCount);
			}

			Console.WriteLine(@"Rows: {0}", rowCount);
			Console.WriteLine(@"Total segments: {0}", totalSegmentCount);
			// Assert.AreEqual(148118, totalSegmentCount);

			// NOTE: gebietsname.oid = 585283 has 804 segments in FGDB query result, but 6598 segments in SDE query result (SAME AS gebietsname.oid = 573121)!!
			// NOTE: these two features are returned in alternating order in BOTH query results 

			/* FGDB (presumably correct segment count):
			 * 601345;573121;39377;Shape: 6598 segments
			 * 601345;585283;39377;Shape: 804 segments
			 * 601345;573121;39378;Shape: 6598 segments
			 * 601345;585283;39378;Shape: 804 segments
			 *
			 * SDE:
			 * 601345;573121;39377;Shape: 6598 segments
			 * 601345;585283;39377;Shape: 6598 segments
			 * 601345;573121;39378;Shape: 6598 segments
			 * 601345;585283;39378;Shape: 6598 segments
			 */
		}

		private static int GetTotalSegmentCount([NotNull] IEnvelope envelope,
		                                        [NotNull] IFeatureClass featureClass,
		                                        out int rowCount)
		{
			var filter = new SpatialFilterClass
			             {
				             Geometry = envelope,
				             GeometryField = featureClass.ShapeFieldName,
				             SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects
			             };

			IFeatureCursor cursor = featureClass.Search(filter, true);

			var totalSegmentCount = 0;
			rowCount = 0;

			try
			{
				int fieldCount = featureClass.Fields.FieldCount;

				for (IFeature feature = cursor.NextFeature();
				     feature != null;
				     feature = cursor.NextFeature())
				{
					rowCount++;
					var sb = new StringBuilder();

					for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
					{
						object value = feature.Value[fieldIndex];
						var shape = value as IPolygon;
						if (shape != null)
						{
							var segments = (ISegmentCollection) shape;

							totalSegmentCount += segments.SegmentCount;
							sb.AppendFormat("Shape: {0} segments", segments.SegmentCount);
						}
						else
						{
							sb.AppendFormat("{0};", value);
						}
					}

					Console.WriteLine(sb.ToString());
				}
			}
			finally
			{
				Marshal.ReleaseComObject(cursor);
			}

			return totalSegmentCount;
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTableNTo1LeftJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_STRASSE_NAME";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			// origin = name, destination = strasse
			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsFalse(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.LeftJoin));
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.LeftJoin);

			long rowCount = GetRowCount(table);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, rowCount);

			Assert.IsNotNull(table);
			Assert.IsFalse(table is IFeatureClass);

			// compare with oid-only table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable oidOnlyTable = TableJoinUtils.CreateQueryTable(rc, JoinType.LeftJoin,
				includeOnlyOIDFields: includeOnlyOIDFields,
				excludeShapeField: excludeShapeField);
			Assert.AreEqual(rowCount, GetRowCount(oidOnlyTable));
			// row count will be larger than origin row count due to :n row duplication

			AssertOidsComplete(rc.OriginClass, oidOnlyTable);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTableNTo1InnerJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_STRASSE_STRASSE_AVS";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			// origin = name, destination = strasse
			long nOrig = ((ITable) rc.OriginClass).RowCount(null);
			long nDest = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.InnerJoin));
			ITable featureClass = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin);

			long featureCount = GetRowCount(featureClass);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  nOrig, nDest, featureCount);

			Assert.IsNotNull(featureClass);
			Assert.IsTrue(featureClass is IFeatureClass);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin,
			                                               includeOnlyOIDFields:
			                                               includeOnlyOIDFields,
			                                               excludeShapeField: excludeShapeField);
			Assert.AreEqual(featureCount, GetRowCount(table));

			IFeatureClass inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, RelationshipClassUtils.GetFeatureClasses(rc).Single(),
				"inMemoryJoined");

			Assert.AreEqual(featureCount, GetRowCount((ITable) inMemoryJoinedClass));

			Stopwatch watch = Stopwatch.StartNew();

			CheckOIDs((ITable) inMemoryJoinedClass, true);

			watch.Stop();
			Console.WriteLine(@"Without unique IDs: {0}ms", watch.ElapsedMilliseconds);

			inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, RelationshipClassUtils.GetFeatureClasses(rc).Single(),
				"inMemoryJoined", JoinType.InnerJoin, true);

			Assert.AreEqual(featureCount, GetRowCount((ITable) inMemoryJoinedClass));

			watch.Restart();
			CheckOIDs((ITable) inMemoryJoinedClass, false);
			watch.Stop();
			Console.WriteLine(@"With unique IDs: {0}ms", watch.ElapsedMilliseconds);
		}

		private static void CheckOIDs(ITable table, bool allowDuplicates = false)
		{
			var oids = new HashSet<long>();

			int duplicates = 0;
			long lastOid = -1;
			foreach (IRow row in GdbQueryUtils.GetRows(table, true))
			{
				lastOid = row.OID;
				if (oids.Contains(lastOid))
				{
					duplicates++;
				}

				if (! allowDuplicates)
					Assert.IsFalse(oids.Contains(lastOid), $"Duplicate OID: {lastOid}");

				oids.Add(lastOid);
			}

			Console.WriteLine(@"Duplicates allowed: {0} ({1} duplicates)", allowDuplicates,
			                  duplicates);

			if (! allowDuplicates && lastOid > 0)
			{
				// Extra check
				long checkId = table.GetRow((int) lastOid).OID;

				Assert.AreEqual(lastOid, checkId);
			}
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTableNTo1RightJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_STRASSE_NAME";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			// origin = name, destination = strasse
			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.RightJoin));
			ITable featureClass = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin);

			// It has the destination table's OIDField (with non-unique values, but guaranteed not null)
			const string rightSideOidField = "TOPGIS_TLM.TLM_STRASSE.OBJECTID";
			Assert.AreEqual(rightSideOidField, featureClass.OIDFieldName);

			long featureCount = GetRowCount(featureClass);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			Assert.IsNotNull(featureClass);
			Assert.IsTrue(featureClass is IFeatureClass);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin,
			                                               includeOnlyOIDFields:
			                                               includeOnlyOIDFields,
			                                               excludeShapeField: excludeShapeField);

			// Right join uses destination table as primary key:
			Assert.AreEqual(rightSideOidField, table.OIDFieldName);

			Assert.AreEqual(featureCount, GetRowCount(table));
			Assert.AreEqual(destinationRowCount, featureCount);

			// all strasse features are expected in result
			AssertOidsComplete(rc.DestinationClass, table);

			// Regarding OIDFieldName, compare with CreateReadOnlyTable
			IReadOnlyTable roFeatureClass = TableJoinUtils.CreateReadOnlyQueryTable(
				rc, JoinType.RightJoin);

			// The Strasse contains the foreign key (and is therefore unique OR NULL)
			// TLM_STRASSEN_NAME.OBJECTID is NOT unique but not NULL!
			// We prefer uniqueness or no OID
			Assert.AreEqual("TOPGIS_TLM.TLM_STRASSE.OBJECTID", roFeatureClass.OIDFieldName);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTable1To1LeftJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			// origin = Strasse, destination = wanderweg
			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.LeftJoin));
			ITable featureClass = TableJoinUtils.CreateQueryTable(rc, JoinType.LeftJoin);

			long featureCount = GetRowCount(featureClass);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			Assert.IsNotNull(featureClass);
			Assert.IsTrue(featureClass is IFeatureClass);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.LeftJoin,
			                                               includeOnlyOIDFields:
			                                               includeOnlyOIDFields,
			                                               excludeShapeField: excludeShapeField);
			Assert.AreEqual(featureCount, GetRowCount(table));
			Assert.AreEqual(originRowCount, featureCount);

			// all strasse features are expected in result
			AssertOidsComplete(rc.OriginClass, table);

			IFeatureClass inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, RelationshipClassUtils.GetFeatureClasses(rc).Single(),
				"inMemoryJoined", JoinType.LeftJoin);

			Assert.AreEqual(featureCount, GetRowCount((ITable) inMemoryJoinedClass));

			// Check if we can iterate also:
			Assert.AreEqual(featureCount,
			                GdbQueryUtils.GetFeatures(inMemoryJoinedClass, true).Count());

			CheckOIDs((ITable) inMemoryJoinedClass);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTable1To1InnerJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			// origin = Strasse, destination = wanderweg
			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.InnerJoin));
			ITable featureClass = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin);

			long featureCount = GetRowCount(featureClass);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			Assert.IsNotNull(featureClass);
			Assert.IsTrue(featureClass is IFeatureClass);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin,
			                                               includeOnlyOIDFields:
			                                               includeOnlyOIDFields,
			                                               excludeShapeField: excludeShapeField);
			Assert.AreEqual(featureCount, GetRowCount(table));

			IFeatureClass inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, RelationshipClassUtils.GetFeatureClasses(rc).Single(),
				"inMemoryJoined");

			Assert.AreEqual(featureCount, GetRowCount((ITable) inMemoryJoinedClass));

			// Check if we can iterate also:
			Assert.AreEqual(featureCount,
			                GdbQueryUtils.GetFeatures(inMemoryJoinedClass, true).Count());

			CheckOIDs((ITable) inMemoryJoinedClass);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTable1To1InnerJoinBetweenFeatureClasses()
		{
			// On both sides there is a feature class (requires filtering of shape/area fields)

			const string relClassName = "TOPGIS_TLM.TLM_GEBAEUDE_GRUNDRISS";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			// origin = Grundriss, destination = Gebaeude
			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.InnerJoin));
			ITable featureClass = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin);

			long featureCount = GetRowCount(featureClass);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			Assert.IsNotNull(featureClass);
			Assert.IsTrue(featureClass is IFeatureClass);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin,
			                                               includeOnlyOIDFields:
			                                               includeOnlyOIDFields,
			                                               excludeShapeField: excludeShapeField);
			Assert.AreEqual(featureCount, GetRowCount(table));

			IFeatureClass inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, (IFeatureClass) rc.DestinationClass, "inMemoryJoined");

			Assert.AreEqual(featureCount, GetRowCount((ITable) inMemoryJoinedClass));
			// Check if we can iterate also:
			Assert.AreEqual(featureCount,
			                GdbQueryUtils.GetFeatures(inMemoryJoinedClass, true).Count());
			CheckOIDs((ITable) inMemoryJoinedClass);

			inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, (IFeatureClass) rc.OriginClass, "inMemoryJoined");

			Assert.AreEqual(featureCount, GetRowCount((ITable) inMemoryJoinedClass));
			// Check if we can iterate also:
			Assert.AreEqual(featureCount,
			                GdbQueryUtils.GetFeatures(inMemoryJoinedClass, true).Count());
			CheckOIDs((ITable) inMemoryJoinedClass);

			// Test filter:
			var filter = new QueryFilterClass()
			             {
				             WhereClause =
					             "TOPGIS_TLM.TLM_GEBAEUDE.OPERATEUR = 'RevelJérém' AND TOPGIS_TLM.TLM_GRUNDRISS.OPERATEUR = 'RevelJérém'"
			             };
			int filteredCount =
				GdbQueryUtils.GetFeatures(inMemoryJoinedClass, filter, true).Count();

			Assert.Less(filteredCount, featureCount);
			Assert.Greater(filteredCount, 0);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTable1To1RightJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			// origin = wanderweg, destination = strasse
			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsFalse(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.RightJoin));
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin);

			long rowCount = GetRowCount(table);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, rowCount);

			Assert.IsNotNull(table);
			Assert.IsFalse(table is IFeatureClass);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable oidOnlyTable = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin,
				includeOnlyOIDFields: includeOnlyOIDFields,
				excludeShapeField: excludeShapeField);
			Assert.AreEqual(rowCount, GetRowCount(oidOnlyTable));
			Assert.AreEqual(destinationRowCount, rowCount);

			// all strasse features are expected in result
			AssertOidsComplete(rc.DestinationClass, table);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTableNToMLeftJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);
			Assert.AreEqual(rc.Cardinality, esriRelCardinality.esriRelCardinalityManyToMany);

			// origin (left) = strassenroute, destination = strasse
			ITable strassenRouteTable = ((ITable) rc.OriginClass);
			long originRowCount = strassenRouteTable.RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsFalse(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.LeftJoin));
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.LeftJoin);

			long rowCount = GetRowCount(table);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, rowCount);

			Assert.IsNotNull(table);
			Assert.IsFalse(table is IFeatureClass);
			// not a feature class, since there would be NULL shapes due to the outer join
			Assert.IsTrue(rowCount >= originRowCount);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable oidOnlyTable = TableJoinUtils.CreateQueryTable(rc, JoinType.LeftJoin,
				includeOnlyOIDFields: includeOnlyOIDFields,
				excludeShapeField: excludeShapeField);
			Assert.AreEqual(rowCount, GetRowCount(oidOnlyTable));
			// row count will be larger than origin row count due to :n row duplication

			// all route features are expected in result
			AssertOidsComplete(rc.OriginClass, table);

			// Now check the in-memory join with the 'geometry-end' (i.e. left table) being the strassenroute class.
			GdbTable inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbTable(
				rc, strassenRouteTable, "inMemoryJoined", JoinType.LeftJoin, true);

			AssertOidsComplete((IObjectClass) strassenRouteTable, inMemoryJoinedClass);

			// And also with the strassen feature class on the left:
			var strassenClass = (IFeatureClass) rc.DestinationClass;
			GdbFeatureClass m2nFeatureClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, strassenClass, "inMemoryJoined", JoinType.LeftJoin, true);

			AssertOidsComplete(strassenClass, m2nFeatureClass);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTableNToMRightJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);
			Assert.AreEqual(rc.Cardinality, esriRelCardinality.esriRelCardinalityManyToMany);

			// origin = wanderroute, destination = strasse
			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.RightJoin));
			ITable featureClass = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin);

			long featureCount = GetRowCount(featureClass);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			Assert.IsNotNull(featureClass);
			Assert.IsTrue(featureClass is IFeatureClass);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin,
			                                               includeOnlyOIDFields:
			                                               includeOnlyOIDFields,
			                                               excludeShapeField: excludeShapeField);

			// TODO: this fails, the oid-only, non-shape result has a larger count (on HIACE)
			// full feature count: 1759433

			// oid-only, without shape:        1761168 --> difference: 1735
			// oid-only, with shape:           1761168 --> difference: 1735
			// with all fields, without shape: 1761222 --> difference: 1789
			// with all fields, WITH shape:    1761222 --> difference: 1789 !! SECOND FCLASS WITH SAME PARAMETERS !!

			Assert.AreEqual(featureCount, GetRowCount(table));
			// row count will be larger than destination row count due to :n row duplication

			// all strasse features are expected in result
			AssertOidsComplete(rc.DestinationClass, table);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryTableNToMInnerJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);
			Assert.AreEqual(rc.Cardinality, esriRelCardinality.esriRelCardinalityManyToMany);

			// origin = wanderroute, destination = strasse
			long nOrig = ((ITable) rc.OriginClass).RowCount(null);
			long nDest = ((ITable) rc.DestinationClass).RowCount(null);

			ITable featureClass = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin);

			long featureCount = GetRowCount(featureClass);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  nOrig, nDest, featureCount);

			Assert.IsNotNull(featureClass);
			Assert.IsTrue(featureClass is IFeatureClass);
			Assert.IsTrue(featureCount <= nDest);

			// compare with oid-only, non-spatial table row count
			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;
			ITable table = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin,
			                                               includeOnlyOIDFields:
			                                               includeOnlyOIDFields,
			                                               excludeShapeField: excludeShapeField);
			Assert.AreEqual(featureCount, GetRowCount(table));

			IFeatureClass geometryEndClass = RelationshipClassUtils.GetFeatureClasses(rc).Single();

			IFeatureClass inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, geometryEndClass, "inMemoryJoined");

			Assert.AreEqual(featureCount, GetRowCount((ITable) inMemoryJoinedClass));

			Stopwatch watch = Stopwatch.StartNew();

			CheckOIDs((ITable) inMemoryJoinedClass, true);

			watch.Stop();
			Console.WriteLine(@"Without unique IDs: {0}ms", watch.ElapsedMilliseconds);

			inMemoryJoinedClass = TableJoinUtils.CreateJoinedGdbFeatureClass(
				rc, geometryEndClass, "inMemoryJoined", JoinType.InnerJoin, true);

			Assert.AreEqual(featureCount, GetRowCount((ITable) inMemoryJoinedClass));

			watch.Restart();
			CheckOIDs((ITable) inMemoryJoinedClass, false);
			watch.Stop();
			Console.WriteLine(@"Wit unique IDs (and extra row in result): {0}ms",
			                  watch.ElapsedMilliseconds);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryDef1to1LeftJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			IQueryDef def = TableJoinUtils.CreateQueryDef(rc, JoinType.LeftJoin);
			Assert.IsNotNull(def);
			Console.WriteLine(def.SubFields);
			Console.WriteLine(def.Tables);
			Console.WriteLine(def.WhereClause);

			Assert.AreEqual("TOPGIS_TLM.TLM_STRASSE,TOPGIS_TLM.TLM_WANDERWEG", def.Tables);
			Assert.AreEqual(
				"TOPGIS_TLM.TLM_STRASSE.UUID = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID (+)",
				def.WhereClause);
			//Assert.AreEqual(
			//	"TOPGIS_TLM.TLM_STRASSE LEFT OUTER JOIN TOPGIS_TLM.TLM_WANDERWEG  ON TOPGIS_TLM.TLM_STRASSE.UUID = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID ",
			//	def.Tables);
			//Assert.IsEmpty(def.WhereClause);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryDef1to1RightJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			IQueryDef def = TableJoinUtils.CreateQueryDef(rc, JoinType.RightJoin);
			Assert.IsNotNull(def);
			Console.WriteLine(def.SubFields);
			Console.WriteLine(def.Tables);
			Console.WriteLine(def.WhereClause);

			Assert.AreEqual("TOPGIS_TLM.TLM_STRASSE,TOPGIS_TLM.TLM_WANDERWEG", def.Tables);
			Assert.AreEqual(
				"TOPGIS_TLM.TLM_STRASSE.UUID (+) = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID",
				def.WhereClause);
			//Assert.AreEqual(
			//	"TOPGIS_TLM.TLM_WANDERWEG LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSE  ON TOPGIS_TLM.TLM_STRASSE.UUID = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID ",
			//	def.Tables);
			//Assert.IsEmpty(def.WhereClause);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryDef1to1InnerJoin()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			IQueryDef def = TableJoinUtils.CreateQueryDef(rc, JoinType.InnerJoin);
			Assert.IsNotNull(def);
			Console.WriteLine(def.SubFields);
			Console.WriteLine(def.Tables);
			Console.WriteLine(def.WhereClause);

			Assert.AreEqual("TOPGIS_TLM.TLM_STRASSE,TOPGIS_TLM.TLM_WANDERWEG", def.Tables);
			Assert.AreEqual(
				"TOPGIS_TLM.TLM_STRASSE.UUID = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID",
				def.WhereClause);
			//Assert.AreEqual(
			//	"TOPGIS_TLM.TLM_STRASSE INNER JOIN TOPGIS_TLM.TLM_WANDERWEG  ON TOPGIS_TLM.TLM_STRASSE.UUID = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID ",
			//	def.Tables);
			//Assert.IsEmpty(def.WhereClause);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanEvaluateOidOnlyQueryDef1To1()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			Assert.AreEqual(rc.Cardinality, esriRelCardinality.esriRelCardinalityOneToOne);

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);

			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;

			// left join
			IQueryDef leftJoinQueryDef1 = TableJoinUtils.CreateQueryDef(rc, JoinType.LeftJoin,
				includeOnlyOIDFields,
				excludeShapeField);
			LogQueryDef(leftJoinQueryDef1);

			// BUG: the first Evaluate() call returns an incorrect row count!!
			long leftJoinRowCount1 = GetRowCount(leftJoinQueryDef1);
			Console.WriteLine(@"left join row count 1: {0}", leftJoinRowCount1);

			IQueryDef leftJoinQueryDef2 = TableJoinUtils.CreateQueryDef(rc, JoinType.LeftJoin,
				includeOnlyOIDFields,
				excludeShapeField);
			LogQueryDef(leftJoinQueryDef2);

			// NOTE: the first Evaluate() call on the second, equally defined instance returns the correct row count
			// NOTE: but only after the first Evaluate() on the first instance.
			long leftJoinRowCount2 = GetRowCount(leftJoinQueryDef2);
			Console.WriteLine(@"left join row count 2: {0}", leftJoinRowCount2);

			Assert.AreEqual(originRowCount, leftJoinRowCount2, "unexpected left join row count");

			// right join
			IQueryDef rightJoinQuery = TableJoinUtils.CreateQueryDef(rc, JoinType.RightJoin,
				includeOnlyOIDFields,
				excludeShapeField);
			LogQueryDef(rightJoinQuery);

			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);
			long rightJoinRowCount = GetRowCount(rightJoinQuery);

			Assert.AreEqual(destinationRowCount, rightJoinRowCount,
			                "unexpected right join row count");
			Assert.AreEqual(leftJoinRowCount2, leftJoinRowCount1,
			                "inconsistent row counts for equal left join query defs");

			// NOTE: running the right join before the left join would also yield the correct results
		}

		[Test]
		[Ignore("Repro test for bug in IQueryDef.Evaluate")]
		public void CanEvaluateQueryDef1To1()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);

			Assert.AreEqual(rc.Cardinality, esriRelCardinality.esriRelCardinalityOneToOne);

			long strasseRowCount = ((ITable) rc.OriginClass).RowCount(null);

			// left join
			IQueryDef leftJoinQueryDef1 = TableJoinUtils.CreateQueryDef(rc, JoinType.LeftJoin);
			LogQueryDef(leftJoinQueryDef1);

			// BUG: the first Evaluate() call returns an incorrect row count!!
			long leftJoinRowCount1 = GetRowCount(leftJoinQueryDef1);
			Console.WriteLine(@"left join row count 1: {0}", leftJoinRowCount1);

			IQueryDef leftJoinQueryDef2 = TableJoinUtils.CreateQueryDef(rc, JoinType.LeftJoin);
			LogQueryDef(leftJoinQueryDef2);

			// NOTE: the first Evaluate() call on the second, equally defined instance returns the correct row count
			// NOTE: but only after the first Evaluate() on the first instance.
			long leftJoinRowCount2 = GetRowCount(leftJoinQueryDef2);
			Console.WriteLine(@"left join row count 2: {0}", leftJoinRowCount2);

			Assert.AreEqual(strasseRowCount, leftJoinRowCount2,
			                "unexpected left join row count");

			// right join
			IQueryDef rightJoinQuery = TableJoinUtils.CreateQueryDef(rc, JoinType.RightJoin);
			LogQueryDef(rightJoinQuery);

			long wanderwegRowCount = ((ITable) rc.DestinationClass).RowCount(null);
			long rightJoinRowCount = GetRowCount(rightJoinQuery);

			Assert.AreEqual(wanderwegRowCount, rightJoinRowCount,
			                "unexpected right join row count");
			Assert.AreEqual(leftJoinRowCount2, leftJoinRowCount1,
			                "inconsistent row counts for equal left join query defs");

			// NOTE: running the right join before the left join would also yield the correct results
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanEvaluateOIdOnlyQueryDef1ToM()
		{
			const string relClassName = "TOPGIS_TLM.TLM_STRASSE_NAME";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);
			Assert.AreEqual(rc.Cardinality, esriRelCardinality.esriRelCardinalityOneToMany);

			long nOrig = ((ITable) rc.OriginClass).RowCount(null);
			long nDest = ((ITable) rc.DestinationClass).RowCount(null);

			const bool includeOnlyOIDFields = true;
			const bool excludeShapeField = true;

			IQueryDef def = TableJoinUtils.CreateQueryDef(rc, JoinType.LeftJoin,
			                                              includeOnlyOIDFields,
			                                              excludeShapeField);
			long nRows = GetRowCount(def);
			Assert.IsTrue(nRows >= nOrig);

			def = TableJoinUtils.CreateQueryDef(rc, JoinType.RightJoin, includeOnlyOIDFields,
			                                    excludeShapeField);
			nRows = GetRowCount(def);
			Assert.AreEqual(nRows, nDest);

			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  nOrig, nDest, nRows);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanEvaluateQueryDef1ToM()
		{
			const string relClassName = "TOPGIS_TLM.TLM_STRASSE_NAME";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);
			Assert.AreEqual(rc.Cardinality, esriRelCardinality.esriRelCardinalityOneToMany);

			long nOrig = ((ITable) rc.OriginClass).RowCount(null);
			long nDest = ((ITable) rc.DestinationClass).RowCount(null);

			IQueryDef def = TableJoinUtils.CreateQueryDef(rc, JoinType.LeftJoin);
			long nRows = GetRowCount(def);
			Assert.IsTrue(nRows >= nOrig);

			def = TableJoinUtils.CreateQueryDef(rc, JoinType.RightJoin);
			nRows = GetRowCount(def);
			Assert.AreEqual(nRows, nDest);

			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  nOrig, nDest, nRows);
		}

		[Test]
		[Ignore("Repro test for bug in IQueryDef.Evaluate")]
		public void CanEvaluateQueryDefNToM()
		{
			const string relClassName = "TOPGIS_TLM.TLM_WANDERROUTE_STRASSE";

			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc = ws.OpenRelationshipClass(relClassName);
			Assert.AreEqual(rc.Cardinality, esriRelCardinality.esriRelCardinalityManyToMany);

			long wanderRouteRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long strasseRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Console.WriteLine(@"origin row count (wanderroute): {0}", wanderRouteRowCount);
			Console.WriteLine(@"destination row count (strasse): {0}", strasseRowCount);

			IQueryDef leftJoinQueryDef = TableJoinUtils.CreateQueryDef(rc, JoinType.LeftJoin);
			long leftJoinRowCount = GetRowCount(leftJoinQueryDef);
			Console.WriteLine(@"Left join row count: {0}", leftJoinRowCount);
			Assert.IsTrue(leftJoinRowCount >= wanderRouteRowCount);

			IQueryDef rightJoinQueryDef = TableJoinUtils.CreateQueryDef(rc, JoinType.RightJoin);
			long rightJoinRowCount = GetRowCount(rightJoinQueryDef);

			Console.WriteLine(@"Right join row count: {0}", rightJoinRowCount);
			Assert.IsTrue(rightJoinRowCount >= strasseRowCount);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryFeatureClass()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();

			IFeatureClass featureClass = TableJoinUtils.CreateQueryFeatureClass(
				ws, "test", "TOPGIS_TLM.TLM_STRASSE.OBJECTID", "SHAPE",
				"TOPGIS_TLM.TLM_STRASSE,TOPGIS_TLM.TLM_WANDERWEG",
				"TOPGIS_TLM.TLM_STRASSE.UUID = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID", "*");

			long rowCount = featureClass.FeatureCount(null);
			Console.WriteLine(@"feature count: {0}", rowCount);
		}

		[Test]
		public void CanInnerJoin_NtoM_Fgdb()
		{
			string dbPath = TestData.GetGdbTableJointUtilsPath();

			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);

			CanInnerJoin_NtoM(ws);
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanLeftJoin_NtoM_Fgdb()
		{
			string dbPath = TestData.GetGdbTableJointUtilsPath();

			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);

			CanLeftJoin_NtoM(ws);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanLeftJoin_NtoM_Pgdb()
		{
			var locator = new TestDataLocator();
			string dbPath = locator.GetPath("TableJoinUtilsTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(dbPath);

			var ex = Assert.Throws<ArgumentException>(() => CanLeftJoin_NtoM(ws));

			Assert.That(ex.Message, Is.EqualTo(
				            "Cannot generate join expression for type: LeftJoin"));

			Console.WriteLine(@"outer joins not supported for pgdb");
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanRightJoin_NtoM_Pgdb()
		{
			var locator = new TestDataLocator();
			string dbPath = locator.GetPath("TableJoinUtilsTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(dbPath);

			var ex = Assert.Throws<ArgumentException>(() => CanRightJoin_NtoM(ws));

			Assert.That(ex.Message, Is.EqualTo(
				            "Cannot generate join expression for type: RightJoin"));

			Console.WriteLine(@"outer joins not supported for pgdb");
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanRightJoin_NtoM_Fgdb()
		{
			string dbPath = TestData.GetGdbTableJointUtilsPath();

			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);

			CanRightJoin_NtoM(ws);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanInnerJoin_NtoM_Pgdb()
		{
			var locator = new TestDataLocator();
			string dbPath = locator.GetPath("TableJoinUtilsTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(dbPath);

			CanInnerJoin_NtoM(ws);
		}

		[Test]
		public void CanRightJoin_1toM_Fgdb()
		{
			string dbPath = TestData.GetGdbTableJointUtilsPath();

			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);

			CanRightJoin_1toM(ws);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanRightJoin_1toM_Pgdb()
		{
			var locator = new TestDataLocator();
			string dbPath = locator.GetPath("TableJoinUtilsTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(dbPath);

			CanRightJoin_1toM(ws);
		}

		[Test]
		public void CanLeftOuterJoin_1toM_Fgdb()
		{
			string dbPath = TestData.GetGdbTableJointUtilsPath();

			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);

			CanLeftOuterJoin_1toM(ws);
		}

		[Test]
		public void CanInnerJoin_1toM_Fgdb()
		{
			string dbPath = TestData.GetGdbTableJointUtilsPath();

			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);

			CanInnerJoin_1toM(ws);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanInnerJoin_1toM_Pgdb()
		{
			var locator = new TestDataLocator();
			string dbPath = locator.GetPath("TableJoinUtilsTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(dbPath);

			CanInnerJoin_1toM(ws);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanLeftOuterJoin_1toM_Pgdb()
		{
			var locator = new TestDataLocator();
			string dbPath = locator.GetPath("TableJoinUtilsTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(dbPath);

			var ex = Assert.Throws<ArgumentException>(() => CanLeftOuterJoin_1toM(ws));

			Assert.That(ex.Message, Is.EqualTo(
				            "Cannot generate join expression for type: LeftJoin"));

			Console.WriteLine(@"outer joins not supported for pgdb");
		}

		[Test]
		public void CanRightOuterJoin_1to1_Fgdb()
		{
			string dbPath = TestData.GetGdbTableJointUtilsPath();

			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);

			CanRightOuterJoin_1to1(ws);
		}

		[Test]
		public void CanInnerJoin_1to1_Fgdb()
		{
			string dbPath = TestData.GetGdbTableJointUtilsPath();

			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(dbPath);

			CanInnerJoin_1to1(ws);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanInnerJoin_1to1_Pgdb()
		{
			var locator = new TestDataLocator();
			string dbPath = locator.GetPath("TableJoinUtilsTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(dbPath);

			CanInnerJoin_1to1(ws);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanRightOuterJoin_1to1_Pgdb()
		{
			var locator = new TestDataLocator();
			string dbPath = locator.GetPath("TableJoinUtilsTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(dbPath);

			var ex = Assert.Throws<ArgumentException>(() => CanRightOuterJoin_1to1(ws));

			Assert.That(ex.Message, Is.EqualTo(
				            "Cannot generate join expression for type: RightJoin"));
		}

		private static void CanRightJoin_NtoM([NotNull] IFeatureWorkspace ws)
		{
			IRelationshipClass rc = ws.OpenRelationshipClass("rel_streets_routes");

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsFalse(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.RightJoin));
			ITable queryTable = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin);

			long featureCount = GetRowCount(queryTable);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			// 3 street features have no related route
			AssertOidsComplete(rc.OriginClass, queryTable,
			                   expectedMissingBaseClassRowsCount: 3);
			Assert.AreEqual("Routes.OBJECTID", ((IObjectClass) queryTable).OIDFieldName);

			// In the many-to-many case the association table always has a non-null unique OID
			IReadOnlyTable roQueryTable =
				TableJoinUtils.CreateReadOnlyQueryTable(rc, JoinType.RightJoin);

			Assert.AreEqual("rel_streets_routes.OBJECTID", roQueryTable.OIDFieldName);
		}

		private static void CanLeftJoin_NtoM([NotNull] IFeatureWorkspace ws)
		{
			IRelationshipClass rc = ws.OpenRelationshipClass("rel_streets_routes");

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.LeftJoin));
			ITable queryTable = TableJoinUtils.CreateQueryTable(rc, JoinType.LeftJoin);
			Assert.AreEqual("Rel_Streets_Routes.RID", queryTable.OIDFieldName);

			long featureCount = GetRowCount(queryTable);

			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			// 3 street features have no related route
			AssertOidsComplete(rc.OriginClass, queryTable,
			                   expectedMissingBaseClassRowsCount: 0);

			// Assert.AreEqual("Streets.OBJECTID", ((IObjectClass) queryTable).OIDFieldName);
		}

		private static void CanInnerJoin_NtoM([NotNull] IFeatureWorkspace ws)
		{
			IRelationshipClass rc = ws.OpenRelationshipClass("rel_streets_routes");

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.InnerJoin));
			ITable queryTable = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin);

			long featureCount = GetRowCount(queryTable);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			// 3 street features have no related route
			AssertOidsComplete(rc.OriginClass, queryTable,
			                   expectedMissingBaseClassRowsCount: 3);
			// Assert.AreEqual("Rel_Streets_Routes.RID", ((IObjectClass) queryTable).OIDFieldName);
		}

		private static void CanInnerJoin_1to1([NotNull] IFeatureWorkspace ws)
		{
			IRelationshipClass rc = ws.OpenRelationshipClass("rel_1t_1p");

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.InnerJoin));
			ITable queryTable = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin);

			long featureCount = GetRowCount(queryTable);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			AssertOidsComplete(rc.OriginClass, queryTable,
			                   expectedMissingBaseClassRowsCount: 5);
		}

		private static void CanRightOuterJoin_1to1([NotNull] IFeatureWorkspace ws)
		{
			IRelationshipClass rc = ws.OpenRelationshipClass("rel_1t_1p");

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.RightJoin));
			ITable queryTable = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin);

			long featureCount = GetRowCount(queryTable);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			AssertOidsComplete(rc.DestinationClass, queryTable);
		}

		private static void CanInnerJoin_1toM([NotNull] IFeatureWorkspace ws)
		{
			IRelationshipClass rc = ws.OpenRelationshipClass("rel_1p_mt");

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.InnerJoin));
			ITable queryTable = TableJoinUtils.CreateQueryTable(rc, JoinType.InnerJoin);

			long featureCount = GetRowCount(queryTable);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			AssertOidsComplete(rc.OriginClass, queryTable,
			                   expectedMissingBaseClassRowsCount: 2);
		}

		private static void CanRightJoin_1toM([NotNull] IFeatureWorkspace ws)
		{
			IRelationshipClass rc = ws.OpenRelationshipClass("rel_1p_mt");

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsFalse(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.RightJoin));
			ITable queryTable = TableJoinUtils.CreateQueryTable(rc, JoinType.RightJoin);

			long featureCount = GetRowCount(queryTable);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			AssertOidsComplete(rc.OriginClass, queryTable,
			                   expectedMissingBaseClassRowsCount: 2);
		}

		private static void CanLeftOuterJoin_1toM([NotNull] IFeatureWorkspace ws)
		{
			IRelationshipClass rc = ws.OpenRelationshipClass("rel_1p_mt");

			long originRowCount = ((ITable) rc.OriginClass).RowCount(null);
			long destinationRowCount = ((ITable) rc.DestinationClass).RowCount(null);

			Assert.IsTrue(TableJoinUtils.CanCreateQueryFeatureClass(rc, JoinType.LeftJoin));
			ITable queryTable = TableJoinUtils.CreateQueryTable(rc, JoinType.LeftJoin);

			long featureCount = GetRowCount(queryTable);
			Console.WriteLine(@"origin: {0} dest: {1} query: {2}",
			                  originRowCount, destinationRowCount, featureCount);

			AssertOidsComplete(rc.OriginClass, queryTable);
		}

		private static void AssertOidsComplete([NotNull] IObjectClass baseClass,
		                                       [NotNull] ITable queryTable,
		                                       int expectedMissingBaseClassRowsCount = 0)
		{
			HashSet<int> baseClassOids = GetIntSet((ITable) baseClass, baseClass.OIDFieldName);

			var baseDataset = (IDataset) baseClass;
			IWorkspace workspace = baseDataset.Workspace;

			string qualifiedBaseOidFieldName =
				((ISQLSyntax) workspace).QualifyColumnName(baseDataset.Name,
				                                           baseClass.OIDFieldName);

			int nullValueCount;
			HashSet<int> queryTableNotNullOids = GetIntSet(queryTable,
			                                               qualifiedBaseOidFieldName,
			                                               out nullValueCount);
			if (nullValueCount != 0)
			{
				throw new InvalidOperationException(
					$"Unexpected null value count: {nullValueCount}");
			}

			// get the base class OIDs that are not included in the query result
			baseClassOids.ExceptWith(queryTableNotNullOids);

			if (baseClassOids.Count != expectedMissingBaseClassRowsCount)
			{
				throw new InvalidOperationException(
					$"Unexpected number of base class OIDs missing from query result: {baseClassOids.Count} " +
					$"(expected: {expectedMissingBaseClassRowsCount})");
			}
		}

		[NotNull]
		private static HashSet<int> GetIntSet([NotNull] ITable table,
		                                      [NotNull] string intFieldName)
		{
			return GetIntSet(table, intFieldName, out int _);
		}

		[NotNull]
		private static HashSet<int> GetIntSet([NotNull] ITable table,
		                                      [NotNull] string intFieldName,
		                                      out int nullValueCount)
		{
			var result = new HashSet<int>();

			int fieldIndex = table.FindField(intFieldName);
			if (fieldIndex < 0)
			{
				throw new ArgumentException($@"Field not found: {intFieldName}",
				                            nameof(intFieldName));
			}

			nullValueCount = 0;

			ICursor cursor = table.Search(null, Recycling: true);
			try
			{
				IRow row = cursor.NextRow();

				while (row != null)
				{
					object value = row.Value[fieldIndex];
					if (value == null || value is DBNull)
					{
						nullValueCount++;
					}
					else
					{
						result.Add(Convert.ToInt32(value));
					}

					row = cursor.NextRow();
				}

				return result;
			}
			finally
			{
				if (Marshal.IsComObject(cursor))
				{
					Marshal.ReleaseComObject(cursor);
				}
				else if (cursor is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
		}

		[NotNull]
		private static IFeatureWorkspace OpenTestWorkspace()
		{
			string versionName = "SDE.DEFAULT"; //"TG_SERVICE.RC_TLM_2022-6-30";

			IFeatureWorkspace defaultVersion =
				(IFeatureWorkspace) TestUtils.OpenUserWorkspaceOracle();

			return WorkspaceUtils.OpenFeatureWorkspaceVersion(defaultVersion, versionName);
		}

		private static long GetRowCount([NotNull] IQueryDef queryDef)
		{
			ICursor cursor = ((IQueryDef2) queryDef).Evaluate2(true);

			try
			{
				var rowCount = 0;

				IRow row = cursor.NextRow();

				while (row != null)
				{
					rowCount++;
					row = cursor.NextRow();
				}

				return rowCount;
			}
			finally
			{
				Marshal.ReleaseComObject(cursor);
			}
		}

		private static long GetRowCount([NotNull] ITable table)
		{
			return table.RowCount(null);

			//const bool recycling = true;
			//ICursor cursor = GdbQueryUtils.OpenCursor(table, recycling);
			//try
			//{
			//	var rowCount = 0;

			//	IRow row = cursor.NextRow();

			//	while (row != null)
			//	{
			//		rowCount++;
			//		row = cursor.NextRow();
			//	}

			//	Assert.AreEqual(result, rowCount);
			//	return rowCount;
			//}
			//finally
			//{
			//	Marshal.ReleaseComObject(cursor);
			//}
		}

		private static void LogQueryDef([NotNull] IQueryDef queryDef)
		{
			if (StringUtils.IsNotEmpty(queryDef.WhereClause))
			{
				Console.WriteLine(@"SELECT {0} FROM {1} WHERE {2}",
				                  queryDef.SubFields, queryDef.Tables, queryDef.WhereClause);
			}
			else
			{
				Console.WriteLine(@"SELECT {0} FROM {1}",
				                  queryDef.SubFields, queryDef.Tables);
			}
		}
	}
}
