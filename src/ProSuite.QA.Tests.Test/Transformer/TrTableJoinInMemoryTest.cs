using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrTableJoinInMemoryTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanInnerJoinOneToOne()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrTableJoinInMemory");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] {FieldUtils.CreateIntegerField("Nr_Line")});
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] {FieldUtils.CreateIntegerField("Nr_Poly")});
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 14;
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(20, 50).LineTo(40, 50)
				                           .LineTo(40, 0).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 14;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			TrTableJoinInMemory tr = new TrTableJoinInMemory(
				ReadOnlyTableFactory.Create(polyFc),
				ReadOnlyTableFactory.Create(lineFc),
				"Nr_Poly", "Nr_Line", JoinType.InnerJoin);

			// The name is used as the table name and thus necessary
			((ITableTransformer) tr).TransformerName = "test_join";
			{
				var intersectsSelf =
					new QaIntersectsSelf((IReadOnlyFeatureClass) tr.GetTransformed());
				var runner = new QaContainerTestRunner(1000, intersectsSelf) {KeepGeometry = true};
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> {"lineFc", "polyFc"};
				CheckInvolvedRows(error.InvolvedRows, 4, realTableNames);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "polyFc.Nr_Poly > 12");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> {"lineFc", "polyFc"};
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}
		}

		[Test]
		public void CanInnerJoinManyToMany()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrTableJoinInMemory");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] {FieldUtils.CreateIntegerField("Nr_Line")});
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] {FieldUtils.CreateIntegerField("Nr_Poly")});

			IFeatureClass bridgeTable =
				CreateFeatureClass(
					ws, "bridgeTable", esriGeometryType.esriGeometryPoint,
					new[]
					{
						FieldUtils.CreateIntegerField("Nr_Poly"),
						FieldUtils.CreateIntegerField("Nr_Line")
					});
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 1;
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 2;
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 11;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(20, 50).LineTo(40, 50)
				                           .LineTo(40, 0).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			{
				IFeature f = bridgeTable.CreateFeature();
				f.Value[1] = 11;
				f.Value[2] = 1;
				f.Shape = GeometryFactory.CreatePoint(0, 0);
				f.Store();
			}
			{
				IFeature f = bridgeTable.CreateFeature();
				f.Value[1] = 12;
				f.Value[2] = 2;
				f.Shape = GeometryFactory.CreatePoint(0, 0);
				f.Store();
			}

			TrTableJoinInMemory tr = new TrTableJoinInMemory(
				                         ReadOnlyTableFactory.Create(polyFc),
				                         ReadOnlyTableFactory.Create(lineFc),
				                         "Nr_Poly", "Nr_Line", JoinType.InnerJoin)
			                         {
				                         ManyToManyTable = ReadOnlyTableFactory.Create(bridgeTable),
				                         ManyToManyTableLeftKey = "Nr_Poly",
				                         ManyToManyTableRightKey = "Nr_Line"
			                         };

			// The name is used as the table name and thus necessary
			((ITableTransformer) tr).TransformerName = "test_join";
			{
				var intersectsSelf =
					new QaIntersectsSelf((IReadOnlyFeatureClass) tr.GetTransformed());
				//intersectsSelf.SetConstraint(0, "polyFc.Nr_Poly < 10");

				var runner = new QaContainerTestRunner(1000, intersectsSelf) {KeepGeometry = true};
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> {"lineFc", "polyFc"};
				CheckInvolvedRows(error.InvolvedRows, 4, realTableNames);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "polyFc.Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> {"lineFc", "polyFc"};
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}
			{
				// TODO: This has currently no effect and should be implemented
				tr.SetConstraint(0, "Nr_Poly < 10");

				QaConstraint test = new QaConstraint(tr.GetTransformed(), "polyFc.Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		private static void CheckInvolvedRows(IList<InvolvedRow> involvedRows, int expectedCount,
		                                      IList<string> realTableNames)
		{
			Assert.AreEqual(expectedCount, involvedRows.Count);

			foreach (InvolvedRow involvedRow in involvedRows)
			{
				Assert.IsTrue(realTableNames.Contains(involvedRow.TableName));
			}
		}

		private static IFeatureClass CreateFeatureClass(IFeatureWorkspace ws, string name,
		                                                esriGeometryType geometryType,
		                                                IList<IField> customFields = null)
		{
			List<IField> fields = new List<IField>();
			fields.Add(FieldUtils.CreateOIDField());
			if (customFields != null)
			{
				fields.AddRange(customFields);
			}

			fields.Add(FieldUtils.CreateShapeField(
				           "Shape", geometryType,
				           SpatialReferenceUtils.CreateSpatialReference
				           ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				            true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));

			return fc;
		}
	}
}
