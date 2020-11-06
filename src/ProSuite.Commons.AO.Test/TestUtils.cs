using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using Path = System.IO.Path;

namespace ProSuite.Commons.AO.Test
{
	public static class TestUtils
	{
		private const string _line1 = "line1.xml";
		private const string _line2 = "line2.xml";
		private const string _poly1 = "poly1.xml";
		private const string _poly2 = "poly2.xml";

		private const string _loggingConfigurationFile = "UnitTest_log4net.config";

		private static int _lastClassId;

		public static string OracleDbNameSde => "PROSUITE_TEST_SERVER";
		public static string OracleDbNameDdx => "PROSUITE_TEST_DDX";

		public static void ConfigureUnittestLogging()
		{
			List<string> logDirs = new List<string>();

			string currentDir = Environment.CurrentDirectory;

			if (Directory.Exists(currentDir))
			{
				logDirs.Add(currentDir);
			}

			string assemblyDir =
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Assert.IsNotNull(assemblyDir);
			if (Directory.Exists(assemblyDir))
			{
				logDirs.Add(assemblyDir);

				DirectoryInfo parent = Directory.GetParent(assemblyDir);
				if (parent.Exists)
				{
					logDirs.Add(parent.FullName);
				}
			}

			if (! LoggingConfigurator.Configure(_loggingConfigurationFile, logDirs))
			{
				Console.WriteLine("Logging configurator failed.");
				Console.WriteLine("logging configuration file: " + _loggingConfigurationFile);
				Console.WriteLine("Search directories: " + StringUtils.Concatenate(logDirs, ", "));
			}
			else
			{
				Console.WriteLine("Logging configured.");
			}
		}

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
				new FeatureClassMock(1, "TestPolyClass",
				                     esriGeometryType.esriGeometryPolygon,
				                     esriFeatureType.esriFTSimple, poly1.SpatialReference,
				                     GeometryUtils.IsZAware(poly1));

			mockPoly1Feature = mockPolyFeatureClass.CreateFeature(poly1);

			mockPoly2Feature = mockPolyFeatureClass.CreateFeature(poly2);

			var mockLineFeatureClass =
				new FeatureClassMock(2, "TestLineClass",
				                     esriGeometryType.esriGeometryPolyline,
				                     esriFeatureType.esriFTSimple, line1.SpatialReference,
				                     GeometryUtils.IsZAware(line1));

			mockLine1Feature = mockLineFeatureClass.CreateFeature(line1);

			mockLine2Feature = mockLineFeatureClass.CreateFeature(line2);
		}

		public static IFeature CreateMockFeature(string xmlGeometryFileName)
		{
			var geometry = ReadGeometryFromXml(GetGeometryTestDataPath(xmlGeometryFileName));

			return CreateMockFeature(geometry);
		}

		public static IFeature CreateMockFeature(IGeometry geometry)
		{
			var spatialRef =
				(ISpatialReferenceTolerance) ((IClone) geometry.SpatialReference).Clone();

			spatialRef.XYTolerance = 0.0125;
			((ISpatialReferenceResolution) spatialRef).set_XYResolution(true, 0.00125);

			var mockFeatureClass =
				new FeatureClassMock(_lastClassId++, "MockFeatureClass", geometry.GeometryType,
				                     esriFeatureType.esriFTSimple,
				                     (ISpatialReference) spatialRef,
				                     GeometryUtils.IsZAware(geometry));

			IFeature mockFeature = mockFeatureClass.CreateFeature(geometry);

			return mockFeature;
		}

		public static string GetGeometryTestDataPath(string fileName)
		{
			var locator = TestDataLocator.Create("ProSuite", @"TestData\Geometry");

			return locator.GetPath(fileName);
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
	}
}
