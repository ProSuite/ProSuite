using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrTableJoinTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void VerifySdeTableJoin()
		{
			IWorkspace workspace = TestDataUtils.OpenTopgisTlm();
			IFeatureClass lineFc =
				((IFeatureWorkspace) workspace).OpenFeatureClass(
					"TOPGIS_TLM.TLM_STRASSE");

			ITable table =
				((IFeatureWorkspace) workspace).OpenTable(
					"TOPGIS_TLM.TLM_STRASSENROUTE");

			string relName = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE";

			TrTableJoin joined =
				new TrTableJoin(ReadOnlyTableFactory.Create(lineFc),
				                ReadOnlyTableFactory.Create(table), relName, JoinType.InnerJoin);

			VerifyTableJoin(joined);
		}

		[Test]
		public void VerifyFgdbTableJoin()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("VerifyFgdbTableJoin");

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFeatureClass pointFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TLM_STRASSE", null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField("OBJEKTART"),
				FieldUtils.CreateIntegerField("FKEY"),
				FieldUtils.CreateShapeField(
					"SHAPE", esriGeometryType.esriGeometryPoint,
					sref, 1000));

			ITable table = DatasetUtils.CreateTable(ws, "TLM_STRASSENROUTE",
			                                        FieldUtils.CreateOIDField(),
			                                        FieldUtils.CreateIntegerField("OBJEKTART"),
			                                        FieldUtils.CreateTextField(
				                                        "ROUTENNUMMER", 100));

			TestWorkspaceUtils.CreateSimple1NRelationship(
				ws, "TLM_STRASSENROUTE_STRASSE",
				table, (ITable) pointFc,
				table.OIDFieldName, "FKEY");

			for (int iRow = 0; iRow < 20; iRow++)
			{
				string routenNummer = $"A{iRow}";
				IRow row = table.CreateRow();
				row.Value[1] = 5;
				row.Value[2] = routenNummer;
				row.Store();

				for (int iFeature = 0; iFeature < 20; iFeature++)
				{
					IFeature f = pointFc.CreateFeature();
					f.Value[1] = routenNummer == "A1" ? 2 : 5;
					f.Value[2] = row.OID;
					f.Shape = GeometryFactory.CreatePoint(2603000 + iRow, 1203000 + iFeature);
					f.Store();
				}

				{
					IFeature f = pointFc.CreateFeature();
					f.Value[1] = routenNummer == "A1" ? 2 : 5;
					f.Value[2] = row.OID;
					f.Shape = GeometryFactory.CreatePoint(2603000 + iRow, 1203000);
					f.Store();
				}
			}

			string relName = "TLM_STRASSENROUTE_STRASSE";

			TrTableJoin joined =
				new TrTableJoin(ReadOnlyTableFactory.Create(pointFc),
				                ReadOnlyTableFactory.Create(table), relName, JoinType.InnerJoin);

			VerifyTableJoin(joined);
		}

		private void VerifyTableJoin(TrTableJoin joined)
		{
			IEnvelope area = GeometryFactory.CreateEnvelope(2601000, 1201000, 2604000, 1204000);
			{
				// full qualified field names without table filter
				QaConstraint test =
					new QaConstraint(joined.GetTransformed(),
					                 "TOPGIS_TLM.TLM_STRASSE.OBJEKTART = 2");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute(area);

				Assert.IsTrue(runner.Errors.Count > 3);
			}
			{
				// full qualified field names without table filter
				QaInteriorIntersectsSelf test =
					new QaInteriorIntersectsSelf((IReadOnlyFeatureClass) joined.GetTransformed());

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute(area);

				Assert.IsTrue(runner.Errors.Count > 3);
			}
			{
				// full qualified field names
				QaConstraint test =
					new QaConstraint(joined.GetTransformed(),
					                 "TOPGIS_TLM.TLM_STRASSE.OBJEKTART = 2");
				test.SetConstraint(0, "TOPGIS_TLM.TLM_STRASSEN_NAME.ROUTENNUMMER = 'A1'");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute(area);

				Assert.IsTrue(runner.Errors.Count < 3);
			}
			{
				// reduce field names
				QaConstraint test =
					new QaConstraint(joined.GetTransformed(), "TLM_STRASSE.OBJEKTART = 2");
				test.SetConstraint(0, "ROUTENNUMMER = 'A1'");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute(area);

				Assert.IsTrue(runner.Errors.Count < 3);
			}
			{
				// reduce field names
				QaConstraint test =
					new QaConstraint(joined.GetTransformed(), "Dummy.TLM_STRASSE.OBJEKTART = 2");
				test.SetConstraint(0, "Dummy.ROUTENNUMMER = 'A1'");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute(area);

				Assert.IsTrue(runner.Errors.Count < 3);
			}
			{
				// non-unique field names
				QaConstraint test =
					new QaConstraint(joined.GetTransformed(), "OBJEKTART = 2");
				test.SetConstraint(0, "ROUTENNUMMER = 'A1'");

				var runner = new QaContainerTestRunner(1000, test);
				bool failed = false;
				try
				{
					runner.Execute(area);
				}
				catch
				{
					failed = true;
				}

				Assert.IsTrue(failed);
			}
		}
	}
}
