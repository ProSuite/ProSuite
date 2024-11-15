using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrGeometryTransformTest
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
		public void CanTransformGeometryToPointsWithAttributes()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrGeomToPoints");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline,
				                   new List<IField>
				                   {
					                   FieldUtils.CreateTextField("TEXT_FIELD", 100, "Some Text"),
					                   FieldUtils.CreateIntegerField("NUMBER_FIELD", "Some Number")
				                   });

			int origTextFieldIdx = lineFc.FindField("TEXT_FIELD");
			int origIntFieldIdx = lineFc.FindField("NUMBER_FIELD");

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).LineTo(70, 70).Curve;
				f.Value[origTextFieldIdx] = "VAL1";
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(69.5, 69.5).LineTo(60, 70)
				                           .Curve;
				f.Value[origTextFieldIdx] = "VAL2";
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 20).LineTo(69.5, 69.5).LineTo(50, 70)
				                           .Curve;
				f.Value[origTextFieldIdx] = "VAL2";
				f.Value[origIntFieldIdx] = 42;
				f.Store();
			}

			{
				TrGeometryToPoints tr = new TrGeometryToPoints(
					ReadOnlyTableFactory.Create(lineFc), GeometryComponent.Vertices);

				// This should be optional and if null, all attributes should be used.
				tr.Attributes = new List<string> { "TEXT_FIELD", "NUMBER_FIELD" };

				TransformedFeatureClass transformedFeatureClass = tr.GetTransformed();

				int textFieldIndex = transformedFeatureClass.FindField("TEXT_FIELD");
				Assert.True(textFieldIndex >= 0);
				int intFieldIndex = transformedFeatureClass.FindField("NUMBER_FIELD");
				Assert.True(intFieldIndex >= 0);

				// Actual querying:
				var transformedBackingDataset =
					(TransformedBackingDataset) transformedFeatureClass.BackingDataset;

				Assert.NotNull(transformedBackingDataset);

				IEnvelope fcExtent = ((IGeoDataset) lineFc).Extent;

				transformedBackingDataset.DataContainer = new UncachedDataContainer(fcExtent);

				ITableFilter filter = new AoTableFilter
				                      {
					                      SubFields = "",
					                      WhereClause = "TEXT_FIELD = 'VAL2'"
				                      };

				List<IReadOnlyRow> foundRows =
					transformedFeatureClass.EnumReadOnlyRows(filter, false).ToList();

				// 2 Original features times 3 vertices:
				Assert.AreEqual(6, foundRows.Count);
				Assert.IsTrue(foundRows.All(r => r.get_Value(textFieldIndex).Equals("VAL2")));
				Assert.AreEqual(3, foundRows.Count(r => r.get_Value(intFieldIndex).Equals(42)));
			}
		}

		[Test]
		public void CanTransformToFootprintWithAttributes()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrFootprint");
			//IFeatureWorkspace ws = TestWorkspaceUtils.CreateTestFgdbWorkspace("TrFootprint");

			IFeatureClass multipatchFc =
				CreateFeatureClass(ws, "multipatchFc", esriGeometryType.esriGeometryMultiPatch,
				                   new List<IField>
				                   {
					                   FieldUtils.CreateTextField("TEXT_FIELD", 100, "Some Text"),
					                   FieldUtils.CreateIntegerField("NUMBER_FIELD", "Some Number")
				                   });

			{
				IFeature f = multipatchFc.CreateFeature();

				var construction = new MultiPatchConstruction();
				construction.StartRing(2600005, 1200004, 1)
				            .Add(2600005, 1200008, 2)
				            .Add(2600008, 1200008, 1)
				            .Add(2600008, 1200004, 1);

				IGeometry multipatchGeometry = construction.MultiPatch;
				GeometryUtils.MakeZAware(multipatchGeometry);
				multipatchGeometry.SpatialReference = DatasetUtils.GetSpatialReference(f);

				multipatchGeometry.SnapToSpatialReference();

				f.Shape = multipatchGeometry;
				f.Store();
			}

			{
				// Explicitly set the list of attributes:
				TrFootprint tr = new TrFootprint(ReadOnlyTableFactory.Create(multipatchFc));
				tr.Attributes = new List<string> { "TEXT_FIELD" };

				TransformedFeatureClass transformedFeatureClass = tr.GetTransformed();

				int textFieldIndex = transformedFeatureClass.FindField("TEXT_FIELD");
				Assert.True(textFieldIndex >= 0);
				int intFieldIndex = transformedFeatureClass.FindField("NUMBER_FIELD");
				Assert.False(intFieldIndex >= 0);
			}

			{
				// Do not set the list of attributes (expect all attributes in the result)
				TrFootprint tr = new TrFootprint(ReadOnlyTableFactory.Create(multipatchFc));

				TransformedFeatureClass transformedFeatureClass = tr.GetTransformed();

				int textFieldIndex = transformedFeatureClass.FindField("TEXT_FIELD");
				Assert.True(textFieldIndex >= 0);
				int intFieldIndex = transformedFeatureClass.FindField("NUMBER_FIELD");
				Assert.True(intFieldIndex >= 0);
			}
		}

		[Test]
		public void CanTransformToFootprintAtTileBoundary()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TrFootprintAtBoundary");

			IFeatureClass multipatchFc =
				CreateFeatureClass(ws, "multipatchFc", esriGeometryType.esriGeometryMultiPatch,
				                   new List<IField>
				                   {
					                   FieldUtils.CreateTextField("TEXT_FIELD", 100, "Some Text"),
					                   FieldUtils.CreateIntegerField("NUMBER_FIELD", "Some Number")
				                   });

			IFeature f = multipatchFc.CreateFeature();

			var construction = new MultiPatchConstruction();
			construction.StartRing(2600005, 1200004, 1)
			            .Add(2600005, 1200008, 2)
			            .Add(2600008, 1200008, 1)
			            .Add(2600008, 1200004, 1);

			IGeometry multipatchGeometry = construction.MultiPatch;
			GeometryUtils.MakeZAware(multipatchGeometry);
			multipatchGeometry.SpatialReference = DatasetUtils.GetSpatialReference(f);

			multipatchGeometry.SnapToSpatialReference();

			f.Shape = multipatchGeometry;
			f.Store();

			// Explicitly set the list of attributes:
			TrFootprint tr = new TrFootprint(ReadOnlyTableFactory.Create(multipatchFc));

			IEnvelope envelope =
				GeometryFactory.CreateEnvelope(2600000, 1200000, 2600010, 1200010);

			string fgdbName = $@"TrTransformToFootprintTileBoundary_{DateTime.Now:yyyyMMdd_HHmmss}";
			string fileGdbFullPath = $@"C:\Temp\UnitTestData\{fgdbName}.gdb";

			TransformedFeatureClass transformedClass = tr.GetTransformed();

			var test = new QaExportTables(new List<IReadOnlyTable>
			                              {
				                              transformedClass
			                              }, fileGdbFullPath)
			           {
				           ExportTileIds = true,
				           ExportTiles = true
			           };

			var runner = new QaContainerTestRunner(5, test);
			runner.Execute(envelope);
			Assert.AreEqual(0, runner.Errors.Count);

			IFeatureWorkspace outputWorkspace =
				WorkspaceUtils.OpenFeatureWorkspace(fileGdbFullPath);

			IFeatureClass outputClass =
				DatasetUtils.OpenFeatureClass(outputWorkspace, transformedClass.Name);

			long featureCount = outputClass.FeatureCount(null);

			// Expected: 1 per tile
			Assert.AreEqual(4, featureCount);
		}

		[Test]
		public void GeometryToPoints()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrGeomToPoints");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);
			IFeatureClass pntFc =
				CreateFeatureClass(ws, "pntFc", esriGeometryType.esriGeometryPoint);
			IFeatureClass refFc =
				CreateFeatureClass(ws, "refFc", esriGeometryType.esriGeometryPoint);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).LineTo(70, 70).Curve;
				f.Store();
			}
			{
				IFeature f = pntFc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(69, 69.5);
				f.Store();
			}
			{
				IFeature f = refFc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(69, 70);
				f.Store();
			}

			{
				TrGeometryToPoints tr = new TrGeometryToPoints(
					ReadOnlyTableFactory.Create(lineFc), GeometryComponent.Vertices);

				QaPointNotNear test = new QaPointNotNear(
					tr.GetTransformed(),
					ReadOnlyTableFactory.Create(refFc), 2);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				TrGeometryToPoints tr = new TrGeometryToPoints(
					ReadOnlyTableFactory.Create(pntFc), GeometryComponent.EntireGeometry);
				QaPointNotNear test = new QaPointNotNear(
					tr.GetTransformed(), ReadOnlyTableFactory.Create(refFc), 2);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				IList<InvolvedRow> involvedRows = runner.Errors[0].InvolvedRows;
				Assert.AreEqual(2, involvedRows.Count);

				foreach (InvolvedRow involvedRow in involvedRows)
				{
					Assert.IsTrue(involvedRow.TableName == "pntFc" ||
					              involvedRow.TableName == "refFc");
				}
			}
			{
				TrGeometryToPoints tr = new TrGeometryToPoints(
					ReadOnlyTableFactory.Create(lineFc), GeometryComponent.LineEndPoints);
				QaPointNotNear test = new QaPointNotNear(
					tr.GetTransformed(), ReadOnlyTableFactory.Create(refFc), 2);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				// Check involved rows. They must be from a 'real' feature class, not form a transformed feature class.
				IList<InvolvedRow> involvedRows = runner.Errors[0].InvolvedRows;
				Assert.AreEqual(2, involvedRows.Count);

				foreach (InvolvedRow involvedRow in involvedRows)
				{
					Assert.IsTrue(involvedRow.TableName == "lineFc" ||
					              involvedRow.TableName == "refFc");
				}
			}
		}

		[Test]
		public void MultilineToLine()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrMultiline");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(70, 70)
				                           .MoveTo(10, 0).LineTo(20, 5).Curve;
				f.Store();
			}

			TrMultilineToLine tr = new TrMultilineToLine(ReadOnlyTableFactory.Create(lineFc));
			QaMinLength test = new QaMinLength(tr.GetTransformed(), 100);
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			test.SetConstraint(0, $"{TrMultilineToLine.AttrPartIndex} = 0");
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void MultipolyToPoly()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrMultipoly");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape =
					CurveConstruction
						.StartPoly(0, 0).LineTo(30, 0).LineTo(30, 30).LineTo(0, 30).LineTo(0, 0)
						.MoveTo(10, 10).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10).LineTo(10, 10)
						.MoveTo(22, 22).LineTo(22, 24).LineTo(24, 24).LineTo(24, 22).LineTo(22, 22)
						.MoveTo(13, 13).LineTo(17, 13).LineTo(17, 17).LineTo(13, 17).LineTo(13, 13)
						.MoveTo(40, 40).LineTo(50, 40).LineTo(50, 50).LineTo(40, 50).LineTo(40, 40)
						.ClosePolygon();
				f.Store();
			}

			TrMultipolygonToPolygon tr =
				new TrMultipolygonToPolygon(ReadOnlyTableFactory.Create(lineFc));
			QaMinArea test = new QaMinArea(tr.GetTransformed(), 850);
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);
			}
			{
				tr.TransformedParts = PolygonPart.OuterRings;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				tr.TransformedParts = PolygonPart.InnerRings;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				tr.TransformedParts = PolygonPart.AllRings;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(4, runner.Errors.Count);
			}
		}

		[Test]
		public void MultipolyToPolyCache()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrMultipoly");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape =
					CurveConstruction
						.StartPoly(0, 0).LineTo(30, 0).LineTo(30, 30).LineTo(0, 30).LineTo(0, 0)
						.MoveTo(10, 10).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10).LineTo(10, 10)
						.MoveTo(22, 22).LineTo(22, 24).LineTo(24, 24).LineTo(24, 22).LineTo(22, 22)
						.MoveTo(13, 13).LineTo(17, 13).LineTo(17, 17).LineTo(13, 17).LineTo(13, 13)
						.MoveTo(40, 40).LineTo(50, 40).LineTo(50, 50).LineTo(40, 50).LineTo(40, 40)
						.ClosePolygon();
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape =
					CurveConstruction
						.StartPoly(-10, -10).LineTo(90, -10).LineTo(90, 90).LineTo(-10, 90)
						.LineTo(-10, -10).ClosePolygon();
				f.Store();
			}

			TrMultipolygonToPolygon tr =
				new TrMultipolygonToPolygon(ReadOnlyTableFactory.Create(lineFc));
			tr.SetConstraint(0, "ObjectId = 1");
			QaOverlapsOther test =
				new QaOverlapsOther(tr.GetTransformed(), ReadOnlyTableFactory.Create(lineFc));
			test.SetConstraint(1, "ObjectId = 2");
			{
				tr.TransformedParts = PolygonPart.AllRings;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
			{
				tr.TransformedParts = PolygonPart.AllRings;
				var runner = new QaContainerTestRunner(20, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		[Test]
		public void CanAccessAttributes()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrAttributes");

			IFeatureClass polyFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolygon,
				                   new[] { FieldUtils.CreateIntegerField("IntField") });

			{
				IFeature f = polyFc.CreateFeature();
				f.Value[polyFc.FindField("IntField")] = 12;
				f.Shape =
					CurveConstruction
						.StartPoly(0, 0).LineTo(30, 0).LineTo(30, 30).LineTo(0, 30).LineTo(0, 0)
						.MoveTo(10, 10).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10).LineTo(10, 10)
						.MoveTo(22, 22).LineTo(22, 24).LineTo(24, 24).LineTo(24, 22).LineTo(22, 22)
						.MoveTo(13, 13).LineTo(17, 13).LineTo(17, 17).LineTo(13, 17).LineTo(13, 13)
						.MoveTo(40, 40).LineTo(50, 40).LineTo(50, 50).LineTo(40, 50).LineTo(40, 40)
						.ClosePolygon();
				f.Store();
			}

			TrMultipolygonToPolygon trMp2p =
				new TrMultipolygonToPolygon(ReadOnlyTableFactory.Create(polyFc));
			//			trMp2p.Attributes = new List<string> { "IntField" };
			{
				QaConstraint test = new QaConstraint(
					trMp2p.GetTransformed(),
					$"IntField < 12 AND {TrMultipolygonToPolygon.AttrInnerRingIndex} = 1 OR {TrMultipolygonToPolygon.AttrOuterRingIndex} = 1");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(
					trMp2p.GetTransformed(),
					$"IntField < 12 AND {TrMultipolygonToPolygon.AttrInnerRingIndex} = 1 OR {TrMultipolygonToPolygon.AttrOuterRingIndex} = 1");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}

			TrPolygonToLine trP2l = new TrPolygonToLine(trMp2p.GetTransformed());
			{
				QaConstraint test = new QaConstraint(
					trP2l.GetTransformed(),
					$"IntField < 12 AND {TrMultipolygonToPolygon.AttrInnerRingIndex} = 1 OR {TrMultipolygonToPolygon.AttrOuterRingIndex} = 1");

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}

			TrMultilineToLine trMl2l = new TrMultilineToLine(trP2l.GetTransformed());
			{
				string constr =
					$"IntField < 12 " +
					$" AND {TrMultipolygonToPolygon.AttrInnerRingIndex} = 1" +
					$" OR {TrMultipolygonToPolygon.AttrOuterRingIndex} = 1" +
					$" OR {TrMultilineToLine.AttrPartIndex} <> 0";
				QaConstraint test = new QaConstraint(
					trMl2l.GetTransformed(), constr);

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}

			TrGeometryToPoints trG2p =
				new TrGeometryToPoints(trP2l.GetTransformed(), GeometryComponent.Vertices);
			{
				string constr =
					$"IntField < 12 " +
					$" AND {TrMultipolygonToPolygon.AttrInnerRingIndex} = 1" +
					$" OR {TrMultipolygonToPolygon.AttrOuterRingIndex} = 1" +
					$" OR {TrGeometryToPoints.AttrPartIndex} <> 0" +
					$" OR {TrGeometryToPoints.AttrVertexIndex} <> 0";
				QaConstraint test = new QaConstraint(
					trG2p.GetTransformed(), constr);

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		private IFeatureClass CreateFeatureClass(IFeatureWorkspace ws, string name,
		                                         esriGeometryType geometryType,
		                                         IList<IField> customFields = null)
		{
			List<IField> fields = new List<IField>();
			fields.Add(FieldUtils.CreateOIDField());
			if (customFields != null)
			{
				fields.AddRange(customFields);
			}

			bool hasZ = geometryType == esriGeometryType.esriGeometryMultiPatch;
			fields.Add(FieldUtils.CreateShapeField(
				           "Shape", geometryType,
				           SpatialReferenceUtils.CreateSpatialReference
				           ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				            true), 1000, hasZ));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));
			return fc;
		}
	}
}
