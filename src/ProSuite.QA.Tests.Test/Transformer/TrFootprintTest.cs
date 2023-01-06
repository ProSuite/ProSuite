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

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrFootprintTest
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
		public void CanGetFootprintsRealData()
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
	}
}
