using System;
using System.Diagnostics;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Testing;
using ProSuite.QA.Tests.Transformers;
using Path = System.IO.Path;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrFootprintTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
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

			WKSEnvelope wksEnvelope = WksGeometryUtils.CreateWksEnvelope(
				roBuildings.Extent.XMin, roBuildings.Extent.YMin,
				roBuildings.Extent.XMax, roBuildings.Extent.YMax);

			TransformedFeatureClass featureClass = transformer.GetTransformed();

			Assert.NotNull(featureClass.BackingDataset);
			var transformedBackingDataset =
				(TransformedBackingDataset) featureClass.BackingDataset;

			transformedBackingDataset.DataContainer = new UncachedDataContainer(wksEnvelope);

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

			Assert.AreEqual(112, emptyCount);

			// 113 Are different due to AO returning envelope if footprint is no polygon
			Assert.LessOrEqual(differerentResultsCount, 113);
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

			WKSEnvelope wksEnvelope = WksGeometryUtils.CreateWksEnvelope(
				roBuildings.Extent.XMin, roBuildings.Extent.YMin,
				roBuildings.Extent.XMax, roBuildings.Extent.YMax);

			TransformedFeatureClass featureClass = transformer1.GetTransformed();

			Assert.NotNull(featureClass.BackingDataset);
			var transformedBackingDataset =
				(TransformedBackingDataset) featureClass.BackingDataset;

			transformedBackingDataset.DataContainer = new UncachedDataContainer(wksEnvelope);

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
			IReadOnlyTable table2 = featureClass2;
			using (var cursor2 = table2.EnumRows(null, false).GetEnumerator())

				foreach (IReadOnlyRow footprint in featureClass1.EnumReadOnlyRows(null, false))
				{
					IGeometry geometry1 = ((IReadOnlyFeature)footprint).Shape;

					IntersectionUtils.UseCustomIntersect = false;

					cursor2.MoveNext();
					IReadOnlyFeature feature2 = (IReadOnlyFeature) cursor2.Current;
					IGeometry geometry2 = feature2?.Shape;

					IntersectionUtils.UseCustomIntersect = true;

					rowCount++;

					//GeometryUtils.SetXyTolerance(geometry1, 0.001);
					//GeometryUtils.SetXyTolerance(geometry2, 0.001);
					bool areEqualInXY = GeometryUtils.AreEqualInXY(geometry1, geometry2);
					if (!areEqualInXY)
					{
						differentCount++;
						Console.WriteLine($"Differences detected in OID {feature2?.OID}");
					}
					else
					{
						//continue;
					}

					IFeature outFeature1 = resultClass1.CreateFeature();
					outFeature1.Shape = geometry1;
					outFeature1.Store();

					IFeature outFeature2 = ressultClass2.CreateFeature();
					outFeature2.Shape = geometry2;
					outFeature2.Store();
				}

			watch.Stop();

			return differentCount;
		}
	}
}
