using System;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class SpatialReferenceUtilsTest
	{
		private ISpatialReference _wgs84;
		private ISpatialReference _lv03lhn95_xytol_01_ztol_01;
		private ISpatialReference _lv95_xytol_01_ztol_01;
		private ISpatialReference _lv95lhn95_xytol_01_ztol_02;
		private ISpatialReference _lv95lhn95_xytol_01_ztol_01;
		private ISpatialReference _lv95ln02_xytol_01_ztol_01;
		private ISpatialReference _lv95_xytol_02_ztol_01;
		private ISpatialReference _lv95_xyres_001_zres_001;
		private ISpatialReference _lv95_xyres_001_zres_001_largerdomains;
		private ISpatialReference _lv95_xyres_001_zres_001_offsetdomains;
		private ISpatialReference _lv95_xyres_001_zres_002;
		private ISpatialReference _lv95_xyres_002_zres_001;

		#region Setup/Teardown

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			CreateSpatialReferences();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		#endregion

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanGetDifferences()
		{
			// vcs difference
			AssertDifferences(_lv95ln02_xytol_01_ztol_01, _lv95lhn95_xytol_01_ztol_01,
			                  false, true, false, // equal, projEqual, vcsEqual
			                  true, true, true, // xy, z, m precision
			                  true, true, true); // xy, z, m tolerances

			// proj difference
			AssertDifferences(_lv95lhn95_xytol_01_ztol_01, _lv03lhn95_xytol_01_ztol_01,
			                  false, false, true,
			                  false, true, true,
			                  true, true, true);

			// vcs difference, xy res difference, z res difference
			AssertDifferences(_lv95_xyres_001_zres_001, _lv95lhn95_xytol_01_ztol_01,
			                  false, true, false,
			                  false, false, true,
			                  true, true, true);

			AssertDifferences(_lv95_xyres_001_zres_001, _lv95_xyres_002_zres_001,
			                  false, true, true,
			                  false, true, true,
			                  true, true, true);

			AssertDifferences(_lv95_xytol_02_ztol_01, _lv95lhn95_xytol_01_ztol_02,
			                  false, true, false,
			                  true, true, true,
			                  false, false, true);

			// type difference (GeoCS vs. ProjCS)
			AssertDifferences(_wgs84, _lv95ln02_xytol_01_ztol_01,
			                  false, false, false,
			                  false, false, false,
			                  false, true, true);

			// TODO detect M differences
		}

		[Test]
		public void CanCompareComponents()
		{
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(
				              _lv95lhn95_xytol_01_ztol_02,
				              _lv95ln02_xytol_01_ztol_01,
				              true, true, true, // xy, z, m precisions
				              false, false)); // tolerances, vcs

			Assert.IsFalse(SpatialReferenceUtils.AreEqual(_lv95lhn95_xytol_01_ztol_02,
			                                              _lv95ln02_xytol_01_ztol_01,
			                                              true, true, true,
			                                              false, true));

			Assert.IsFalse(SpatialReferenceUtils.AreEqual(_lv95lhn95_xytol_01_ztol_02,
			                                              _lv95ln02_xytol_01_ztol_01,
			                                              true, true, true,
			                                              true, false));
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanCompareDomainOnlyDifferences()
		{
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(
				              _lv95_xyres_001_zres_001,
				              _lv95_xyres_001_zres_001_largerdomains,
				              true, true, true, // xy, z, m precisions
				              false, false)); // tolerances, vcs

			Assert.IsTrue(SpatialReferenceUtils.AreEqual(
				              _lv95_xyres_001_zres_001,
				              _lv95_xyres_001_zres_001_largerdomains,
				              true, true, true, // xy, z, m precisions
				              true, true)); // tolerances, vcs

			Assert.IsTrue(SpatialReferenceUtils.AreEqual(
				              _lv95_xyres_001_zres_001_largerdomains,
				              _lv95_xyres_001_zres_001,
				              true, true, true, // xy, z, m precisions
				              false, false)); // tolerances, vcs

			Assert.IsTrue(SpatialReferenceUtils.AreEqual(
				              _lv95_xyres_001_zres_001_largerdomains,
				              _lv95_xyres_001_zres_001,
				              true, true, true, // xy, z, m precisions
				              true, true)); // tolerances, vcs
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanCompareDomainOffsetDifferences()
		{
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(
				              _lv95_xyres_001_zres_001,
				              _lv95_xyres_001_zres_001_offsetdomains,
				              false, false, false, // xy, z, m precisions
				              true, true)); // tolerances, vcs

			Assert.IsFalse(SpatialReferenceUtils.AreEqual(
				               _lv95_xyres_001_zres_001,
				               _lv95_xyres_001_zres_001_offsetdomains,
				               true, false, false, // xy, z, m precisions
				               true, true)); // tolerances, vcs

			// NOTE: the z domain has an offset smaller than the z resolution, but IsZPrecisionEqual still returns true!!
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(
				              _lv95_xyres_001_zres_001,
				              _lv95_xyres_001_zres_001_offsetdomains,
				              false, true, false, // xy, z, m precisions
				              true, true)); // tolerances, vcs
		}

		[Test]
		public void CanToString()
		{
			var lv95 = SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			string xmlString = SpatialReferenceUtils.ToXmlString(lv95);

			Console.WriteLine(xmlString);

			ISpatialReference deserialized = SpatialReferenceUtils.FromXmlString(xmlString);

			Assert.IsTrue(SpatialReferenceUtils.AreEqual(lv95, deserialized));
		}

		[Test]
		public void CanCompareAllComponentsFastEnough()
		{
			Stopwatch watch = Stopwatch.StartNew();
			const int count = 10000;

			for (int i = 0; i < count; i++)
			{
				SpatialReferenceUtils.AreEqual(_lv95lhn95_xytol_01_ztol_02,
				                               _lv95lhn95_xytol_01_ztol_02,
				                               true, true, true,
				                               true, true);
			}

			watch.Stop();
			Console.WriteLine(@"count={0}: {1:N0} ms; per call: {2:N4} ms",
			                  count, watch.ElapsedMilliseconds,
			                  watch.ElapsedMilliseconds / (double) count);
			Assert.Less((double) watch.ElapsedMilliseconds, 1000);
		}

		[Test]
		public void CanCompareTypicalComponentsFastEnough()
		{
			// typical: only xy precision
			Stopwatch watch = Stopwatch.StartNew();
			const int count = 10000;

			for (int i = 0; i < count; i++)
			{
				SpatialReferenceUtils.AreEqual(_lv95lhn95_xytol_01_ztol_02,
				                               _lv95lhn95_xytol_01_ztol_01,
				                               true, false, false,
				                               false, false);
			}

			watch.Stop();
			Console.WriteLine(@"count={0}: {1:N0} ms; per call: {2:N4} ms",
			                  count, watch.ElapsedMilliseconds,
			                  watch.ElapsedMilliseconds / (double) count);
			Assert.Less((double) watch.ElapsedMilliseconds, 1000);
		}

		[Test]
		public void CanCompareMinimumComponentsFastEnough()
		{
			Stopwatch watch = Stopwatch.StartNew();
			const int count = 10000;

			for (int i = 0; i < count; i++)
			{
				SpatialReferenceUtils.AreEqual(_lv95lhn95_xytol_01_ztol_02,
				                               _lv95lhn95_xytol_01_ztol_01,
				                               false, false, false,
				                               false, false);
			}

			watch.Stop();
			Console.WriteLine(@"count={0}: {1:N0} ms; per call: {2:N4} ms",
			                  count, watch.ElapsedMilliseconds,
			                  watch.ElapsedMilliseconds / (double) count);
			Assert.Less((double) watch.ElapsedMilliseconds, 1000);
		}

		[Test]
		public void CanCheckAreEqualCheckCSTypeDifference()
		{
			const bool comparePrecision = true;
			const bool compareVCS = true;

			Assert.IsFalse(
				SpatialReferenceUtils.AreEqual(_wgs84,
				                               _lv95_xytol_01_ztol_01,
				                               comparePrecision, compareVCS));
		}

		[Test]
		public void CanCheckAreEqualCheckVcsDifference()
		{
			const bool comparePrecision = true;
			const bool compareVCS = true;

			Assert.IsFalse(
				SpatialReferenceUtils.AreEqual(_lv95ln02_xytol_01_ztol_01,
				                               _lv95_xytol_01_ztol_01,
				                               comparePrecision, compareVCS));

			Assert.IsFalse(
				SpatialReferenceUtils.AreEqual(_lv95ln02_xytol_01_ztol_01,
				                               _lv95lhn95_xytol_01_ztol_01,
				                               comparePrecision, compareVCS));
		}

		[Test]
		public void CanCheckAreEqualCheckXyResolutionDifference()
		{
			const bool comparePrecision = true;
			const bool compareVCS = true;

			Assert.IsFalse(
				SpatialReferenceUtils.AreEqual(_lv95_xyres_001_zres_001,
				                               _lv95_xyres_002_zres_001,
				                               comparePrecision, compareVCS));
		}

		[Test]
		public void CanCheckAreEqualCheckXyResolutionIgnoreZResolutionDifference()
		{
			const bool comparePrecision = true;
			const bool compareVCS = false;

			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(_lv95_xyres_001_zres_001,
				                               _lv95_xyres_001_zres_002,
				                               comparePrecision, compareVCS));
		}

		[Test]
		public void CanCheckAreEqualCheckXyToleranceDifference()
		{
			const bool comparePrecision = true;
			const bool compareVCS = true;

			Assert.IsFalse(
				SpatialReferenceUtils.AreEqual(_lv95_xytol_01_ztol_01,
				                               _lv95_xytol_02_ztol_01,
				                               comparePrecision, compareVCS));
		}

		[Test]
		public void CanCheckAreEqualCheckXyToleranceIgnoreZToleranceDifference()
		{
			const bool comparePrecision = true;
			const bool compareVCS = false;

			var watch = new Stopwatch();
			watch.Start();

			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(_lv95_xytol_01_ztol_01,
				                               _lv95lhn95_xytol_01_ztol_02,
				                               comparePrecision, compareVCS));

			Console.WriteLine(@"AreEqual (precision compare): {0:N2} ms",
			                  watch.ElapsedMilliseconds);
		}

		[Test]
		public void CanCheckAreEqualIgnoreVcsDifference()
		{
			const bool comparePrecision = true;
			const bool compareVCS = false;

			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(_lv95ln02_xytol_01_ztol_01,
				                               _lv95_xytol_01_ztol_01,
				                               comparePrecision, compareVCS));

			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(_lv95ln02_xytol_01_ztol_01,
				                               _lv95lhn95_xytol_01_ztol_01,
				                               comparePrecision, compareVCS));
		}

		[Test]
		public void CanCheckAreEqualIgnoreXyResolutionDifference()
		{
			const bool comparePrecision = false;
			const bool compareVCS = true;

			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(_lv95_xyres_001_zres_001,
				                               _lv95_xyres_002_zres_001,
				                               comparePrecision, compareVCS));
		}

		[Test]
		public void CanCheckAreEqualIgnoreXyToleranceDifference()
		{
			const bool comparePrecision = false;
			const bool compareVCS = true;

			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(_lv95_xytol_01_ztol_01,
				                               _lv95_xytol_02_ztol_01,
				                               comparePrecision, compareVCS));
		}

		[Test]
		public void CanCheckAreEqualWithPrecisionAndVCSCompareFastEnough()
		{
			const bool comparePrecision = true;
			const bool compareVCS = true;

			var watch = new Stopwatch();
			watch.Start();

			const int iterations = 1000;

			for (int i = 0; i < iterations; i++)
			{
				SpatialReferenceUtils.AreEqual(_lv95_xytol_01_ztol_01,
				                               _lv95ln02_xytol_01_ztol_01,
				                               comparePrecision, compareVCS);
			}

			double perIteration = (double) watch.ElapsedMilliseconds / iterations;
			Console.WriteLine(@"AreEqual (precision compare): {0:N6} ms", perIteration);

			Assert.Less(perIteration, 0.05, "AreEqual takes too long");
		}

		[Test]
		public void CanCheckAreEqualWithPrecisionCompareFastEnough()
		{
			const bool comparePrecision = true;
			const bool compareVCS = false;

			var watch = new Stopwatch();
			watch.Start();

			const int iterations = 1000;

			for (int i = 0; i < iterations; i++)
			{
				SpatialReferenceUtils.AreEqual(_lv95_xytol_01_ztol_01,
				                               _lv95lhn95_xytol_01_ztol_02,
				                               comparePrecision, compareVCS);
			}

			double perIteration = (double) watch.ElapsedMilliseconds / iterations;
			Console.WriteLine(@"AreEqual (precision compare): {0:N6} ms", perIteration);

			Assert.Less(perIteration, 0.05, "AreEqual takes too long");
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanExportToXml()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IFeatureClass fclass =
				DatasetUtils.OpenFeatureClass(workspace, "TOPGIS_TLM.TLM_STRASSE");

			ISpatialReference spatialReference = ((IGeoDataset) fclass).SpatialReference;

			string xml = SpatialReferenceUtils.ToXmlString(spatialReference);

			Console.WriteLine(xml);

			Assert.IsNotNull(xml);
			Assert.IsNotEmpty(xml);

			ISpatialReference imported = SpatialReferenceUtils.FromXmlString(xml);

			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(imported, spatialReference));
		}

		[Test]
		public void LearningICompareCoordinateSystems()
		{
			// IsEqualNoVCS ignores precision differences
			Assert.IsTrue(
				((ICompareCoordinateSystems) _lv95_xyres_001_zres_001).IsEqualNoVCS(
					_lv95_xyres_002_zres_001));

			// IsEqualNoVCS ignores tolerance differences
			Assert.IsTrue(
				((ICompareCoordinateSystems) _lv95_xytol_01_ztol_01).IsEqualNoVCS(
					_lv95_xytol_02_ztol_01));

			// IsEqualLeftLongitude works on projected cs also
			Assert.IsTrue(
				((ICompareCoordinateSystems) _lv95_xytol_01_ztol_01).IsEqualLeftLongitude(
					_lv95lhn95_xytol_01_ztol_01, false));

			Assert.IsTrue(
				((ICompareCoordinateSystems) _lv95ln02_xytol_01_ztol_01)
				.IsEqualLeftLongitude(
					_lv95lhn95_xytol_01_ztol_01, false));

			Assert.IsFalse(
				((ICompareCoordinateSystems) _lv95_xytol_01_ztol_01).IsEqualLeftLongitude(
					_lv95lhn95_xytol_01_ztol_01, true));

			Assert.IsFalse(
				((ICompareCoordinateSystems) _lv95ln02_xytol_01_ztol_01)
				.IsEqualLeftLongitude(
					_lv95lhn95_xytol_01_ztol_01, true));
		}

		private static void AssertDifferences([NotNull] ISpatialReference sref1,
		                                      [NotNull] ISpatialReference sref2,
		                                      bool equal, bool projectionEqual,
		                                      bool vcsEqual,
		                                      bool xyPrecisionEqual, bool zPrecisionEqual,
		                                      bool mPrecisionEqual,
		                                      bool xyToleranceEqual, bool zToleranceEqual,
		                                      bool mToleranceEqual)
		{
			bool projectionDifferent;
			bool vcsDifferent;
			bool xyPrecisionDifferent;
			bool zPrecisionDifferent;
			bool mPrecisionDifferent;
			bool xyToleranceDifferent;
			bool zToleranceDifferent;
			bool mToleranceDifferent;
			bool isEqual = SpatialReferenceUtils.AreEqual(sref1, sref2,
			                                              out projectionDifferent,
			                                              out vcsDifferent,
			                                              out xyPrecisionDifferent,
			                                              out zPrecisionDifferent,
			                                              out mPrecisionDifferent,
			                                              out xyToleranceDifferent,
			                                              out zToleranceDifferent,
			                                              out mToleranceDifferent);

			Assert.AreEqual(equal, isEqual);
			Assert.AreEqual(projectionEqual, ! projectionDifferent);
			Assert.AreEqual(vcsEqual, ! vcsDifferent);
			Assert.AreEqual(xyPrecisionEqual, ! xyPrecisionDifferent);
			Assert.AreEqual(zPrecisionEqual, ! zPrecisionDifferent);
			Assert.AreEqual(mPrecisionEqual, ! mPrecisionDifferent);
			Assert.AreEqual(xyToleranceEqual, ! xyToleranceDifferent);
			Assert.AreEqual(zToleranceEqual, ! zToleranceDifferent);
			Assert.AreEqual(mToleranceEqual, ! mToleranceDifferent);
		}

		private void CreateSpatialReferences()
		{
			_wgs84 = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRGeoCSType.esriSRGeoCS_WGS1984);
			SpatialReferenceUtils.SetXYDomain(_wgs84,
			                                  -180, -90, 180, 90,
			                                  0.00001, 0.0001);
			SpatialReferenceUtils.SetZDomain(_wgs84,
			                                 -100, 5000,
			                                 0.0001, 0.1);

			_lv03lhn95_xytol_01_ztol_01 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903_LV03,
					(int) esriSRVerticalCSType.esriSRVertCS_Landeshohennetz1995);
			SpatialReferenceUtils.SetXYDomain(_lv03lhn95_xytol_01_ztol_01,
			                                  -100, -100, 1000, 1000,
			                                  0.0001, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv03lhn95_xytol_01_ztol_01,
			                                 -100, 5000,
			                                 0.0001, 0.1);

			_lv95_xytol_01_ztol_01 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);
			SpatialReferenceUtils.SetXYDomain(_lv95_xytol_01_ztol_01,
			                                  -100, -100, 1000, 1000,
			                                  0.0001, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv95_xytol_01_ztol_01,
			                                 -100, 5000,
			                                 0.0001, 0.1);

			_lv95lhn95_xytol_01_ztol_02 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					(int) esriSRVerticalCSType.esriSRVertCS_Landeshohennetz1995);
			SpatialReferenceUtils.SetXYDomain(_lv95lhn95_xytol_01_ztol_02,
			                                  -100, -100, 1000, 1000,
			                                  0.0001, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv95lhn95_xytol_01_ztol_02,
			                                 -100, 5000,
			                                 0.0001, 0.2);

			_lv95lhn95_xytol_01_ztol_01 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					(int) esriSRVerticalCSType.esriSRVertCS_Landeshohennetz1995);

			SpatialReferenceUtils.SetXYDomain(_lv95lhn95_xytol_01_ztol_01,
			                                  -100, -100, 1000, 1000,
			                                  0.0001, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv95lhn95_xytol_01_ztol_01,
			                                 -100, 5000,
			                                 0.0001, 0.1);

			_lv95ln02_xytol_01_ztol_01 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					(int) esriSRVerticalCSType.esriSRVertCS_Landesnivellement1902);

			SpatialReferenceUtils.SetXYDomain(_lv95ln02_xytol_01_ztol_01,
			                                  -100, -100, 1000, 1000,
			                                  0.0001, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv95ln02_xytol_01_ztol_01,
			                                 -100, 5000,
			                                 0.0001, 0.1);

			_lv95_xytol_02_ztol_01 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);
			SpatialReferenceUtils.SetXYDomain(_lv95_xytol_02_ztol_01,
			                                  -100, -100, 1000, 1000,
			                                  0.0001, 0.2);
			SpatialReferenceUtils.SetZDomain(_lv95_xytol_02_ztol_01,
			                                 -100, 5000,
			                                 0.0001, 0.1);

			_lv95_xyres_001_zres_001 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);
			SpatialReferenceUtils.SetXYDomain(_lv95_xyres_001_zres_001,
			                                  -100, -100, 1000, 1000,
			                                  0.001, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv95_xyres_001_zres_001,
			                                 -100, 5000,
			                                 0.001, 0.1);

			_lv95_xyres_001_zres_001_largerdomains =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);
			SpatialReferenceUtils.SetXYDomain(_lv95_xyres_001_zres_001_largerdomains,
			                                  -10000, -10000, 100000, 100000,
			                                  0.001, 0.1);
			// note: a different ZMin value makes the ZPrecision different
			// (the same thing is not true for xy precision: domain differences alone 
			// don't make the precision different, as long as the grids are compatible)
			SpatialReferenceUtils.SetZDomain(_lv95_xyres_001_zres_001_largerdomains,
			                                 -100, 10000,
			                                 0.001, 0.1);

			_lv95_xyres_001_zres_001_offsetdomains =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);
			SpatialReferenceUtils.SetXYDomain(_lv95_xyres_001_zres_001_offsetdomains,
			                                  -100.0001, -100, 1000, 1000,
			                                  0.001, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv95_xyres_001_zres_001_offsetdomains,
			                                 -100.0001, 5000,
			                                 0.001, 0.1);

			_lv95_xyres_001_zres_002 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					(int) esriSRVerticalCSType.esriSRVertCS_Landesnivellement1902);
			SpatialReferenceUtils.SetXYDomain(_lv95_xyres_001_zres_002,
			                                  -100, -100, 1000, 1000,
			                                  0.001, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv95_xyres_001_zres_002,
			                                 -100, 5000,
			                                 0.001, 0.2);

			_lv95_xyres_002_zres_001 =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);
			SpatialReferenceUtils.SetXYDomain(_lv95_xyres_002_zres_001,
			                                  -100, -100, 1000, 1000,
			                                  0.002, 0.1);
			SpatialReferenceUtils.SetZDomain(_lv95_xyres_002_zres_001,
			                                 -100, 5000,
			                                 0.001, 0.1);
		}
	}
}
