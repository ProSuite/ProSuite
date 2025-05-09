using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using System.Security.AccessControl;
using System.Security.Principal;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.IO;
using ProSuite.QA.Tests.External;
using ProSuite.QA.Tests.Test.TestRunners;
using Path = System.IO.Path;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaExternalGraphicConflictTest
	{
		const string Localhost = "localhost";
		private const int Port = 50051; //5181;

		const string unitTestData =
			@"C:\git\Swisstopo.Topgis\ProSuite\src\ProSuite.QA.Tests.Test\TestData\ExternalGraphicConflict";

		const string zippedTestdata =
			@"C:\git\Swisstopo.Topgis\ProSuite\src\ProSuite.QA.Tests.Test\TestData\ExternalGraphicConflict.zip";

		private const string fcNameStrasse = "TLM_Strasse_Sample";
		private const string fcNameEisenbahn = "TLM_Eisenbahn_Sample";
		readonly string lyrStrasse = Path.Combine(unitTestData, "TLM_Strasse_Sample.lyrx");
		readonly string lyrEisenbahn = Path.Combine(unitTestData, "TLM_Eisenbahn_Sample.lyrx");
		readonly string testFgdb = Path.Combine(unitTestData, "GraphicConflict.gdb");

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			// Start the server:
			StartServer(Localhost, Port);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
#if NET6_0_OR_GREATER
		[SupportedOSPlatform("windows")]
#endif
		public void TestWithFgdb()
		{
			try
			{
				FileSystemUtils.DeleteDirectory(unitTestData, true);
			}
			catch (Exception)
			{
				// caught deliberately
			}

			ZipUtils.ExtractToDirectory(zippedTestdata, unitTestData);

			// Get directory access info
			DirectoryInfo dinfo = new DirectoryInfo(unitTestData);
			DirectorySecurity dSecurity = dinfo.GetAccessControl();

			// Add the FileSystemAccessRule to the security settings. 
			dSecurity.AddAccessRule(new FileSystemAccessRule(
				                        new SecurityIdentifier(WellKnownSidType.WorldSid, null),
				                        FileSystemRights.FullControl,
				                        InheritanceFlags.ObjectInherit |
				                        InheritanceFlags.ContainerInherit,
				                        PropagationFlags.InheritOnly, AccessControlType.Allow));

			// Set the access control
			dinfo.SetAccessControl(dSecurity);

			IFeatureWorkspace featureWorkspace =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(testFgdb);

			IFeatureClass fcStrasse =
				DatasetUtils.OpenFeatureClass(featureWorkspace, fcNameStrasse);
			IFeatureClass fcBahn = DatasetUtils.OpenFeatureClass(featureWorkspace, fcNameEisenbahn);

			ISpatialReference sr = DatasetUtils.GetSpatialReference(fcStrasse);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(
				2664000, 1211000, 2665500, 1213500, sr);

			string connectionUrl = $"http://{Localhost}:{Port}";

			List<ITable> tables = new[] { fcBahn, fcStrasse }.Cast<ITable>().ToList();

			var test = new QaExternalGraphicConflict(
				ReadOnlyTableFactory.Create(fcBahn), lyrEisenbahn,
				ReadOnlyTableFactory.Create(fcStrasse), lyrStrasse,
				"100 meters", "20 meters", 50000, connectionUrl);

			using (var testRunner = new QaTestRunner(test))
			{
				testRunner.Execute(envelope);
				//Assert.AreEqual(3, testRunner.Errors.Count);

				//QaError qaError = testRunner.Errors.First();

				//Assert.Greater(qaError.InvolvedRows.Count, 0);

				//Assert.AreEqual("Strasse", qaError.InvolvedRows[0].TableName);
			}
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void TestSdeWorkspace()
		{
			var workspace = TestUtils.OpenUserWorkspaceOracle();

			IFeatureClass fcStr =
				DatasetUtils.OpenFeatureClass(workspace, "TOPGIS_TLM.TLM_STRASSE");
			IFeatureClass fcBahn =
				DatasetUtils.OpenFeatureClass(workspace, "TOPGIS_TLM.TLM_EISENBAHN");

			ISpatialReference sr = DatasetUtils.GetSpatialReference(fcStr);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(
				2664000, 1211000, 2665500, 1213500, sr);

			string connectionUrl = $"http://{Localhost}:{Port}";

			List<IReadOnlyTable> tables = new[]
			                              {
				                              ReadOnlyTableFactory.Create(fcBahn),
				                              ReadOnlyTableFactory.Create(fcStr)
			                              }.Cast<IReadOnlyTable>().ToList();

			var test = new QaExternalService(
				tables, connectionUrl, string.Empty);

			using (var testRunner = new QaTestRunner(test))
			{
				testRunner.Execute(envelope);

				Assert.Greater(testRunner.Errors.Count, 0);
			}
		}

		private static void StartServer(string hostname, int port)
		{
			// start python grpc server here
		}
	}
}
