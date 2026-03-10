using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;
using Path = System.IO.Path;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaGeometryConstraintTest
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
		public void CanTestPolylineFeature()
		{
			IFeatureClass fc = new FeatureClassMock("LineFc",
			                                        esriGeometryType.esriGeometryPolyline, 1);

			IFeature feature = fc.CreateFeature();

			feature.Shape = CurveConstruction.StartLine(0, 0)
			                                 .LineTo(10, 10)
			                                 .CircleTo(30, 10)
			                                 .Curve;
			feature.Store();

			var test = new QaGeometryConstraint(ReadOnlyTableFactory.Create(fc),
			                                    "$CircularArcCount = 0", perPart: false);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error =
				AssertUtils.OneError(runner, "GeometryConstraint.ConstraintNotFulfilled.ForShape");
			Assert.True(GeometryUtils.AreEqual(feature.Shape, error.Geometry));
		}

		[Test]
		public void CanTestPolylineFeaturePath()
		{
			IFeatureClass fc = new FeatureClassMock("LineFc",
			                                        esriGeometryType.esriGeometryPolyline, 1);

			IFeature feature = fc.CreateFeature();

			IPolycurve correctPath = CurveConstruction.StartLine(100, 100)
			                                          .LineTo(110, 110)
			                                          .Curve;
			// note: union converts linear circular arcs to lines -> make sure the arc is not linear
			IPolycurve incorrectPath = CurveConstruction.StartLine(0, 0)
			                                            .LineTo(10, 10)
			                                            .CircleTo(30, 10)
			                                            .Curve;
			feature.Shape = GeometryUtils.Union(correctPath, incorrectPath);
			feature.Store();

			var test = new QaGeometryConstraint(ReadOnlyTableFactory.Create(fc),
			                                    "$CircularArcCount = 0", perPart: true);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error =
				AssertUtils.OneError(
					runner, "GeometryConstraint.ConstraintNotFulfilled.ForShapePart");

			Assert.True(GeometryUtils.AreEqual(incorrectPath,
			                                   GeometryFactory.CreatePolyline(error.Geometry)));
		}

		[Test]
		public void CanCheckPointIds()
		{
			// Multipatch geometry with point IDs - some within valid range, some outside
			// Valid range assumption: 0 to 5000
			// This geometry contains IDs: 0, 141, 1093816192, -1109318073, -1845413376

			IGeometry multipatch = GeometryUtils.FromXmlString(_multipatchXml);

			Assert.IsTrue(GeometryUtils.IsPointIDAware(multipatch),
			              "Geometry should be point-ID aware");

			// Create a file geodatabase for the multipatch feature class
			string gdbName = "CanCheckPointIds";
			string dir = Path.GetTempPath();
			string gdbPath = Path.Combine(dir, gdbName) + ".gdb";

			if (Directory.Exists(gdbPath))
			{
				Directory.Delete(gdbPath, true);
			}

			IWorkspaceName wsName = WorkspaceUtils.CreateFileGdbWorkspace(dir, gdbName);
			var featureWorkspace = (IFeatureWorkspace) ((IName) wsName).Open();

			// Create the multipatch feature class with Z support (required for multipatches)
			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(
					esriGeometryType.esriGeometryMultiPatch,
					multipatch.SpatialReference,
					hasZ: true));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(
				featureWorkspace, "MultipatchFc", fields);

			// Create and store the feature
			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			TestPointId(fc, feature);
		}

		private static void TestPointId(IFeatureClass featureClass, IFeature feature)
		{
			// Test: Geometry should be point-ID aware
			var testIsPointIdAware = new QaGeometryConstraint(
				ReadOnlyTableFactory.Create(featureClass),
				"$IsPointIdAware", perPart: false);
			var runnerIsPointIdAware = new QaTestRunner(testIsPointIdAware);
			runnerIsPointIdAware.Execute(feature);

			Assert.AreEqual(0, runnerIsPointIdAware.Errors.Count,
			                "Geometry should be point-ID aware");

			// Test: Point IDs should violate the valid range constraint (0 to 5000)
			// The geometry has PointIdMin = -1845413376 and PointIdMax = 1093816192
			// This constraint should FAIL because IDs are outside 0-5000
			var testValidRange = new QaGeometryConstraint(
				ReadOnlyTableFactory.Create(featureClass),
				"$PointIdMin >= 0 AND $PointIdMax < 5000", perPart: false);
			var runnerValidRange = new QaTestRunner(testValidRange);
			runnerValidRange.Execute(feature);

			QaError error = AssertUtils.OneError(
				runnerValidRange, "GeometryConstraint.ConstraintNotFulfilled.ForShape");
			Assert.IsNotNull(error, "Should report error for point IDs outside valid range");

			// Test: Verify the actual min/max values using a constraint that should pass
			// PointIdMin = -1845413376 (minimum), PointIdMax = 1093816192 (maximum)
			var testActualRange = new QaGeometryConstraint(
				ReadOnlyTableFactory.Create(featureClass),
				"$PointIdMin < 0 AND $PointIdMax > 5000", perPart: false);
			var runnerActualRange = new QaTestRunner(testActualRange);
			runnerActualRange.Execute(feature);

			Assert.AreEqual(0, runnerActualRange.Errors.Count,
			                "Point ID range should include negative values and values > 5000");

			// Combined test: Check both IsPointIdAware and valid range in one constraint:
			var testCombined = new QaGeometryConstraint(
				ReadOnlyTableFactory.Create(featureClass),
				"(NOT $IsPointIdAware) OR ($PointIdMin >= 0 AND $PointIdMax < 5000)",
				perPart: false);
			var runnerCombined = new QaTestRunner(testCombined);
			runnerCombined.Execute(feature);
			error = AssertUtils.OneError(
				runnerCombined, "GeometryConstraint.ConstraintNotFulfilled.ForShape");
			Assert.IsNotNull(error, "Should report error for point IDs outside valid range");
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCheckPointIdsFromSde()
		{
			// Open SDE workspace and query the multipatch feature from TLM_GEBAEUDE
			IWorkspace workspace = TestUtils.OpenOsaWorkspaceOracle();
			var featureWorkspace = (IFeatureWorkspace) workspace;

			IFeatureClass fc = featureWorkspace.OpenFeatureClass("TOPGIS_TLM.TLM_GEBAEUDE");

			// Get the feature with OID 7304000
			const long featureOid = 7304000;
			IFeature feature = GdbQueryUtils.GetFeature(fc, featureOid);

			Assert.IsNotNull(feature, $"Feature with OID {featureOid} not found");

			IGeometry multipatch = feature.Shape;

			Assert.AreEqual(esriGeometryType.esriGeometryMultiPatch, multipatch.GeometryType,
			                "Feature should be a multipatch");

			// Test: Geometry should be point-ID aware
			var testIsPointIdAware = new QaGeometryConstraint(
				ReadOnlyTableFactory.Create(fc),
				"$IsPointIdAware", perPart: false);
			var runnerIsPointIdAware = new QaTestRunner(testIsPointIdAware);
			runnerIsPointIdAware.Execute(feature);

			// Note: The actual point ID awareness depends on the data in the SDE
			// Log the result for verification
			bool isPointIdAware = GeometryUtils.IsPointIDAware(multipatch);
			if (isPointIdAware)
			{
				Assert.AreEqual(0, runnerIsPointIdAware.Errors.Count,
				                "Geometry should be point-ID aware");

				// Test: Point IDs should violate the valid range constraint (0 to 5000)
				// This constraint should FAIL if IDs are outside 0-5000
				var testValidRange = new QaGeometryConstraint(
					ReadOnlyTableFactory.Create(fc),
					"$PointIdMin >= 0 AND $PointIdMax < 5000", perPart: false);
				var runnerValidRange = new QaTestRunner(testValidRange);
				runnerValidRange.Execute(feature);

				QaError error = AssertUtils.OneError(
					runnerValidRange, "GeometryConstraint.ConstraintNotFulfilled.ForShape");
				Assert.IsNotNull(error, "Should report error for point IDs outside valid range");

				// Test: Verify the actual min/max values using a constraint that should pass
				var testActualRange = new QaGeometryConstraint(
					ReadOnlyTableFactory.Create(fc),
					"$PointIdMin < 0 AND $PointIdMax > 5000", perPart: false);
				var runnerActualRange = new QaTestRunner(testActualRange);
				runnerActualRange.Execute(feature);

				Assert.AreEqual(0, runnerActualRange.Errors.Count,
				                "Point ID range should include negative values and values > 5000");

				// Combined test: Check both IsPointIdAware and valid range in one constraint
				var testCombined = new QaGeometryConstraint(
					ReadOnlyTableFactory.Create(fc),
					"(NOT $IsPointIdAware) OR ($PointIdMin >= 0 AND $PointIdMax < 5000)",
					perPart: false);
				var runnerCombined = new QaTestRunner(testCombined);
				runnerCombined.Execute(feature);
				error = AssertUtils.OneError(
					runnerCombined, "GeometryConstraint.ConstraintNotFulfilled.ForShape");
				Assert.IsNotNull(error, "Should report error for point IDs outside valid range");
			}
			else
			{
				// If not point ID aware, the constraint should fail
				Assert.AreEqual(1, runnerIsPointIdAware.Errors.Count,
				                "Geometry is not point-ID aware, constraint should fail");
			}
		}

		private const string _multipatchXml =
			@"<MultiPatchN xsi:type=""typens:MultiPatchN"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:typens=""http://www.esri.com/schemas/ArcGIS/10.8"">
  <HasID>true</HasID>
  <HasZ>true</HasZ>
  <HasM>false</HasM>
  <Extent xsi:type=""typens:EnvelopeN"">
    <XMin>2699088.7580000013</XMin>
    <YMin>1263189.5850000009</YMin>
    <XMax>2699104.8079999983</XMax>
    <YMax>1263210.9400000013</YMax>
    <ZMin>497.94199999999546</ZMin>
    <ZMax>497.94199999999546</ZMax>
  </Extent>
  <SurfacePatchArray xsi:type=""typens:ArrayOfSurfacePatch"">
    <SurfacePatch xsi:type=""typens:Ring"">
      <PointArray xsi:type=""typens:ArrayOfPoint"">
        <Point xsi:type=""typens:PointN"">
          <X>2699089.4360000007</X>
          <Y>1263198.1339999996</Y>
          <Z>497.94199999999546</Z>
          <ID>0</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699090.2540000007</X>
          <Y>1263206.6539999992</Y>
          <Z>497.94199999999546</Z>
          <ID>0</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699102.368999999</X>
          <Y>1263210.9400000013</Y>
          <Z>497.94199999999546</Z>
          <ID>0</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699104.4930000007</X>
          <Y>1263204.9349999987</Y>
          <Z>497.94199999999546</Z>
          <ID>0</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699098.1099999994</X>
          <Y>1263202.6770000011</Y>
          <Z>497.94199999999546</Z>
          <ID>0</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699097.8709999993</X>
          <Y>1263200.188000001</Y>
          <Z>497.94199999999546</Z>
          <ID>1093816192</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699097.5410000011</X>
          <Y>1263196.7529999986</Y>
          <Z>497.94199999999546</Z>
          <ID>-1109318073</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699104.8079999983</X>
          <Y>1263196.0549999997</Y>
          <Z>497.94199999999546</Z>
          <ID>-1845413376</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699104.186999999</X>
          <Y>1263189.5850000009</Y>
          <Z>497.94199999999546</Z>
          <ID>0</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699088.7580000013</X>
          <Y>1263191.0659999996</Y>
          <Z>497.94199999999546</Z>
          <ID>141</ID>
        </Point>
        <Point xsi:type=""typens:PointN"">
          <X>2699089.4360000007</X>
          <Y>1263198.1339999996</Y>
          <Z>497.94199999999546</Z>
          <ID>0</ID>
        </Point>
      </PointArray>
    </SurfacePatch>
  </SurfacePatchArray>
  <SpatialReference xsi:type=""typens:ProjectedCoordinateSystem"">
    <WKT>PROJCS[""CH1903+_LV95"",GEOGCS[""GCS_CH1903+"",DATUM[""D_CH1903+"",SPHEROID[""Bessel_1841"",6377397.155,299.1528128]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Hotine_Oblique_Mercator_Azimuth_Center""],PARAMETER[""False_Easting"",2600000.0],PARAMETER[""False_Northing"",1200000.0],PARAMETER[""Scale_Factor"",1.0],PARAMETER[""Azimuth"",90.0],PARAMETER[""Longitude_Of_Center"",7.439583333333333],PARAMETER[""Latitude_Of_Center"",46.95240555555556],UNIT[""Meter"",1.0]],VERTCS[""LHN95"",VDATUM[""Landeshohennetz_1995""],PARAMETER[""Vertical_Shift"",0.0],PARAMETER[""Direction"",1.0],UNIT[""Meter"",1.0]]</WKT>
    <XOrigin>-27386400</XOrigin>
    <YOrigin>-32067900</YOrigin>
    <XYScale>140996569.55187955</XYScale>
    <ZOrigin>-100000</ZOrigin>
    <ZScale>1000</ZScale>
    <MOrigin>-100000</MOrigin>
    <MScale>10000</MScale>
    <XYTolerance>0.002</XYTolerance>
    <ZTolerance>0.01</ZTolerance>
    <MTolerance>0.01</MTolerance>
    <HighPrecision>true</HighPrecision>
    <WKID>2056</WKID>
    <LatestWKID>2056</LatestWKID>
    <VCSWKID>5729</VCSWKID>
    <LatestVCSWKID>5729</LatestVCSWKID>
  </SpatialReference>
</MultiPatchN>";
	}
}
