using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using ProSuite.QA.Tests.Transformers.Filters;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrFilterTest
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
		public void CanGetFilteredLinesWithPolyIntersecting()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrOnlyIntersectingRows");

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
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, 20).LineTo(20, 20)
				                           .LineTo(20, 0).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, -70).LineTo(-70, -70)
				                           .LineTo(-70, 0).ClosePolygon();
				f.Store();
			}

			var tr = new TrOnlyIntersectingFeatures(ReadOnlyTableFactory.Create(lineFc),
			                                        ReadOnlyTableFactory.Create(polyFc));

			// The name is used as the table name and thus necessary
			((ITableTransformer) tr).TransformerName = "filtered_lines";
			{
				var intersectsSelf = new QaIntersectsSelf(tr.GetTransformed());
				//intersectsSelf.SetConstraint(0, "polyFc.Nr_Poly < 10");

				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();

				// Theoretically they intersect, but one was filtered out:
				Assert.AreEqual(0, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Line < 0");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc" };
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
			}
		}

		[Test]
		public void CanGetFilteredLinesWithPolyIntersectingLargeArea()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrOnlyIntersectingRows");

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
				f.Value[1] = 1;
				f.Shape = CurveConstruction.StartLine(50, 50).LineTo(500, 50).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 2;
				f.Shape = CurveConstruction.StartLine(50, 40).LineTo(600, 40).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, 100).LineTo(100, 100)
				                           .LineTo(100, 0).ClosePolygon();
				f.Store();
			}

			var tr = new TrOnlyIntersectingFeatures(ReadOnlyTableFactory.Create(lineFc),
			                                        ReadOnlyTableFactory.Create(polyFc));

			((ITableTransformer) tr).TransformerName = "filtered_lines";
			{
				var test = new QaMinLength(tr.GetTransformed(), 500);

				var runner = new QaContainerTestRunner(1000, test)
				             { KeepGeometry = true };
				runner.Execute();

				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				var test = new QaMinLength(tr.GetTransformed(), 500);

				var runner = new QaContainerTestRunner(1000, test)
				             { KeepGeometry = true };
				runner.Execute(GeometryFactory.CreateEnvelope(400, 0, 600, 100));

				// interesecting polygon is outside test area 
				Assert.AreEqual(0, runner.Errors.Count);
			}

			var trFullSearch = new TrOnlyIntersectingFeatures(ReadOnlyTableFactory.Create(lineFc),
			                                                  ReadOnlyTableFactory.Create(polyFc));
			trFullSearch.FilteringSearchOption = TrSpatiallyFiltered.SearchOption.All;
			{
				var test = new QaMinLength(trFullSearch.GetTransformed(), 500);

				var runner = new QaContainerTestRunner(1000, test)
				             { KeepGeometry = true };
				runner.Execute(GeometryFactory.CreateEnvelope(400, 0, 600, 100));

				// interesecting polygon is outside test area, but is searched everywhere 
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanGetFilteredLinesWithPolyDisjoint()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrOnlyDisjointRows");

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
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, 20).LineTo(20, 20)
				                           .LineTo(20, 0).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, -70).LineTo(-70, -70)
				                           .LineTo(-70, 0).ClosePolygon();
				f.Store();
			}

			var tr = new TrOnlyDisjointFeatures(ReadOnlyTableFactory.Create(lineFc),
			                                    ReadOnlyTableFactory.Create(polyFc));

			// The name is used as the table name and thus necessary
			((ITableTransformer) tr).TransformerName = "filtered_lines";
			{
				var intersectsSelf = new QaIntersectsSelf(tr.GetTransformed());
				//intersectsSelf.SetConstraint(0, "polyFc.Nr_Poly < 10");

				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();

				// Theoretically they intersect, but one was filtered out:
				Assert.AreEqual(0, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "Nr_Line < 0");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc" };
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
			}
		}

		[Test]
		public void CanGetFilteredLinesWithPolyDisjointLargeArea()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrOnlyDisjointRows");

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
				f.Value[1] = 1;
				f.Shape = CurveConstruction.StartLine(50, 50).LineTo(500, 50).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 2;
				f.Shape = CurveConstruction.StartLine(150, 40).LineTo(600, 40).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, 100).LineTo(100, 100)
				                           .LineTo(100, 0).ClosePolygon();
				f.Store();
			}

			var tr = new TrOnlyDisjointFeatures(ReadOnlyTableFactory.Create(lineFc),
			                                    ReadOnlyTableFactory.Create(polyFc));

			((ITableTransformer) tr).TransformerName = "filtered_lines";
			{
				var test = new QaMaxLength(tr.GetTransformed(), 400);

				var runner = new QaContainerTestRunner(1000, test)
				             { KeepGeometry = true };
				runner.Execute();

				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				var test = new QaMaxLength(tr.GetTransformed(), 400);

				var runner = new QaContainerTestRunner(1000, test)
				             { KeepGeometry = true };
				runner.Execute(GeometryFactory.CreateEnvelope(400, 0, 600, 100));

				// disjoint polygon is outside test area 
				Assert.AreEqual(2, runner.Errors.Count);
			}

			var trFullSearch = new TrOnlyDisjointFeatures(ReadOnlyTableFactory.Create(lineFc),
			                                              ReadOnlyTableFactory.Create(polyFc));
			trFullSearch.FilteringSearchOption = TrSpatiallyFiltered.SearchOption.All;
			{
				var test = new QaMaxLength(trFullSearch.GetTransformed(), 400);

				var runner = new QaContainerTestRunner(1000, test)
				             { KeepGeometry = true };
				runner.Execute(GeometryFactory.CreateEnvelope(400, 0, 600, 100));

				// disjoint polygon is outside test area, but is searched everywhere 
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanGetFilteredLinesWithPolyContained()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrOnlyIntersectingRows");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });

			{
				// Contained:
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 1;
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(10, 10).Curve;
				f.Store();
			}
			{
				// Not contained:
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 2;
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 11;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, 20).LineTo(20, 20)
				                           .LineTo(20, 0).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, -70).LineTo(-70, -70)
				                           .LineTo(-70, 0).ClosePolygon();
				f.Store();
			}

			var tr = new TrOnlyContainedFeatures(ReadOnlyTableFactory.Create(lineFc),
			                                     ReadOnlyTableFactory.Create(polyFc));

			// The name is used as the table name and thus necessary
			((ITableTransformer) tr).TransformerName = "filtered_lines";
			{
				var intersectsSelf = new QaIntersectsSelf(tr.GetTransformed());
				//intersectsSelf.SetConstraint(0, "polyFc.Nr_Poly < 10");

				var runner = new QaContainerTestRunner(1000, intersectsSelf)
				             { KeepGeometry = true };
				runner.Execute();

				// Theoretically they intersect, but one was filtered out:
				Assert.AreEqual(0, runner.Errors.Count);
			}
			{
				IList<QaError> errors = ExecuteQaConstraint(tr, "Nr_Line < 0");

				Assert.AreEqual(1, errors.Count);

				// Check involved rows:
				QaError error = errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "lineFc" };
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
			}

			// Test constraint on input of transformed table:
			{
				// Already filters input:
				tr.SetConstraint(0, "Nr_Line < 0");

				IList<QaError> errors = ExecuteQaConstraint(tr, "Nr_Line < 0");

				Assert.AreEqual(0, errors.Count);

				// Remove constraint:
				tr.SetConstraint(0, null);
				errors = ExecuteQaConstraint(tr, "Nr_Line < 0");

				Assert.AreEqual(1, errors.Count);
			}

			// Test constraint on filtering table:
			{
				// Leaves only the non-containing poly in containing
				tr.SetConstraint(1, "Nr_Poly <> 11");

				IList<QaError> errors = ExecuteQaConstraint(tr, "Nr_Line < 0");

				// Nothing is contained -> no feature -> error is gone:
				Assert.AreEqual(0, errors.Count);

				// Remove constraint:
				tr.SetConstraint(1, null);
				errors = ExecuteQaConstraint(tr, "Nr_Line < 0");

				// Filtered again, no error
				Assert.AreEqual(1, errors.Count);
			}
		}

		[Test]
		public void CanGetFilteredLinesWithPolyContainedNonSpatialSearch()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrOnlyIntersectingRows");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });

			{
				// Contained:
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 1;
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(10, 10).Curve;
				f.Store();
			}
			{
				// Not contained:
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 2;
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 11;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, 20).LineTo(20, 20)
				                           .LineTo(20, 0).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, -70).LineTo(-70, -70)
				                           .LineTo(-70, 0).ClosePolygon();
				f.Store();
			}

			var tr = new TrOnlyContainedFeatures(ReadOnlyTableFactory.Create(lineFc),
			                                     ReadOnlyTableFactory.Create(polyFc));

			IEnvelope envelope = GeometryFactory.CreateEnvelope(-100, -100, 100, 100);

			FilteredFeatureClass filteredFeatureClass = tr.GetTransformed();

			ITableFilter tableFilter = new AoTableFilter()
			                           {
				                           WhereClause = "OBJECTID < 100"
			                           };

			Assert.NotNull(filteredFeatureClass.BackingDataset);
			var transformedBackingDataset =
				(TransformedBackingData) filteredFeatureClass.BackingDataset;

			transformedBackingDataset.DataSearchContainer = new UncachedDataContainer(envelope);

			// By now the DatasetContainer should have been assigned -> test non-spatial filter:
			var filteredRows = filteredFeatureClass.EnumReadOnlyRows(tableFilter, false).ToList();
			Assert.AreEqual(1, filteredRows.Count);
		}

		[Test]
		public void CanGetFilteredWithCombinedFilters()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrCombinedFilter");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Poly") });
			IFeatureClass pointFc =
				CreateFeatureClass(
					ws, "pointFc", esriGeometryType.esriGeometryPoint,
					new[] { FieldUtils.CreateIntegerField("Nr_Point") });

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
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, 20).LineTo(20, 20)
				                           .LineTo(20, 0).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(0, -70).LineTo(-70, -70)
				                           .LineTo(-70, 0).ClosePolygon();
				f.Store();
			}

			{
				// In polygon AND on line
				IFeature f = pointFc.CreateFeature();
				f.Value[1] = 1;
				f.Shape = GeometryFactory.CreatePoint(10, 10);
				f.Store();
			}
			{
				// In polygon but NOT on line
				IFeature f = pointFc.CreateFeature();
				f.Value[1] = 1;
				f.Shape = GeometryFactory.CreatePoint(10, 12);
				f.Store();
			}
			{
				// On line but NOT in polygon
				IFeature f = pointFc.CreateFeature();
				f.Value[1] = 1;
				f.Shape = GeometryFactory.CreatePoint(69.5, 69.5);
				f.Store();
			}

			// The names are used as the table name and thus necessary
			var trInPoly = new TrOnlyIntersectingFeatures(ReadOnlyTableFactory.Create(pointFc),
			                                              ReadOnlyTableFactory.Create(polyFc))
			               { TransformerName = "filtered_by_poly" };

			var trOnLine = new TrOnlyIntersectingFeatures(ReadOnlyTableFactory.Create(pointFc),
			                                              ReadOnlyTableFactory.Create(lineFc))
			               { TransformerName = "filtered_by_line" };

			var inputFilters = new List<IReadOnlyFeatureClass>
			                   {
				                   trInPoly.GetTransformed(),
				                   trOnLine.GetTransformed()
			                   };

			// Without expression (i.e. implicit AND condition)
			var trCombined =
				new TrCombinedFilter(ReadOnlyTableFactory.Create(pointFc), inputFilters, null)
				{ TransformerName = "filtered_by_both" };
			{
				QaConstraint test = new QaConstraint(trCombined.GetTransformed(), "Nr_Point = 0");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "pointFc" };
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
			}

			// Now with explicit AND condition
			string expression = "filtered_by_poly AND filtered_by_line";
			trCombined =
				new TrCombinedFilter(ReadOnlyTableFactory.Create(pointFc), inputFilters, expression)
				{ TransformerName = "filtered_by_both" };
			{
				QaConstraint test = new QaConstraint(trCombined.GetTransformed(), "Nr_Point = 0");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "pointFc" };
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
			}

			// Now with OR expression: All 3 are returned (and generate an error)
			expression = "filtered_by_poly OR filtered_by_line";
			trCombined =
				new TrCombinedFilter(ReadOnlyTableFactory.Create(pointFc), inputFilters, expression)
				{
					TransformerName = "filtered_by_either"
				};

			{
				QaConstraint test = new QaConstraint(trCombined.GetTransformed(), "Nr_Point = 0");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "pointFc" };
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
			}

			// Now with NOT - OR expression: 1 feature is on polygon but not on line
			expression = "filtered_by_poly AND (NOT filtered_by_line)";
			trCombined =
				new TrCombinedFilter(ReadOnlyTableFactory.Create(pointFc), inputFilters, expression)
				{
					TransformerName = "filtered_by_either"
				};

			{
				QaConstraint test = new QaConstraint(trCombined.GetTransformed(), "Nr_Point = 0");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> { "pointFc" };
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
			}
		}

		[Test]
		public void IssueTOP_5658()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("IssueTOP_5658");
			IFeatureClass sgFc = CreateFeatureClass(
				ws, "TLM_STEHENDES_GEWAESSER", esriGeometryType.esriGeometryPolyline,
				new List<IField> { FieldUtils.CreateTextField("TLM_GEWAESSERLAUF_UUID", 36) });
			IReadOnlyFeatureClass sg = ReadOnlyTableFactory.Create(sgFc);

			IFeatureClass fgFc = CreateFeatureClass(
				ws, "TLM_FLIESSGEWAESSER", esriGeometryType.esriGeometryPolyline,
				new List<IField> { });
			IReadOnlyFeatureClass fg = ReadOnlyTableFactory.Create(fgFc);

			IFeatureClass gkFc = CreateFeatureClass(
				ws, "TLM_GEWAESSERNETZKNOTEN", esriGeometryType.esriGeometryPoint,
				new List<IField> { FieldUtils.CreateIntegerField("OBJEKTART") });
			IReadOnlyFeatureClass gk = ReadOnlyTableFactory.Create(gkFc);

			ITable glTbl =
				DatasetUtils.CreateTable(
					ws, "TLM_GEWAESSER_LAUF",
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateTextField("UUID", 36),
					FieldUtils.CreateTextField("GEWISS_NR", 20)
				);
			IReadOnlyTable gl = ReadOnlyTableFactory.Create(glTbl);

			IPoint p0 = GeometryFactory.CreatePoint(2600000, 1200000);
			IPoint p1 = GeometryFactory.CreatePoint(2601000, 1201000);
			IPoint p2 = GeometryFactory.CreatePoint(2600000, 1202000);
			{
				IRow row = glTbl.CreateRow();
				row.Value[gl.FindField("UUID")] = "A";
				row.Value[gl.FindField("GEWISS_NR")] = "GWN_A";
				row.Store();
			}
			{
				IFeature f = sgFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(p0).LineTo(p1)
				                           .Curve;
				f.Value[sgFc.FindField("TLM_GEWAESSERLAUF_UUID")] = "A";
				f.Store();
			}

			{
				IFeature f = fgFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(p1).LineTo(p2).Curve;
				f.Store();
			}

			{
				IFeature f = gkFc.CreateFeature();
				f.Shape = p0;
				f.Value[gkFc.FindField("OBJEKTART")] = 1;
				f.Store();
			}
			{
				IFeature f = gkFc.CreateFeature();
				f.Shape = p1;
				f.Value[gkFc.FindField("OBJEKTART")] = 2;
				f.Store();
			}
			{
				IFeature f = gkFc.CreateFeature();
				f.Shape = p2;
				f.Value[gkFc.FindField("OBJEKTART")] = 3;
				f.Store();
			}

			TrTableJoinInMemory trMemJoin =
				new TrTableJoinInMemory(sg, gl, "TLM_GEWAESSER_LAUF_UUID", "UUID",
				                        JoinType.InnerJoin)
				{ TransformerName = "Tr_JoinInMemory_StehendesGewaesser_Gewaesserlauf" };
			TrOnlyDisjointFeatures trDisjoint =
				new TrOnlyDisjointFeatures((IReadOnlyFeatureClass) trMemJoin.GetTransformed(), fg)
				{ TransformerName = "Tr_OnlyDisjoint_STGW_FGW" };

			TrDissolve trDissolveDisjoint =
				new TrDissolve(trDisjoint.GetTransformed())
				{
					NeighborSearchOption = SearchOption.All,
					Attributes = new List<string>
					             { "COUNT(TLM_STEHENDES_GEWAESSER_OBJECTID) AS ANZAHL_LAEUFE" },
					GroupBy = new List<string> { "GEWISS_NR" },

					TransformerName = "Tr_Dissolve_OnlyDisjoint_STGW_FGW_See_AnzahlLaeufe"
				};
			TrSpatialJoin trSpatJoin =
				new TrSpatialJoin(trDissolveDisjoint.GetTransformed(), gk)
				{
					OuterJoin = false,
					NeighborSearchOption = SearchOption.All,
					Grouped = true,
					T1Attributes = new List<string>
					               {
						               "SUM(LOOP_JUNCTION) AS ANZAHL_LOOP_JUNCTIONS",
						               "SUM(SECONDARY_JUNCTION) AS ANZAHL_SECONDARY_JUNCTIONS"
					               },
					T1CalcAttributes = new List<string>()
					                   {
						                   "IIF(OBJEKTART=1,1,0) AS LOOP_JUNCTION",
						                   "IIF(OBJEKTART=2,1,0) AS SECONDARY_JUNCTION"
					                   },

					TransformerName = "Tr_SpatialJoin_Dissolve_STGW_Gewässernetzknoten"
				};
			QaConstraint test = new QaConstraint(trSpatJoin.GetTransformed(),
			                                     "ANZAHL_LAEUFE = 2 AND ANZAHL_LOOP_JUNCTIONS = 1 AND ANZAHL_SECONDARY_JUNCTIONS = 1");
		}

		[Test]
		public void IssueTOP_5735()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("IssueTOP_5735");
			IFeatureClass fcGrundriss =
				CreateFeatureClass(
					ws, "tlm_grundriss", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });

			IFeatureClass fcKanton =
				CreateFeatureClass(
					ws, "tlm_kanton", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr_Line") });

			IFeatureClass fcDachgrundriss =
				CreateFeatureClass(
					ws, "tlm_dach_grundriss", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("uuid") });

			IFeatureClass fcDach =
				CreateFeatureClass(
					ws, "tlm_dach", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("tlm_dach_grundriss_uuid") });

			{
				IFeature f = fcDachgrundriss.CreateFeature();
				f.Value[1] = 1;
				f.Shape = CurveConstruction.StartPoly(10, 10).LineTo(20, 10).LineTo(20, 20)
				                           .LineTo(10, 20).LineTo(10, 10).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = fcDach.CreateFeature();
				f.Value[1] = 1;
				f.Shape = CurveConstruction.StartPoly(10, 10).LineTo(20, 10).LineTo(20, 20)
				                           .LineTo(10, 20).LineTo(10, 10).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = fcKanton.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(1, 1).LineTo(99, 1).LineTo(99, 99)
				                           .LineTo(1, 99).LineTo(1, 1).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = fcGrundriss.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(30, 30).LineTo(40, 30).LineTo(40, 40)
				                           .LineTo(30, 40).LineTo(30, 30).ClosePolygon();
				f.Store();
			}


			IReadOnlyFeatureClass roDachGrundriss = ReadOnlyTableFactory.Create(fcDachgrundriss);
			IReadOnlyFeatureClass roGrundriss = ReadOnlyTableFactory.Create(fcGrundriss);
			IReadOnlyFeatureClass roKanton = ReadOnlyTableFactory.Create(fcKanton);
			IReadOnlyFeatureClass roDach = ReadOnlyTableFactory.Create(fcDach);

			TrTableJoinInMemory trJoinDgDach = new TrTableJoinInMemory(
				roDachGrundriss, roDach, "uuid", "tlm_dach_grundriss_uuid", JoinType.InnerJoin);

			TrOnlyContainedFeatures trOcf = new TrOnlyContainedFeatures(
				(IReadOnlyFeatureClass)trJoinDgDach.GetTransformed(),
				roKanton);


			QaMustIntersectOther test = new QaMustIntersectOther(trOcf.GetTransformed(), roGrundriss, "");

			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}

		}
		private static IList<QaError> ExecuteQaConstraint(TrOnlyContainedFeatures tr,
		                                                  string constraint)
		{
			QaConstraint test = new QaConstraint(tr.GetTransformed(), constraint);

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();

			return runner.Errors;
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
