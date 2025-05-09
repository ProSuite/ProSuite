using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Test;
using ProSuite.Commons.Testing;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Transformers;
using Path = System.IO.Path;
using TestUtils = ProSuite.QA.Container.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrFootprintTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanGetFootprintsRealDataPerformance()
		{
			string path = TestDataPreparer.ExtractZip("GebZueriberg.gdb.zip").GetPath();

			IFeatureWorkspace featureWorkspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);
			IFeatureClass buildings =
				DatasetUtils.OpenFeatureClass(featureWorkspace, "TLM_GEBAEUDE");

			ReadOnlyFeatureClass roBuildings = ReadOnlyTableFactory.Create(buildings);

			var transformer = new TrFootprint(roBuildings);

			TransformedFeatureClass featureClass = transformer.GetTransformed();

			Assert.NotNull(featureClass.BackingDataset);
			var transformedBackingDataset =
				(TransformedBackingDataset) featureClass.BackingDataset;

			transformedBackingDataset.DataContainer = new UncachedDataContainer(roBuildings.Extent);

			bool originalValue = IntersectionUtils.UseCustomIntersect;

			try
			{
				IntersectionUtils.UseCustomIntersect = false;
				Console.WriteLine("AO implementation:");
				MeasureFootprintCreation(featureClass);
				IntersectionUtils.UseCustomIntersect = true;
				Console.WriteLine("Geom implementation:");
				MeasureFootprintCreation(featureClass);
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = originalValue;
			}

			// Result on Ryzen 9 (x86, Debug build):
			// AO implementation:
			// Created 906 rings in 30.4556375s. 0 Geometries could not be processed
			// Geom implementation:
			// Created 908 rings in 2.9820412s. 0 Geometries could not be processed

			// The difference in ring count is due to duplicate inner rings and AO
			// probably only regards the orientation instead of the ring type (inner).
		}

		[Test]
		[Category(TestCategory.FixMe)]
		public void CanGetFootprintsRealData()
		{
			string path = TestDataPreparer.ExtractZip("GebZueriberg.gdb.zip").Overwrite().GetPath();

			IFeatureWorkspace featureWorkspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);
			IFeatureClass buildings =
				DatasetUtils.OpenFeatureClass(featureWorkspace, "TLM_GEBAEUDE");

			ReadOnlyFeatureClass roBuildings = ReadOnlyTableFactory.Create(buildings);

			TransformedFeatureClass featureClass1 = GetFootprintClass(roBuildings);
			TransformedFeatureClass featureClass2 = GetFootprintClass(roBuildings);

			string outDir = Path.GetDirectoryName(path);
			Assert.NotNull(outDir);
			IWorkspaceName wsName = WorkspaceUtils.CreateFileGdbWorkspace(outDir, "Output.gdb");
			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(wsName.PathName);

			IFeatureClass outFeatures1 =
				CreateOutFeatureClass(ws, "geomOut1", roBuildings.SpatialReference);

			IFeatureClass outFeatures2 =
				CreateOutFeatureClass(ws, "geomOut2", roBuildings.SpatialReference);

			int differerentResultsCount =
				CompareFootprints(featureClass1, featureClass2, outFeatures1, outFeatures2);

			// 2 Are different due to AO using orientation instead of ring type
			Assert.LessOrEqual(differerentResultsCount, 2);
		}

		[Test]
		[Category(TestCategory.FixMe)]
		public void CanGetFootprintsRealDataVerticals()
		{
			string path = TestDataPreparer.ExtractZip("GebkoerperSmallAreas.gdb.zip").Overwrite()
			                              .GetPath();

			IFeatureWorkspace featureWorkspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);
			IFeatureClass buildings =
				DatasetUtils.OpenFeatureClass(featureWorkspace, "geometry_issue");

			ReadOnlyFeatureClass roBuildings = ReadOnlyTableFactory.Create(buildings);

			TransformedFeatureClass featureClass1 = GetFootprintClass(roBuildings);
			TransformedFeatureClass featureClass2 = GetFootprintClass(roBuildings);

			string outDir = Path.GetDirectoryName(path);
			Assert.NotNull(outDir);
			IWorkspaceName wsName = WorkspaceUtils.CreateFileGdbWorkspace(outDir, "Output.gdb");
			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(wsName.PathName);

			IFeatureClass outFeatures1 =
				CreateOutFeatureClass(ws, "geomOut1", roBuildings.SpatialReference);

			IFeatureClass outFeatures2 =
				CreateOutFeatureClass(ws, "geomOut2", roBuildings.SpatialReference);

			int differerentResultsCount =
				CompareFootprints(featureClass1, featureClass2, outFeatures1, outFeatures2);

			// Count the empty features in fc1:
			int emptyCount = 0;
			foreach (IFeature feature in GdbQueryUtils.GetFeatures(outFeatures1, true))
			{
				IGeometry geometry = feature.Shape;
				if (geometry.IsEmpty)
				{
					emptyCount++;
				}
			}

			// TODO: Check the difference compared to commit 8b547680d169f7c611fb97dffc3d1c4f54713a96
			// and make sure it is no regression. But most likely it is not.
			Assert.AreEqual(111, emptyCount);

			// Many are different due to AO returning envelope if footprint is no polygon
			// In some cases the equality check seems to incorrectly state that they are different. 
			Assert.LessOrEqual(differerentResultsCount, 112);
		}

		private static IFeatureClass CreateOutFeatureClass(IFeatureWorkspace ws,
		                                                   string name,
		                                                   ISpatialReference sr)
		{
			IField shapeField =
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon, sr, 0, true);

			IFeatureClass outFeatures1 = DatasetUtils.CreateSimpleFeatureClass(ws, name, null,
				shapeField);

			return outFeatures1;
		}

		private static TransformedFeatureClass GetFootprintClass(IReadOnlyFeatureClass roBuildings)
		{
			var transformer1 = new TrFootprint(roBuildings);

			TransformedFeatureClass featureClass = transformer1.GetTransformed();

			Assert.NotNull(featureClass.BackingDataset);
			var transformedBackingDataset =
				(TransformedBackingDataset) featureClass.BackingDataset;

			transformedBackingDataset.DataContainer = new UncachedDataContainer(roBuildings.Extent);

			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) featureClass.SpatialReference).Clone();

			srTolerance.XYTolerance = 0.0001;

			return featureClass;
		}

		private static void MeasureFootprintCreation(VirtualTable featureClass)
		{
			Stopwatch watch = Stopwatch.StartNew();

			int ringCount = 0;
			int nullCount = 0;
			foreach (IReadOnlyRow footprint in featureClass.EnumReadOnlyRows(null, true))
			{
				IGeometry geometry = ((IReadOnlyFeature) footprint).Shape;

				if (geometry != null)
				{
					ringCount += GeometryUtils.GetPartCount(geometry);
				}
				else
				{
					nullCount++;
				}
			}

			watch.Stop();

			Console.WriteLine("Created {0} rings in {1}s. {2} Geometries could not be processed",
			                  ringCount, watch.Elapsed.TotalSeconds, nullCount);
		}

		private static int CompareFootprints(VirtualTable featureClass1,
		                                     VirtualTable featureClass2,
		                                     IFeatureClass resultClass1,
		                                     IFeatureClass ressultClass2)
		{
			Stopwatch watch = Stopwatch.StartNew();
			int rowCount = 0;
			int differentCount = 0;
			int aoGeometryIsEnvelope = 0;
			IReadOnlyTable table2 = featureClass2;
			using (var cursor2 = table2.EnumRows(null, false).GetEnumerator())

				foreach (IReadOnlyRow footprint in featureClass1.EnumReadOnlyRows(null, false))
				{
					IGeometry geometry1 = ((IReadOnlyFeature) footprint).Shape;

					IntersectionUtils.UseCustomIntersect = false;

					cursor2.MoveNext();
					IReadOnlyFeature feature2 = (IReadOnlyFeature) cursor2.Current;
					IGeometry geometry2 = feature2?.Shape;

					IntersectionUtils.UseCustomIntersect = true;

					rowCount++;

					//GeometryUtils.SetXyTolerance(geometry1, 0.001);
					//GeometryUtils.SetXyTolerance(geometry2, 0.001);
					bool areEqualInXY = GeometryUtils.AreEqualInXY(geometry1, geometry2);

					if (! areEqualInXY)
					{
						differentCount++;
						Console.WriteLine($"Differences detected in OID {feature2?.OID}");

						// Check if AO geometry is the envelope
						if (geometry2 != null &&
						    GeometryUtils.GetPointCount(geometry2) == 5)
						{
							double area = ((IArea) geometry2).Area;
							double envelopeArea = ((IArea) geometry2.Envelope).Area;

							if (MathUtils.AreEqual(area, envelopeArea, area))
							{
								aoGeometryIsEnvelope++;
							}
						}
					}

					IFeature outFeature1 = resultClass1.CreateFeature();
					outFeature1.Shape = geometry1;
					outFeature1.Store();

					IFeature outFeature2 = ressultClass2.CreateFeature();
					outFeature2.Shape = geometry2;
					outFeature2.Store();
				}

			watch.Stop();

			Console.WriteLine("Roughly {0} AO geometries are just envelopes.",
			                  aoGeometryIsEnvelope);

			return differentCount;
		}

		[Test]
		public void CanHandleOutOfTileRequests()
		{
			IFeatureWorkspace ws =
				Commons.AO.Test.TestWorkspaceUtils.CreateInMemoryWorkspace("TrFootprint");

			IFeatureClass featureClass =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryMultiPatch,
					new[] { FieldUtils.CreateIntegerField("Nr") });

			ReadOnlyFeatureClass roPolyFc = ReadOnlyTableFactory.Create(featureClass);

			double tileSize = 100;
			double x = 2600000;
			double y = 1200000;

			// Left of first tile, NOT within search distance
			IFeature leftOfFirst = CreateFeature(featureClass, x - 20, y + 30, x - 15, y + 40);
			IFeature leftOfFirstIntersect =
				CreateFeature(featureClass, x - 20, y + 30, x - 15, y + 40);

			// Inside first tile:
			IFeature insideFirst = CreateFeature(featureClass, x, y, x + 10, y + 10);
			IFeature insideFirstIntersect = CreateFeature(featureClass, x, y, x + 10, y + 10);

			// Right of first tile, NOT within search distance
			IFeature rightOfFirst =
				CreateFeature(featureClass, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);
			IFeature rightOfFirstIntersect =
				CreateFeature(featureClass, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);

			// Left of second tile, NOT within the search distance:
			IFeature leftOfSecond =
				CreateFeature(featureClass, x + tileSize - 20, y, x + tileSize - 15, y + 10);
			IFeature leftOfSecondIntersect =
				CreateFeature(featureClass, x + tileSize - 20, y, x + tileSize - 15, y + 10);

			TrFootprint tr = new TrFootprint(roPolyFc)
			                 {
				                 // NOTE: The search logic should work correctly even if search option is Tile! (e.g. due to downstream transformers)
				                 //NeighborSearchOption = TrSpatialJoin.SearchOption.All
			                 };

			TransformedFeatureClass transformedClass = tr.GetTransformed();
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
					Assert.AreEqual(4, outsideTileFeatures.Count);

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
					Assert.AreEqual(2, outsideTileFeatures.Count);

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

			ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass);

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
			var polygon = GeometryFactory.CreatePolygon(xMin, yMin, xMax, yMax, 1.0);
			var multipatch = GeometryFactory.CreateMultiPatch(polygon);
			row.Shape = multipatch;
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

			fields.Add(FieldUtils.CreateShapeField(
				           "Shape", geometryType,
				           SpatialReferenceUtils.CreateSpatialReference
				           ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				            true), 1000, true));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));
			return fc;
		}
	}
}
