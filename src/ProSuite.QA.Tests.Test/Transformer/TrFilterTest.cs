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
using ProSuite.QA.Tests.Transformers.Filters;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrFilterTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanGetFilteredLinesWithPolyIntersecting()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrOnlyIntersectingRows");

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

				var runner = new QaContainerTestRunner(1000, intersectsSelf) {KeepGeometry = true};
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
				List<string> realTableNames = new List<string> {"lineFc"};
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
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
					new[] {FieldUtils.CreateIntegerField("Nr_Line")});
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] {FieldUtils.CreateIntegerField("Nr_Poly")});

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

				var runner = new QaContainerTestRunner(1000, intersectsSelf) {KeepGeometry = true};
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
				List<string> realTableNames = new List<string> {"lineFc"};
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
		public void CanGetMultiFilteredAndCombination()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrMultiFilter");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] {FieldUtils.CreateIntegerField("Nr_Line")});
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] {FieldUtils.CreateIntegerField("Nr_Poly")});
			IFeatureClass pointFc =
				CreateFeatureClass(
					ws, "pointFc", esriGeometryType.esriGeometryPoint,
					new[] {FieldUtils.CreateIntegerField("Nr_Point")});

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
			               {TransformerName = "filtered_by_poly"};

			var trOnLine = new TrOnlyIntersectingFeatures(ReadOnlyTableFactory.Create(pointFc),
			                                              ReadOnlyTableFactory.Create(lineFc))
			               {TransformerName = "filtered_by_line"};

			var inputFilters = new List<IReadOnlyFeatureClass>
			                   {
				                   trInPoly.GetTransformed(),
				                   trOnLine.GetTransformed()
			                   };

			var trCombined =
				new TrMultiFilter(ReadOnlyTableFactory.Create(pointFc), inputFilters, null)
				{TransformerName = "filtered_by_both"};

			{
				QaConstraint test = new QaConstraint(trCombined.GetTransformed(), "Nr_Point = 0");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows:
				QaError error = runner.Errors[0];

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				List<string> realTableNames = new List<string> {"pointFc"};
				CheckInvolvedRows(error.InvolvedRows, 1, realTableNames);
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
