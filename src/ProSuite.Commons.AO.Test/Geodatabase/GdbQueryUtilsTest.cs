using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Logging;
using Assert = ProSuite.Commons.Essentials.Assertions.Assert;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class GdbQueryUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnittestLogging();

			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		private static Dictionary<int, IRow> GetFirstNRows(ITable table, int count)
		{
			var dic = new Dictionary<int, IRow>(count);
			foreach (IRow row in GdbQueryUtils.GetRows(table, false))
			{
				dic.Add(row.OID, row);
				if (dic.Count >= count)
				{
					break;
				}
			}

			return dic;
		}

		private static IFeatureWorkspace OpenTestWorkspace()
		{
			return (IFeatureWorkspace) TestUtils.OpenSDEWorkspaceOracle();
		}

		[Test]
		[Category(TestCategory.Slow)]
		[Category(TestCategory.Sde)]
		public void CanGetExistingFeaturesFastEnough()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			const int count = 100;
			IDictionary<int, IRow> rows = GetFirstNRows((ITable) fc, count);

			var watch = new Stopwatch();
			watch.Start();

			foreach (int oid in rows.Keys)
			{
				Assert.NotNull(GdbQueryUtils.GetFeature(fc, oid));
				_msg.Info($"Oid {oid} time: {watch.ElapsedMilliseconds}");
			}

			watch.Stop();

			double msPerIteration = watch.ElapsedMilliseconds / (double) rows.Count;

			_msg.InfoFormat(@"GetFeature() per iteration: {0} ms", msPerIteration);

			Assert.True(msPerIteration < 50,
			            "GetFeature with existing feature takes too long ({0} ms)",
			            msPerIteration);
		}

		[Test]
		[Category(TestCategory.Fast)]
		[Category(TestCategory.Sde)]
		public void CanGetExistingRowsFastEnough()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			ITable fc = ws.OpenTable("TOPGIS_TLM.TLM_WANDERWEG");

			const int max = 100;
			IDictionary<int, IRow> rows = GetFirstNRows(fc, max);

			var watch = new Stopwatch();
			watch.Start();

			foreach (int oid in rows.Keys)
			{
				Assert.NotNull(GdbQueryUtils.GetRow(fc, oid));
				_msg.Info($"Oid {oid} time: {watch.ElapsedMilliseconds}");
			}

			watch.Stop();

			double msPerIteration = watch.ElapsedMilliseconds / (double) rows.Count;

			_msg.InfoFormat(@"GetRow() per iteration: {0} ms", msPerIteration);

			Assert.True(msPerIteration < 50,
			            "GetFeature with existing feature takes too long ({0} ms, {1} rows)",
			            msPerIteration, rows.Count);
		}

		[Test]
		[Category(TestCategory.Fast)]
		[Category(TestCategory.Sde)]
		public void CanGetNullForNonExistingFeature()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			Assert.Null(GdbQueryUtils.GetFeature(fc, 999999999));
		}

		[Test]
		[Category(TestCategory.Fast)]
		[Category(TestCategory.Sde)]
		public void CanGetNullForNonExistingFeatureByGetRow()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			Assert.Null(GdbQueryUtils.GetRow((ITable) fc, 999999999));
		}

		[Test]
		[Category(TestCategory.Fast)]
		[Category(TestCategory.Sde)]
		public void CanGetNullForNonExistingFeatureFastEnough()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			const int iterations = 200;

			var watch = new Stopwatch();
			watch.Start();

			for (var iteration = 0; iteration < iterations; iteration++)
			{
				int oid = 999999999 + iteration;
				Assert.Null(GdbQueryUtils.GetFeature(fc, oid));
			}

			watch.Stop();

			double msPerIteration = watch.ElapsedMilliseconds / (double) iterations;

			_msg.InfoFormat(@"GetFeature() per iteration: {0} ms", msPerIteration);

			const int maxMilliseconds = 35;
			Assert.True(msPerIteration < maxMilliseconds,
			            "GetFeature with non-existing feature takes too long ({0} ms)",
			            msPerIteration);
		}

		[Test]
		[Category(TestCategory.Fast)]
		[Category(TestCategory.Sde)]
		public void CanGetNullForNonExistingRow()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			ITable table = ws.OpenTable("TOPGIS_TLM.TLM_WANDERWEG");

			Assert.Null(GdbQueryUtils.GetRow(table, 999999999));
		}

		[Test]
		[Category(TestCategory.Fast)]
		[Category(TestCategory.Sde)]
		public void CanGetProxiesNonSpatial()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc =
				ws.OpenRelationshipClass("TOPGIS_TLM.TLM_STRASSE_NAME");
			NUnit.Framework.Assert.AreEqual(rc.Cardinality,
			                                esriRelCardinality.esriRelCardinalityOneToMany);

			foreach (
				IRow row in
				GdbQueryUtils.GetRowProxys(rc.OriginClass, null,
				                           new[] {rc}))
			{
				NUnit.Framework.Assert.AreEqual(row.Table, rc.OriginClass);
				NUnit.Framework.Assert.Greater(row.OID, 0);
			}
		}

		[Test]
		[Category(TestCategory.Fast)]
		public void CanGetProxiesNonSpatial1()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc =
				ws.OpenRelationshipClass("TOPGIS_TLM.TLM_STRASSEN_NAMENSTEIL_NAME");
			NUnit.Framework.Assert.AreEqual(rc.Cardinality,
			                                esriRelCardinality.esriRelCardinalityOneToMany);

			foreach (
				IRow row in
				GdbQueryUtils.GetRowProxys(rc.OriginClass, null,
				                           new[] {rc}))
			{
				NUnit.Framework.Assert.AreEqual(row.Table, rc.OriginClass);
				NUnit.Framework.Assert.Greater(row.OID, 0);
			}
		}

		[Test]
		[Category(TestCategory.Fast)]
		[Category(TestCategory.Sde)]
		public void CanGetProxiesSpatial()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc =
				ws.OpenRelationshipClass("TOPGIS_TLM.TLM_STRASSE_NAME");
			NUnit.Framework.Assert.AreEqual(rc.Cardinality,
			                                esriRelCardinality.esriRelCardinalityOneToMany);

			foreach (IRow row in GdbQueryUtils.GetRowProxys(rc.OriginClass,
			                                                GeometryFactory.CreatePolygon(
				                                                2696300, 1264100, 2696400,
				                                                1264130),
			                                                new[] {rc}))
			{
				NUnit.Framework.Assert.AreEqual(row.Table, rc.OriginClass);
				NUnit.Framework.Assert.Greater(row.OID, 0);
			}
		}

		[Test]
		[Category(TestCategory.Slow)]
		[Category(TestCategory.Sde)]
		public void CanGetRelated()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IRelationshipClass rc =
				ws.OpenRelationshipClass("TOPGIS_TLM.TLM_STRASSE_NAME");
			NUnit.Framework.Assert.AreEqual(rc.Cardinality,
			                                esriRelCardinality.esriRelCardinalityOneToMany);

			int count = 0;
			foreach (IRow row in GdbQueryUtils.GetRows((ITable) rc.OriginClass, false))
			{
				var obj = (IObject) row;
				IList<IObject> features = GdbQueryUtils.GetRelatedObjectList(obj, new[] {rc});
				count += features.Count;
			}

			NUnit.Framework.Assert.Greater(count, 0);
		}

		[Test]
		[Category(TestCategory.Fast)]
		[Category(TestCategory.Sde)]
		public void CanGetRowsInLongList()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			// fill list
			const int max = 2000;
			Dictionary<int, IRow> dic = GetFirstNRows((ITable) fc, max);

			var selList = new List<int>(max);
			foreach (IRow row in GdbQueryUtils.GetRowsInList((ITable) fc, fc.OIDFieldName,
			                                                 dic.Keys, false, null))
			{
				selList.Add(row.OID);
				IRow origRow = dic[row.OID];
				Assert.AreEqual(origRow.OID, row.OID, "");
				//dic[row.OID] = null; REMARK: this changes list dic.Keys and leads to an error in foreach
			}

			Assert.AreEqual(dic.Count, selList.Count, "List counts differ");

			var oidList = new List<int>(dic.Keys);
			oidList.Sort();
			selList.Sort();
			for (var i = 0; i < oidList.Count; i++)
			{
				Assert.AreEqual(oidList[i], selList[i], "{0}th element differs", i);
			}
		}

		[Test]
		[Category(TestCategory.Slow)]
		[Category(TestCategory.Sde)]
		public void CanGetRowsNotInIntList()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			ITable tbl = ws.OpenTable("TOPGIS_TLM.TLM_STRASSE");

			var nRows = 0;
			IQueryFilter filter = new QueryFilterClass();
			foreach (
				// ReSharper disable once UnusedVariable
				IRow row in
				GdbQueryUtils.GetRowsNotInList(tbl, filter, true, "OBJEKTART",
				                               new object[] {1, 2, 3}))
			{
				nRows++;
			}

			filter.WhereClause = "OBJEKTART not in (1, 2, 3)";
			int n = tbl.RowCount(filter);
			Assert.AreEqual(n, nRows, "");
		}

		[Test]
		[Category(TestCategory.Slow)]
		[Category(TestCategory.Sde)]
		public void CanGetRowsNotInStringList()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			ITable tbl = ws.OpenTable("TOPGIS_TLM.TLM_STRASSE");

			var nRows = 0;
			IQueryFilter filter = new QueryFilterClass();
			foreach (
				// ReSharper disable once UnusedVariable
				IRow row in
				GdbQueryUtils.GetRowsNotInList(tbl, filter, true, "OPERATEUR",
				                               new[] {"STR_Imp"}))
			{
				nRows++;
			}

			filter.WhereClause = "OPERATEUR not in ('STR_Imp')";
			int n = tbl.RowCount(filter);
			Assert.AreEqual(n, nRows, "");
		}

		[Test]
		[Category(TestCategory.Slow)]
		[Category(TestCategory.Sde)]
		public void CanGetRowsNotInUUIDList()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			ITable tbl = ws.OpenTable("TOPGIS_TLM.TLM_STRASSE");

			var nRows = 0;
			IQueryFilter filter = new QueryFilterClass();
			foreach (
				// ReSharper disable once UnusedVariable
				IRow row in
				GdbQueryUtils.GetRowsNotInList(tbl, filter, true, "UUID",
				                               new[]
				                               {
					                               "{8C5517C9-B19F-4CC1-A6A1-D3DD317BCDD1}"
				                               }))
			{
				nRows++;
			}

			filter.WhereClause = "UUID not in ('{8C5517C9-B19F-4CC1-A6A1-D3DD317BCDD1}')";
			int n = tbl.RowCount(filter);
			Assert.AreEqual(n, nRows, "");
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void Learning_CanFindFeaturesWithSubResolutionEnvelope()
		{
			// TODO: This used to fail (in 2011) ("The number of points is less than required")
			// TEST if it now works on all supported versions!

			// 10.2.1: Test passes, Features are found
			// 10.4.1: Test passes, Features are found
			// 10.6.1: Test passes, Features are found

			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			IEnvelope subResolutionEnv = GeometryFactory.CreateEnvelope(2600000, 1200000,
			                                                            2600000.0001,
			                                                            1200000.0001);

			ISpatialReference spatialReference =
				Assert.NotNull(DatasetUtils.GetSpatialReference(fc));

			subResolutionEnv.SpatialReference = spatialReference;

			IPoint point = null;
			foreach (var feature in GdbQueryUtils.GetFeatures(fc, true))
			{
				point = ((IPolyline) feature.Shape).FromPoint;
				break;
			}

			Assert.NotNull(point);
			IEnvelope envelope = point.Envelope;

			ISpatialFilter spatialFilter = new SpatialFilterClass();

			spatialFilter.GeometryField = fc.ShapeFieldName;

			spatialFilter.set_GeometryEx(envelope, true);

			spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

			var srClone =
				(ISpatialReferenceResolution) ((IClone) spatialReference).Clone();
			srClone.set_XYResolution(true, 0.00001);

			spatialFilter.set_OutputSpatialReference(fc.ShapeFieldName,
			                                         (ISpatialReference) srClone);

			int found = GdbQueryUtils.GetFeatures(fc, spatialFilter, true).Count();

			Assert.True(found > 0, "No features found with mini-envelope");

			envelope.Expand(0.0001, 0.0001, false);
			IPolygon miniPoly = GeometryFactory.CreatePolygon(envelope);
			spatialFilter.set_GeometryEx(miniPoly, true);

			found = GdbQueryUtils.GetFeatures(fc, spatialFilter, true).Count();

			Assert.True(found > 0, "No features found with mini-polygon");
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void Learning_CanFindFeaturesWithNonZSimpleSearchGeometry()
		{
			// 10.2.1: Test fails (correct) with COM Exception on OpenCursor
			// 10.4.1: Test fails (correct) with COM Exception on OpenCursor
			// 10.6.1: Test passes (incorrectly!), no features found

			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			IEnvelope nonZSimpleEnvelope = GeometryFactory.CreateEnvelope(2600000, 1200000,
			                                                              2700000, 1300000);

			GeometryUtils.MakeZAware(nonZSimpleEnvelope);

			Assert.False(((IZAware) nonZSimpleEnvelope).ZSimple, "Must be non-Z-simple");

			ISpatialReference spatialReference = DatasetUtils.GetSpatialReference(fc);

			nonZSimpleEnvelope.SpatialReference = spatialReference;

			ISpatialFilter spatialFilter = new SpatialFilterClass();

			spatialFilter.GeometryField = fc.ShapeFieldName;
			spatialFilter.set_GeometryEx(nonZSimpleEnvelope, true);
			spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

			Assert.AreEqual(0, GdbQueryUtils.GetFeatures(fc, spatialFilter, true).Count(),
			                "Behaviour changed: Now features are found even with non-Z-simple search geometry.");
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateSpatialFilterWithSubResolutionPolygon()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			IPolygon subResolutionPoly = GeometryFactory.CreatePolygon(2600000, 1200000,
			                                                           2600000.0001,
			                                                           1200000.0001);

			subResolutionPoly.SpatialReference = ((IGeoDataset) fc).SpatialReference;

			IQueryFilter filter = GdbQueryUtils.CreateSpatialFilter(
				fc, subResolutionPoly, esriSpatialRelEnum.esriSpatialRelIntersects, false,
				null);

			IFeatureCursor cursor = fc.Search(filter, true);
			Marshal.ReleaseComObject(cursor);

			IPolygon linearPoly = GeometryFactory.CreatePolygon(2600000, 1200000,
			                                                    2600000.0001, 1200010);
			linearPoly.SpatialReference = ((IGeoDataset) fc).SpatialReference;

			filter = GdbQueryUtils.CreateSpatialFilter(
				fc, linearPoly, esriSpatialRelEnum.esriSpatialRelIntersects, true,
				null);

			cursor = fc.Search(filter, true);
			Marshal.ReleaseComObject(cursor);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateSpatialFilterWithSubResolutionEnvelope()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			IEnvelope subResolutionEnv = GeometryFactory.CreateEnvelope(2600000, 1200000,
			                                                            2600000.0001,
			                                                            1200000.0001);

			ISpatialReference spatialReference = DatasetUtils.GetSpatialReference(fc);

			subResolutionEnv.SpatialReference = spatialReference;

			double xyResolution =
				SpatialReferenceUtils.GetXyResolution(Assert.NotNull(spatialReference));

			IGeometry validGeometry;
			string message;
			Assert.False(GdbQueryUtils.IsValidFilterGeometry(
				             subResolutionEnv, xyResolution, out validGeometry, out message),
			             "Sub-resolution polygon should not be valid");
			Assert.NotNull(validGeometry);
			Assert.False(subResolutionEnv == validGeometry,
			             "Corrected geometry must be different to input");

			Assert.True(GdbQueryUtils.IsValidFilterGeometry(
				            validGeometry, xyResolution, out validGeometry, out message),
			            "Corrected geometry should be valid");

			IQueryFilter filter = GdbQueryUtils.CreateSpatialFilter(
				fc, subResolutionEnv, esriSpatialRelEnum.esriSpatialRelIntersects, false,
				spatialReference);

			IFeatureCursor cursor = fc.Search(filter, true);
			Marshal.ReleaseComObject(cursor);

			IEnvelope linearEnv = GeometryFactory.CreateEnvelope(2600000, 1200000,
			                                                     2600000.0001, 1201010);
			linearEnv.SpatialReference = ((IGeoDataset) fc).SpatialReference;

			filter = GdbQueryUtils.CreateSpatialFilter(
				fc, linearEnv, esriSpatialRelEnum.esriSpatialRelIntersects, true,
				null);

			cursor = fc.Search(filter, true);

			Marshal.ReleaseComObject(cursor);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateSpatialFilterWithSubResolutionPolyline()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			IPolyline subResolutionPolyline = GeometryFactory.CreatePolyline(2600000, 1200000,
			                                                                 2600000.0001,
			                                                                 1200000.0001);

			subResolutionPolyline.SpatialReference = ((IGeoDataset) fc).SpatialReference;

			Exception expectedEx = null;
			try
			{
				ISpatialFilter standardFilter = new SpatialFilterClass();
				standardFilter.Geometry = subResolutionPolyline;
				standardFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIndexIntersects;
				// ReSharper disable once UnusedVariable
				IFeatureCursor failingCursor = fc.Search(standardFilter, true);
			}
			catch (Exception ex)
			{
				expectedEx = ex;
			}

			if (RuntimeUtils.Is10_2 || RuntimeUtils.Is10_3 || RuntimeUtils.Is10_4orHigher)
			{
				Assert.Null(expectedEx);
			}
			else
			{
				Assert.NotNull(expectedEx);
			}

			IQueryFilter filter = GdbQueryUtils.CreateSpatialFilter(
				fc, subResolutionPolyline, esriSpatialRelEnum.esriSpatialRelIntersects, false,
				null);

			Assert.True(((ISpatialFilter) filter).FilterOwnsGeometry,
			            "Filter should own geometry due to cloned geometry in GetValidSearchGeometry.");
			Assert.AreEqual(((ISpatialFilter) filter).SearchOrder,
			                esriSearchOrder.esriSearchOrderSpatial,
			                "Default should be spatial.");
			Assert.AreEqual(((ISpatialFilter) filter).SpatialRel,
			                esriSpatialRelEnum.esriSpatialRelIntersects,
			                "Default should be spatial.");

			IFeatureCursor cursor = fc.Search(filter, true);
			Marshal.ReleaseComObject(cursor);

			// test the exact half of the resolution - which is the limit
			double resolution = GeometryUtils.GetXyResolution(fc);
			subResolutionPolyline = GeometryFactory.CreatePolyline(2600000 - 0.00001,
			                                                       1200000 - 0.00001,
			                                                       2600000 + resolution -
			                                                       0.00001,
			                                                       1200000 + resolution -
			                                                       0.00001);

			filter = GdbQueryUtils.CreateSpatialFilter(
				fc, subResolutionPolyline, esriSpatialRelEnum.esriSpatialRelIntersects, false,
				null);

			cursor = fc.Search(filter, true);
			Marshal.ReleaseComObject(cursor);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateSpatialFilterWithMultipatch()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			ISpatialReference spatialReference = DatasetUtils.GetSpatialReference(fc);

			IEnvelope largeEnvelope = GeometryFactory.CreateEnvelope(2600000, 1200000,
			                                                         2601000, 1201000,
			                                                         445, spatialReference);

			IMultiPatch multiPatch =
				GeometryFactory.CreateMultiPatch(GeometryFactory.CreatePolygon(largeEnvelope));

			double xyResolution =
				SpatialReferenceUtils.GetXyResolution(Assert.NotNull(spatialReference));

			// NOTE: Multipatch implements IRelationalOperator since a while!
			IGeometry validGeometry;
			string message;
			Assert.True(GdbQueryUtils.IsValidFilterGeometry(
				            multiPatch, xyResolution, out validGeometry, out message),
			            "Multipatch should be valid");
			Assert.NotNull(validGeometry);
			Assert.True(multiPatch == validGeometry,
			            "Multipatch should be valid");

			Assert.True(GdbQueryUtils.IsValidFilterGeometry(
				            validGeometry, xyResolution, out validGeometry, out message),
			            "Corrected geometry should be valid");

			IQueryFilter filter = GdbQueryUtils.CreateSpatialFilter(
				fc, multiPatch, esriSpatialRelEnum.esriSpatialRelIntersects, false,
				spatialReference);

			Assert.True(GdbQueryUtils.GetFeatures(fc, filter, true).Any(), "No features found.");
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateSpatialFilterWithNonZSimpleGeometry()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			IEnvelope nonZSimpleEnvelope = GeometryFactory.CreateEnvelope(2600000, 1200000,
			                                                              2700000, 1300000);

			GeometryUtils.MakeZAware(nonZSimpleEnvelope);

			Assert.False(((IZAware) nonZSimpleEnvelope).ZSimple, "Must be non-Z-simple");

			ISpatialReference spatialReference =
				Assert.NotNull(DatasetUtils.GetSpatialReference(fc));

			IGeometry validGeometry;
			string message;
			Assert.False(GdbQueryUtils.IsValidFilterGeometry(
				             nonZSimpleEnvelope,
				             SpatialReferenceUtils.GetXyResolution(spatialReference),
				             out validGeometry, out message),
			             "Search geometry should not be valid");

			Assert.NotNull(validGeometry);

			IQueryFilter filter = GdbQueryUtils.CreateSpatialFilter(fc, nonZSimpleEnvelope);

			Assert.True(GdbQueryUtils.GetFeatures(fc, filter, true).Any(), "No features found");
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateSpatialFilterWithEmptyPolygon()
		{
			IFeatureWorkspace ws = OpenTestWorkspace();
			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");

			IPolygon emptyPoly = new PolygonClass();
			emptyPoly.SpatialReference = ((IGeoDataset) fc).SpatialReference;

			IGeometry validGeometry;
			string message;
			Assert.False(GdbQueryUtils.IsValidFilterGeometry(
				             emptyPoly, 0.001, out validGeometry, out message),
			             "Empty polygon should not be valid");

			Assert.Null(validGeometry);

			if (RuntimeUtils.Is10_4orHigher)
			{
				NUnit.Framework.Assert.Catch<Exception>(
					() => GdbQueryUtils.CreateSpatialFilter(fc, emptyPoly));
			}

			ISpatialFilter spatialFilter = new SpatialFilterClass();

			spatialFilter.GeometryField = fc.ShapeFieldName;
			spatialFilter.set_GeometryEx(emptyPoly, true);
			spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

			IFeatureCursor cursor = null;

			if (RuntimeUtils.Is10_1 || RuntimeUtils.Is10_2 || RuntimeUtils.Is10_3 ||
			    RuntimeUtils.Is10_4orHigher)
			{
				NUnit.Framework.Assert.Throws<NullReferenceException>(
					() => cursor = fc.Search(spatialFilter, true));
			}
			else
			{
				cursor = fc.Search(spatialFilter, true);
			}

			if (cursor != null)
				Marshal.ReleaseComObject(cursor);
		}

		[Test]
		[Category(TestCategory.Fast)]
		public void CanQueryDateField()
		{
			const string featureClassName = "points";
			const string fieldName = "date";
			var equalDateTime = new DateTime(2012, 03, 22, 12, 00, 00);
			var greaterDateTime = new DateTime(2012, 03, 22, 12, 00, 01);
			var lowerDateTime = new DateTime(2012, 03, 22, 11, 59, 59);

			IFeatureWorkspace workspace =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(TestData.GetFileGdb93Path());
			IFeatureClass fc = DatasetUtils.OpenFeatureClass(workspace,
			                                                 featureClassName);
			int fieldIndex = fc.Fields.FindField(fieldName);
			IField dateField = fc.Fields.get_Field(fieldIndex);
			Assert.True(dateField.Type == esriFieldType.esriFieldTypeDate,
			            "Wrong FieldType in test data");

			IList<IFeature> rows = GdbQueryUtils.FindList(fc, "OBJECTID = 1");
			Assert.True(rows.Count == 1, "Expected object not in test data");

			DateTime dateTime = Convert.ToDateTime(rows[0].get_Value(fieldIndex));
			_msg.InfoFormat(@"Testing with dateTime: {0}", dateTime);

			Assert.True(dateTime == equalDateTime, "Expected DateTime = DateTime failed.");
			Assert.True(dateTime <= greaterDateTime, "Expected DateTime <= DateTime failed.");
			Assert.True(dateTime < greaterDateTime, "Expected DateTime < DateTime failed.");
			Assert.True(dateTime >= lowerDateTime, "Expected DateTime >= DateTime failed.");
			Assert.True(dateTime > lowerDateTime, "Expected DateTime > DateTime failed.");

			//Test query logic for date field
			const string equals = "=";
			const string lowerOrEquals = "<=";
			const string lower = "<";
			const string greaterOrEquals = ">=";
			const string greater = ">";

			string where = string.Format("{0} {1} {2}", fieldName, equals,
			                             GdbSqlUtils.GetFGDBDateLiteral(equalDateTime));
			Assert.True(GdbQueryUtils.Count(fc, where) == 1, "Query '{0}' fails.", equals);

			where = string.Format("{0} {1} {2}", fieldName, lowerOrEquals,
			                      GdbSqlUtils.GetFGDBDateLiteral(greaterDateTime));
			Assert.True(GdbQueryUtils.Count(fc, where) == 1, "Query '{0}' fails.",
			            lowerOrEquals);

			where = string.Format("{0} {1} {2}", fieldName, lower,
			                      GdbSqlUtils.GetFGDBDateLiteral(greaterDateTime));
			Assert.True(GdbQueryUtils.Count(fc, where) == 1, "Query '{0}' fails.", lower);

			where = string.Format("{0} {1} {2}", fieldName, greaterOrEquals,
			                      GdbSqlUtils.GetFGDBDateLiteral(lowerDateTime));
			Assert.True(GdbQueryUtils.Count(fc, where) == 1, "Query '{0}' fails.",
			            greaterOrEquals);

			where = string.Format("{0} {1} {2}", fieldName, greater,
			                      GdbSqlUtils.GetFGDBDateLiteral(lowerDateTime));
			Assert.True(GdbQueryUtils.Count(fc, where) == 1, "Query '{0}' fails.", greater);
		}
	}
}