using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrTableJoinInMemoryTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanInnerJoinOneToOne()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace(
					"TrTableJoinInMemory_CanInnerJoinOneToOne");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });
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
				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 4, realTableNames);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 12");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}
		}

		[Test]
		public void CanInnerJoinManyToOne()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace(
					"TrTableJoinInMemory_CanInnerJoinManyToOne");

			// lineFc ist the right table, containing the unique key
			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });
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

			// polyFc is the left table, containing several keys with value 12
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(20, 50).LineTo(40, 50)
				                           .LineTo(40, 0).ClosePolygon();
				f.Store();
			}
			{
				// Same foreign key again but completely different geometry
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(100, 1).LineTo(120, 51).LineTo(140, 51)
				                           .LineTo(140, 1).ClosePolygon();
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
				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 4, realTableNames);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 12");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}

			IReadOnlyFeatureClass transformed =
				(IReadOnlyFeatureClass) ((ITableTransformer) tr).GetTransformed();

			int rowCount = transformed.EnumRows(null, false).Count();
			Assert.AreEqual(3, rowCount);

			int distinctCount =
				transformed.EnumRows(null, true).Select(r => r.OID).Distinct().Count();
			Assert.AreEqual(3, distinctCount);
		}

		[Test]
		public void CanInnerJoinOneToMany()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace(
					"TrTableJoinInMemory_CanInnerJoinOneToMany");

			// lineFc is the right table, containing several keys with value 12
			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				// Same line again, completely different geometry
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartLine(100, 1).LineTo(169.5, 79.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 14;
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}

			// polyFc is the left table, containing the unique key (which will be expanded)
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

			GdbTable joinedTable = tr.GetTransformed();
			List<IReadOnlyRow> joinedRows = joinedTable.EnumReadOnlyRows(null, false).ToList();
			Assert.AreEqual(3, joinedRows.Count);
			//Assert.AreEqual(3, joinedRows.Distinct().Count());

			// The name is used as the table name and thus necessary
			((ITableTransformer) tr).TransformerName = "test_join";
			{
				var intersectsSelf =
					new QaIntersectsSelf((IReadOnlyFeatureClass) tr.GetTransformed());
				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 4, realTableNames);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 12");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}
		}

		[Test]
		public void CanInnerJoinManyToMany()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace(
					"TrTableJoinInMemory_CanInnerJoinManyToMany");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });

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
				// Line without relationship:
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 5;
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
				// poly with no relationship:
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 17;
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

				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 4, realTableNames);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}
			{
				tr.SetConstraint(0, "Nr_Poly < 10");

				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		[Test]
		public void CanInnerJoinManyToManyWithConstraint()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace(
					"TrTableJoinInMemory_CanInnerJoinManyToManyWithConstraint");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[]
					{
						FieldUtils.CreateIntegerField("Nr_Line"),
						FieldUtils.CreateTextField("LINE_TYPE", 12)
					});
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[]
					{
						FieldUtils.CreateIntegerField("Nr_Poly"),
						FieldUtils.CreateTextField("POLY_TYPE", 12),
						FieldUtils.CreateDoubleField("POLY_PRIO")
					});

			IFeatureClass bridgeTable =
				CreateFeatureClass(
					ws, "bridgeTable", esriGeometryType.esriGeometryPoint,
					new[]
					{
						FieldUtils.CreateIntegerField("Nr_Poly"),
						FieldUtils.CreateTextField("Nr_Line", 12)
					});
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 1;
				f.Value[2] = "lowerleft";
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 2;
				f.Value[2] = "upperright";
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				// Line without relationship:
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 5;
				f.Value[2] = "upperright";
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			// Polygon features
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 11;
				f.Value[2] = "small";
				f.Value[3] = "3.3";
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(20, 50).LineTo(40, 50)
				                           .LineTo(40, 0).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Value[2] = "large";
				f.Value[3] = "4.4";
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}
			{
				// poly with no relationship:
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 17;
				f.Value[2] = "large";
				f.Value[3] = "5.5";
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

			tr.SetConstraint(0, "POLY_TYPE in ('small')");
			tr.TransformerName = "test_join";

			{
				// Just search spatially without container (to test the constraint filter)
				// to reproduce TOP-5639 (the subfields consisting of the key-field  only
				// prevents correct filtering:
				var memoryJoinedFc = (IReadOnlyFeatureClass) tr.GetTransformed();

				int count = memoryJoinedFc.EnumRows(null, true).Count();

				Assert.AreEqual(1, count);
			}
			tr.SetConstraint(1, "LINE_TYPE <> 'does not exist'");
			{
				var memoryJoinedFc = (IReadOnlyFeatureClass) tr.GetTransformed();

				int count = memoryJoinedFc.EnumRows(null, true).Count();

				Assert.AreEqual(1, count);
			}
			{
				// The same with really restricted sub-fields and a non-restrictive where clause
				// that references non-sub-fields.
				var memoryJoinedFc = (IReadOnlyFeatureClass) tr.GetTransformed();
				var filter = GdbQueryUtils.CreateFeatureClassFilter(
					GeometryFactory.CreateEnvelope(0, 0, 1000, 1000,
					                               memoryJoinedFc.SpatialReference));
				filter.SubFields = "OBJECTID";
				filter.WhereClause =
					"POLY_PRIO < 1000 AND LINE_TYPE in ('lowerleft', 'upperright')";

				int count = memoryJoinedFc.EnumRows(filter, true).Count();

				Assert.AreEqual(1, count);
			}
			tr.SetConstraint(1, "LINE_TYPE = 'upperright'");
			{
				var memoryJoinedFc = (IReadOnlyFeatureClass) tr.GetTransformed();

				int count = memoryJoinedFc.EnumRows(null, true).Count();

				Assert.AreEqual(0, count);
			}

			// Test with QA test:
			tr.SetConstraint(1, null);
			{
				var memoryJoinedFc = (IReadOnlyFeatureClass) tr.GetTransformed();

				var intersectsSelf =
					new QaIntersectsSelf(memoryJoinedFc);
				//intersectsSelf.SetConstraint(0, "POLY_TYPE in ('small', 'large')");

				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
			{
				// The small poly is still in the result set, now 'filter' the right table
				tr.SetConstraint(1, "LINE_TYPE <> 'does not exist'");
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}
			{
				// And now with a proper filter:
				tr.SetConstraint(1, "LINE_TYPE = 'upperright'");
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
			{
				tr.SetConstraint(0, "Nr_Poly < 10");

				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		[Test]
		public void CanLeftJoinManyToMany()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace(
					"TrTableJoinInMemory_CanLeftJoinManyToMany");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });

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
				// Line without relationship:
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 5;
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
				// poly with no relationship:
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 17;
				f.Shape = CurveConstruction.StartPoly(70, 70).LineTo(70, 80).LineTo(80, 80)
				                           .LineTo(80, 70).ClosePolygon();
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
				                         "Nr_Poly", "Nr_Line", JoinType.LeftJoin)
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

				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 4, realTableNames);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}
			{
				tr.SetConstraint(0, "Nr_Poly < 10");

				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 11");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		[Test]
		public void CanInnerJoinJoinManyToManyWithNullKeys()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateTestFgdbWorkspace(
					"TrTableJoinInMemory_CanInnerJoinJoinManyToManyWithNullKeys");

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			const string leftTableKey = "STRASSENNUMMER";

			IFeatureClass pointFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TLM_POINTS", null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField("OBJEKTART"),
				FieldUtils.CreateIntegerField(leftTableKey),
				FieldUtils.CreateShapeField(
					"SHAPE", esriGeometryType.esriGeometryPoint,
					sref, 1000));

			const string rightTableKey = "ROUTENNUMMER";

			ITable routeTable = DatasetUtils.CreateTable(ws, "TLM_STRASSENROUTE",
			                                             FieldUtils.CreateOIDField(),
			                                             FieldUtils.CreateIntegerField("OBJEKTART"),
			                                             FieldUtils.CreateTextField(
				                                             rightTableKey, 100));

			// Rather than a 'proper' many-to-many relationship, we create a normal table as bridge table:
			ITable associationTable = DatasetUtils.CreateTable(
				ws, "TLM_STRASSENROUTE_STRASSE",
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField(leftTableKey),
				FieldUtils.CreateTextField(rightTableKey, 100));

			for (int iRow = 0; iRow < 20; iRow++)
			{
				string routenNummer = $"A{iRow}";
				IRow row = routeTable.CreateRow();
				row.Value[1] = 5;
				row.Value[2] = routenNummer;
				row.Store();

				for (int iFeature = 0; iFeature < 20; iFeature++)
				{
					int roadNumber = iRow * 1000 + iFeature;
					IFeature f = pointFc.CreateFeature();
					// Objektart:
					f.Value[1] = iFeature % 3;
					// Strassennummer:
					f.Value[2] = roadNumber;
					f.Shape = GeometryFactory.CreatePoint(2603000 + iRow, 1203000 + iFeature);
					f.Store();

					IRow a = associationTable.CreateRow();
					a.Value[1] = roadNumber;
					a.Value[2] = routenNummer;
					a.Store();
				}

				// Feature with relationship to association table, but association table has no link
				// to route table (null foreign key, TOP-5877):
				{
					int roadIdWithoutRoute = 100 + iRow;

					IFeature f = pointFc.CreateFeature();
					f.Value[2] = roadIdWithoutRoute;
					f.Shape = GeometryFactory.CreatePoint(2603000 + iRow, 1203000);
					f.Store();

					// No 'proper' relationship to other side, but null foreign key (TOP-5877);
					IRow a = associationTable.CreateRow();
					a.Value[1] = roadIdWithoutRoute;
					a.Store();
				}
				// Feature with no relationship to association table:
				{
					IFeature f = pointFc.CreateFeature();
					f.Value[1] = iRow % 5;
					f.Shape = GeometryFactory.CreatePoint(2603000 + iRow, 1203000);
					f.Store();
				}
				// Association table entry with link to route table, but no link to point table:
				{
					IRow a = associationTable.CreateRow();
					a.Value[2] = routenNummer;
					a.Store();
				}
			}

			TrTableJoinInMemory tr =
				new TrTableJoinInMemory(ReadOnlyTableFactory.Create(pointFc),
				                        ReadOnlyTableFactory.Create(routeTable), leftTableKey,
				                        rightTableKey, JoinType.InnerJoin);
			tr.ManyToManyTable = ReadOnlyTableFactory.Create(associationTable);
			tr.ManyToManyTableLeftKey = leftTableKey;
			tr.ManyToManyTableRightKey = rightTableKey;

			// Tests with the transformed table (TOP-5877):
			// The name is used as the table name and thus necessary
			((ITableTransformer) tr).TransformerName = "test_join";

			Assert.AreEqual(400, tr.GetTransformed().RowCount(null));

			// Normal join:
			AssertJoinedRowExists(tr, 20);

			// Association record OID 21 has no link to the route table (ok in left join)
			AssertJoinedRowExists(tr, 21);

			// Association record OID 22 has no link to the point table (currently not ok, right join is not yet supported)
			Assert.Throws<InvalidOperationException>(() => AssertJoinedRowExists(tr, 22));

			// Row count: 400 (the rest is filtered due to inner join)
			{
				QaRowCount test = new QaRowCount(tr.GetTransformed(), 400, 400);

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}

			// Within box of lower left quadrant
			{
				var intersectsSelf =
					new QaWithinBox((IReadOnlyFeatureClass) tr.GetTransformed(), 2603000, 1203000,
					                2603000 + 9, 1203000 + 9);
				//intersectsSelf.SetConstraint(0, "polyFc.Nr_Poly < 10");

				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();

				// 100 are inside the box, 300 are outside:
				Assert.AreEqual(300, runner.Errors.Count);
			}

			// QaConstraint:
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "STRASSENNUMMER > 5");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(6, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not from a transformed feature class.
				List<string> realTableNames =
					new List<string> { "TLM_STRASSENROUTE", "TLM_POINTS" };
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
			}
			{
				tr.SetConstraint(0, "STRASSENNUMMER < 10");

				QaConstraint test =
					new QaConstraint(tr.GetTransformed(),
					                 "TLM_STRASSENROUTE_OBJEKTART IS NOT NULL");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		private static void AssertJoinedRowExists(TrTableJoinInMemory tr, int id)
		{
			JoinedBackingDataset joinedDataset =
				(JoinedBackingDataset) tr.GetTransformed().BackingDataset;
			Assert.NotNull(joinedDataset);

			VirtualRow joinedRow = joinedDataset.GetRow(id);
			Assert.NotNull(joinedRow);
		}

		/// <summary>
		/// This test currently fails because (most) transformers cannot deal with non-spatial queries
		/// which are necessary in order to get the rows from the 'rightTable'.
		/// Options:
		/// - All transformers check if the filter is non-spatial and if so, circumvent the DataContainer
		/// - The DataContainer naively or cleverly handles non-spatial queries by
		///    - either reading and pass-through-yielding the DB rows directly.
		///    - or, in case there is a where-clause (typically 'KEY_FIELD IN (long-list-of-uuids)')
		///      gets the OIDs only, checks which rows are already cached and which need to be properly
		///      fetched from the DB.
		/// </summary>
		[Test]
		[Ignore("Requires not yet implemented functionality")]
		public void CanJoinPreTransformedTables()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrTableJoinInMemory");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });
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

			TrGeometryToPoints trPoly2Pt =
				new TrGeometryToPoints(
					ReadOnlyTableFactory.Create(polyFc), GeometryComponent.Vertices)
				{ TransformerName = "polgon2Points" };
			TrGeometryToPoints trLine2Pt =
				new TrGeometryToPoints(
					ReadOnlyTableFactory.Create(lineFc), GeometryComponent.Vertices)
				{ TransformerName = "line2Points" };

			TrTableJoinInMemory tr = new TrTableJoinInMemory(
				trPoly2Pt.GetTransformed(),
				trLine2Pt.GetTransformed(),
				"Nr_Poly", "Nr_Line", JoinType.InnerJoin);

			tr.SetConstraint(1, "Nr_Line < 10");

			// The name is used as the table name and thus necessary
			((ITableTransformer) tr).TransformerName = "test_join";
			{
				var intersectsSelf =
					new QaIntersectsSelf((IReadOnlyFeatureClass) tr.GetTransformed());
				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 4, realTableNames);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Poly > 12");
				IFilterEditTest ft = test;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc", "polyFc" };
				CheckInvolvedRows(error.InvolvedRows, 2, realTableNames);
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

		[Test]
		public void CanHandleOutOfTileRequests()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrTableJoinInMemory");

			IFeatureClass featureClass1 =
				CreateFeatureClass(
					ws, "polyFc1", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr") });

			IFeatureClass featureClass2 =
				CreateFeatureClass(
					ws, "polyFc2", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr") });

			ReadOnlyFeatureClass roPolyFc1 = ReadOnlyTableFactory.Create(featureClass1);
			ReadOnlyFeatureClass roPolyFc2 = ReadOnlyTableFactory.Create(featureClass2);

			double tileSize = 100;
			double x = 2600000;
			double y = 1200000;

			// Left of first tile, NOT within search distance
			IFeature leftOfFirst = CreateFeature(featureClass1, x - 20, y + 30, x - 15, y + 40);
			IFeature leftOfFirstIntersect =
				CreateFeature(featureClass2, x - 20, y + 30, x - 15, y + 40);

			// Inside first tile:
			IFeature insideFirst = CreateFeature(featureClass1, x, y, x + 10, y + 10);
			IFeature insideFirstIntersect = CreateFeature(featureClass2, x, y, x + 10, y + 10);

			// Right of first tile, NOT within search distance
			IFeature rightOfFirst =
				CreateFeature(featureClass1, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);
			IFeature rightOfFirstIntersect =
				CreateFeature(featureClass2, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);

			// Left of second tile, NOT within the search distance:
			IFeature leftOfSecond =
				CreateFeature(featureClass1, x + tileSize - 20, y, x + tileSize - 15, y + 10);
			IFeature leftOfSecondIntersect =
				CreateFeature(featureClass2, x + tileSize - 20, y, x + tileSize - 15, y + 10);

			TrTableJoinInMemory tr =
				new TrTableJoinInMemory(roPolyFc1, roPolyFc2, "OBJECTID", "OBJECTID",
				                        JoinType.InnerJoin)
				{
					// NOTE: The search logic should work correctly even if search option is Tile! (e.g. due to downstream transformers)
					//NeighborSearchOption = TrSpatialJoin.SearchOption.All
				};

			var transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);

			var test =
				new ContainerOutOfTileDataAccessTest(transformedClass)
				{
					SearchDistanceIntoNeighbourTiles = 50
				};

			test.TileProcessed = (tile, outsideTileFeatures) =>
			{
				if (tile.CurrentEnvelope.XMin == x && tile.CurrentEnvelope.YMin == y)
				{
					// first tile: the leftOfFirst and rightOfFirst
					Assert.AreEqual(2, outsideTileFeatures.Count);

					foreach (IReadOnlyRow outsideTileFeature in outsideTileFeatures)
					{
						Assert.True(InvolvedRowUtils.GetInvolvedRows(outsideTileFeature).All(
							            r => r.OID == leftOfFirst.OID ||
							                 r.OID == leftOfFirstIntersect.OID ||
							                 r.OID == rightOfFirst.OID ||
							                 r.OID == rightOfFirstIntersect.OID));
					}
				}

				if (tile.CurrentEnvelope.XMin == x + tileSize && tile.CurrentEnvelope.YMin == y)
				{
					// second tile: leftOfSecond
					Assert.AreEqual(1, outsideTileFeatures.Count);

					foreach (IReadOnlyRow outsideTileFeature in outsideTileFeatures)
					{
						Assert.True(InvolvedRowUtils.GetInvolvedRows(outsideTileFeature).All(
							            r => r.OID == leftOfSecond.OID ||
							                 r.OID == leftOfSecondIntersect.OID));
					}
				}

				return 0;
			};

			test.SetSearchDistance(10);

			var container = new TestContainer { TileSize = tileSize };

			container.AddTest(test);

			ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass1);

			IEnvelope aoi = GeometryFactory.CreateEnvelope(
				2600000, 1200000.00, 2600000 + 2 * tileSize, 1200000.00 + tileSize, sr);

			// First, using FullGeometrySearch:
			test.UseFullGeometrySearch = true;
			container.Execute(aoi);

			// Now simulate full tile loading:
			test.UseFullGeometrySearch = false;
			test.UseTileEnvelope = true;
			container.Execute(aoi);
		}

		private static IFeature CreateFeature(IFeatureClass featureClass,
		                                      double xMin, double yMin,
		                                      double xMax, double yMax)
		{
			ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass);

			IFeature row = featureClass.CreateFeature();
			row.Shape = GeometryFactory.CreatePolygon(xMin, yMin, xMax, yMax, sr);
			row.Store();
			return row;
		}

		private static void WriteFieldNames(IReadOnlyTable targetTable)
		{
			for (int i = 0; i < targetTable.Fields.FieldCount; i++)
			{
				IField field = targetTable.Fields.Field[i];

				Console.WriteLine(field.Name);
			}
		}
	}
}
