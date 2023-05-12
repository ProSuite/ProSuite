using System;
using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;
using Path = System.IO.Path;

namespace ProSuite.Commons.AO.Test
{
	public static class TestUtils
	{
		private const string _line1 = "line1.xml";
		private const string _line2 = "line2.xml";
		private const string _poly1 = "poly1.xml";
		private const string _poly2 = "poly2.xml";

		private static int _lastClassId;

		public static string OracleDbNameSde => "PROSUITE_TEST_SERVER";
		public static string OracleDbNameDdx => "PROSUITE_TEST_DDX";

		public static IWorkspace OpenUserWorkspaceOracle([NotNull] string repositoryName = "SDE")
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				repositoryName, DirectConnectDriver.Oracle11g, OracleDbNameSde,
				"unittest", "unittest");

			return workspace;
		}

		public static IWorkspace OpenOsaWorkspaceOracle([NotNull] string repositoryName = "SDE")
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				repositoryName, DirectConnectDriver.Oracle11g, OracleDbNameSde);

			return workspace;
		}

		public static IWorkspace OpenSDEWorkspaceOracle()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"SDE", DirectConnectDriver.Oracle11g, OracleDbNameSde,
				"sde", "sde");

			return workspace;
		}

		public static IWorkspace OpenDDxWorkspaceOracle()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"SDE", DirectConnectDriver.Oracle11g, OracleDbNameDdx,
				"unittest", "unittest");

			return workspace;
		}

		public static IWorkspace OpenUserWorkspacePostgres()
		{
			return WorkspaceUtils.OpenSDEWorkspace(
				"data_osm", DirectConnectDriver.PostgreSQL, "localhost",
				"osm", "osm");
		}

		public static void GetMockFeatures(out IFeature mockLine1Feature,
		                                   out IFeature mockLine2Feature,
		                                   out IFeature mockPoly1Feature,
		                                   out IFeature mockPoly2Feature)
		{
			Console.WriteLine(@"Loading test data...");

			var line1 = (IPolyline) ReadGeometryFromXml(GetGeometryTestDataPath(_line1));
			var line2 = (IPolyline) ReadGeometryFromXml(GetGeometryTestDataPath(_line2));
			var poly1 = (IPolygon) ReadGeometryFromXml(GetGeometryTestDataPath(_poly1));
			var poly2 = (IPolygon) ReadGeometryFromXml(GetGeometryTestDataPath(_poly2));

			var mockPolyFeatureClass =
				new FeatureClassMock("TestPolyClass",
				                     esriGeometryType.esriGeometryPolygon,
				                     1, esriFeatureType.esriFTSimple,
				                     poly1.SpatialReference, GeometryUtils.IsZAware(poly1));

			mockPoly1Feature = mockPolyFeatureClass.CreateFeature(poly1);

			mockPoly2Feature = mockPolyFeatureClass.CreateFeature(poly2);

			var mockLineFeatureClass =
				new FeatureClassMock("TestLineClass",
				                     esriGeometryType.esriGeometryPolyline,
				                     2, esriFeatureType.esriFTSimple,
				                     line1.SpatialReference, GeometryUtils.IsZAware(line1));

			mockLine1Feature = mockLineFeatureClass.CreateFeature(line1);

			mockLine2Feature = mockLineFeatureClass.CreateFeature(line2);
		}

		public static IFeature CreateMockFeature(string xmlGeometryFileName)
		{
			var geometry = ReadGeometryFromXml(GetGeometryTestDataPath(xmlGeometryFileName));

			return CreateMockFeature(geometry);
		}

		public static IFeature CreateMockFeature([NotNull] string xmlOrWkbGeometryFileName,
		                                         [CanBeNull] ISpatialReference spatialRef)
		{
			string filePath = GetGeometryTestDataPath(xmlOrWkbGeometryFileName);

			IGeometry geometry;
			if (xmlOrWkbGeometryFileName.EndsWith(
				    "wkb", StringComparison.InvariantCultureIgnoreCase))
			{
				geometry = ReadGeometryFromWkb(filePath);
			}
			else
			{
				geometry = ReadGeometryFromXml(filePath);
			}

			if (spatialRef != null)
			{
				geometry.SpatialReference = spatialRef;
			}

			return CreateMockFeature(geometry);
		}

		public static IFeature CreateMockFeature(IGeometry geometry,
		                                         double featureClassTolerance = 0.0125,
		                                         double featureClassResolution = 0.00125)
		{
			var spatialRef =
				(ISpatialReferenceTolerance) ((IClone) geometry.SpatialReference).Clone();

			// Use a defined tolerance / resolution because the geometry might come from an
			// edit session with extremely small resolution and/or changed tolerance.
			spatialRef.XYTolerance = featureClassTolerance;
			((ISpatialReferenceResolution) spatialRef).set_XYResolution(
				true, featureClassResolution);

			var mockFeatureClass =
				new FeatureClassMock("MockFeatureClass", geometry.GeometryType,
				                     _lastClassId++,
				                     esriFeatureType.esriFTSimple,
				                     (ISpatialReference) spatialRef,
				                     GeometryUtils.IsZAware(geometry));

			IFeature mockFeature = mockFeatureClass.CreateFeature(geometry);

			return mockFeature;
		}

		public static string GetGeometryTestDataPath(string fileName)
		{
			return TestDataPreparer.FromDirectory(@"TestData\Geometry").GetPath(fileName);
		}

		public static IGeometry ReadGeometryFromXml(string filePath)
		{
			IXMLSerializer serializer = new XMLSerializerClass();

			IXMLReader reader = new XMLReaderClass();

			IXMLStream stream = new XMLStreamClass();

			stream.LoadFromFile(filePath);

			reader.ReadFrom((IStream) stream);

			return (IGeometry) serializer.ReadObject(reader, null, null);
		}

		public static IGeometry ReadGeometryFromWkb(string filePath)
		{
			byte[] bytes = File.ReadAllBytes(filePath);

			WkbGeometryReader wkbReader = new WkbGeometryReader();

			IGeometry geometry = wkbReader.ReadGeometry(new MemoryStream(bytes));

			return geometry;
		}

		public static string GetTempDirPath([CanBeNull] string tempDirName)
		{
			if (tempDirName == null)
			{
				tempDirName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

				Assert.NotNull(tempDirName);
			}

			string localTempDir = Path.Combine(Path.GetTempPath(), tempDirName);
			return localTempDir;
		}

		private static IArcGISLicense _lic;

		public static void InitializeLicense(bool includeAnalyst3d = false,
		                                     bool activateAdvancedLicense = false)
		{
			if (_lic == null)
			{
				_lic = activateAdvancedLicense
					       ? ActivateAdvancedLicense()
					       : ActivateLicense();
			}

			bool initialized = _lic.Initialize(includeAnalyst3d);

			Assert.True(initialized, "Unable to initialize ArcGIS license");
		}

		public static void ReleaseLicense()
		{
			_lic?.Release();
		}

		private static IArcGISLicense ActivateLicense()
		{
			// Allow forcing a different license depending on the machine, such as Advanced:
			string agLicenseClass = Environment.GetEnvironmentVariable("VSArcGISLicense");

			if (string.IsNullOrEmpty(agLicenseClass))
			{
				return EnvironmentUtils.Is64BitProcess
					       ? (IArcGISLicense) new ServerLicense()
					       : new BasicLicense();
			}

			Type licType = Type.GetType(agLicenseClass);

			if (licType != null)
			{
				return (IArcGISLicense) Activator.CreateInstance(licType);
			}

			throw new FileNotFoundException($"Type {agLicenseClass} could not be loaded.");
		}

		private static IArcGISLicense ActivateAdvancedLicense()
		{
			// Allow forcing a different license depending on the machine, such as Advanced:
			string agLicenseClass = Environment.GetEnvironmentVariable("VSArcGISLicense");

			if (string.IsNullOrEmpty(agLicenseClass))
			{
				return new AdvancedLicense();
			}

			Type licType = Type.GetType(agLicenseClass);

			if (licType != null)
			{
				return (IArcGISLicense) Activator.CreateInstance(licType);
			}

			throw new FileNotFoundException($"Type {agLicenseClass} could not be loaded.");
		}
	}
}
