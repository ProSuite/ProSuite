using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaRequiredFieldsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanTestOevNameFull()
		{
			var ws = (IFeatureWorkspace)TestUtils.OpenSDEWorkspaceOracle();
			ITable oevNameTbl = ws.OpenTable("TOPGIS_TLM.TLM_OEV_Name");
			var roOevName = ReadOnlyTableFactory.Create(oevNameTbl);

			QaRequiredFields test = new QaRequiredFields(roOevName, new[] { "SPRACHCODE" });

			int nMissing = test.Execute();
			Assert.True(nMissing < 100);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanTestOevNameGeometryRelated()
		{
			var ws = (IFeatureWorkspace)TestUtils.OpenSDEWorkspaceOracle();
			ITable oevNameTbl = ws.OpenTable("TOPGIS_TLM.TLM_OEV_Name");
			IRelationshipClass relClass = ws.OpenRelationshipClass("TOPGIS_TLM.TLM_HALTESTELLE_NAME");

			IReadOnlyTable roOevName = ReadOnlyTableFactory.Create(oevNameTbl);

			QaRequiredFields test = new QaRequiredFields(roOevName, new[] { "SPRACHCODE" });

			IEnvelope testPerimeter = GeometryFactory.CreateEnvelope(2600000, 1200000, 2610000, 1210000);
			HashSet<long> rowOids = GdbQueryUtils.GetRelatedOids((IObjectClass) oevNameTbl,
			                                                     testPerimeter,
			                                                     new[] { relClass });

			int nMissing = 0;
			foreach (var oid in rowOids)
			{
				nMissing += test.Execute(roOevName.GetRow(oid));
			}
			Assert.True(nMissing < 10);
		}

		[Test]
		public void VerifySimple()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("verify");
			string fieldName = "txt";
			ITable tbl = DatasetUtils.CreateTable(
				ws, "Recs", FieldUtils.CreateOIDField(), FieldUtils.CreateTextField(fieldName, 20));

			{
				IRow r = tbl.CreateRow();
				r.Value[1] = "hallo";
				r.Store();
			}
			{
				IRow r = tbl.CreateRow();
				r.Value[1] = "  ";
				r.Store();
			}
			{
				IRow r = tbl.CreateRow();
				r.Value[1] = "";
				r.Store();
			}
			{
				IRow r = tbl.CreateRow();
				r.Store();
			}

			IReadOnlyTable roTbl = ReadOnlyTableFactory.Create(tbl);
			QaRequiredFields test = new QaRequiredFields(roTbl, new[] { fieldName });

			int nMissing = test.Execute();
			Assert.AreEqual(2, nMissing);
		}
	}
}
