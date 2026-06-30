using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.GeoDb;
#if !ArcGIS
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
#endif

namespace ProSuite.Commons.AO.Test.Geodatabase.GdbSchema
{
	[TestFixture]
	public class GdbServiceWorkspaceTest
	{
		private const string _redlandsUrl =
			"https://sampleserver6.arcgisonline.com/arcgis/rest/services/RedlandsEmergencyVehicles/FeatureServer";

		// NOTE: deliberately with a trailing slash and incorrect capitalization (ArcGIS), to exercise URL normalization.
		private const string _wildfireUrl =
			"https://sampleserver6.arcgisonline.com/ArcGIS/rest/services/Wildfire/FeatureServer/";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		#region Offline comparison tests (no network access)

		[Test]
		public void CanCreateServiceWorkspace()
		{
			GdbWorkspace workspace = GdbWorkspace.CreateForFeatureService(_redlandsUrl);

			Assert.AreEqual(WorkspaceDbType.FeatureService, workspace.DbType);
			Assert.AreEqual(esriWorkspaceType.esriRemoteDatabaseWorkspace, workspace.Type);
			Assert.AreEqual(_redlandsUrl, workspace.PathName);
			Assert.IsTrue(workspace.Exists());
		}

		[Test]
		public void TwoServiceWorkspacesFromSameUrlAreEqual()
		{
			GdbWorkspace ws1 = GdbWorkspace.CreateForFeatureService(_redlandsUrl);
			GdbWorkspace ws2 = GdbWorkspace.CreateForFeatureService(_redlandsUrl);

			Assert.IsTrue(ws1.Equals(ws2));
			Assert.IsTrue(ws2.Equals(ws1));
			Assert.AreEqual(ws1.GetHashCode(), ws2.GetHashCode());

			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(ws1, ws2));
			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(ws2, ws1));

			Assert.IsTrue(WorkspaceUtils.IsSameWorkspace(
				              ws1, ws2, WorkspaceComparison.AnyUserSameVersion));
			Assert.IsTrue(WorkspaceUtils.IsSameWorkspace(
				              ws2, ws1, WorkspaceComparison.AnyUserSameVersion));
		}

		[Test]
		public void ServiceUrlIsNormalizedForComparison()
		{
			// Same service, once with and once without a trailing slash:
			GdbWorkspace ws1 = GdbWorkspace.CreateForFeatureService(_wildfireUrl);
			GdbWorkspace ws2 = GdbWorkspace.CreateForFeatureService(_wildfireUrl.TrimEnd('/'));

			Assert.AreEqual(ws1.PathName, ws2.PathName);
			Assert.IsTrue(ws1.Equals(ws2));
			Assert.AreEqual(ws1.GetHashCode(), ws2.GetHashCode());
		}

		[Test]
		public void DifferentServiceUrlsAreNotEqual()
		{
			GdbWorkspace redlands = GdbWorkspace.CreateForFeatureService(_redlandsUrl);
			GdbWorkspace wildfire = GdbWorkspace.CreateForFeatureService(_wildfireUrl);

			Assert.IsFalse(redlands.Equals(wildfire));
			Assert.IsFalse(WorkspaceUtils.IsSameDatabase(redlands, wildfire));
		}

		[Test]
		public void ServiceWorkspacesWithDifferentHandlesAreNotEqual()
		{
			GdbWorkspace ws1 =
				GdbWorkspace.CreateForFeatureService(_redlandsUrl, workspaceHandle: 1);
			GdbWorkspace ws2 =
				GdbWorkspace.CreateForFeatureService(_redlandsUrl, workspaceHandle: 2);

			Assert.IsFalse(ws1.Equals(ws2));
			Assert.IsFalse(WorkspaceUtils.IsSameDatabase(ws1, ws2));
		}

		[Test]
		public void ServiceWorkspaceIsNotSameAsLocalGdbWorkspace()
		{
			GdbWorkspace serviceWorkspace =
				GdbWorkspace.CreateForFeatureService(_redlandsUrl);

			// A (local) file geodatabase workspace, constructed without opening a real gdb:
			var localWorkspace = new GdbWorkspace(
				new GdbTableContainer(), 42, WorkspaceDbType.FileGeodatabase,
				@"C:\temp\Some.gdb");

			Assert.AreNotEqual(serviceWorkspace.Type, localWorkspace.Type);
			Assert.IsFalse(serviceWorkspace.Equals(localWorkspace));
			Assert.IsFalse(WorkspaceUtils.IsSameDatabase(serviceWorkspace, localWorkspace));
			Assert.IsFalse(WorkspaceUtils.IsSameDatabase(localWorkspace, serviceWorkspace));
		}

		#endregion

		// The FeatureServiceSchemaReader (and its System.Text.Json dependency) is excluded
		// from the .NET Framework (ArcObjects) build, hence so are these tests.
#if !ArcGIS

		#region Offline FeatureServiceSchemaReader tests (canned JSON, no network access)

		// These exercise the same JSON-parsing / naming logic as the Online tests below,
		// but against canned responses, since FeatureServiceSchemaReader's HttpClient is
		// injectable. This keeps the (de facto unit-)logic fast and runnable offline/in CI.

		private const string _fakeWildfireUrl =
			"https://fake.server/arcgis/rest/services/Wildfire/FeatureServer";

		private const string _fakeRedlandsUrl =
			"https://fake.server/arcgis/rest/services/Redlands/FeatureServer";

		private const string _fakeWildfireServiceJson = @"{
			""layers"": [ { ""id"": 0, ""name"": ""Wildfire Response Points"" } ],
			""tables"": []
		}";

		private const string _fakeWildfireLayer0Json = @"{
			""id"": 0,
			""name"": ""Wildfire Response Points"",
			""type"": ""Feature Layer"",
			""geometryType"": ""esriGeometryPoint"",
			""fields"": [
				{ ""name"": ""OBJECTID"", ""type"": ""esriFieldTypeOID"", ""alias"": ""OBJECTID"" },
				{ ""name"": ""Status"", ""type"": ""esriFieldTypeString"", ""alias"": ""Status"", ""length"": 50 }
			],
			""extent"": { ""spatialReference"": { ""wkid"": 4326 } }
		}";

		private const string _fakeRedlandsServiceJson = @"{
			""layers"": [ { ""id"": 0, ""name"": ""Emergency Vehicles"" } ],
			""tables"": []
		}";

		private const string _fakeRedlandsLayer0Json = @"{
			""id"": 0,
			""name"": ""Emergency Vehicles"",
			""type"": ""Feature Layer"",
			""geometryType"": ""esriGeometryPoint"",
			""fields"": [
				{ ""name"": ""OBJECTID"", ""type"": ""esriFieldTypeOID"", ""alias"": ""OBJECTID"" }
			],
			""extent"": { ""spatialReference"": { ""wkid"": 4326 } }
		}";

		private class FakeHttpMessageHandler : HttpMessageHandler
		{
			private readonly IDictionary<string, string> _jsonByUrl;

			public FakeHttpMessageHandler(IDictionary<string, string> jsonByUrl)
			{
				_jsonByUrl = jsonByUrl;
			}

			protected override Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request, CancellationToken cancellationToken)
			{
				string requestUrl = request.RequestUri.ToString();
				string baseUrl = requestUrl.Split('?')[0];

				if (! _jsonByUrl.TryGetValue(baseUrl, out string json))
				{
					throw new InvalidOperationException(
						$"No canned response set up for {baseUrl}");
				}

				var response = new HttpResponseMessage(HttpStatusCode.OK)
				               {
					               Content = new StringContent(json)
				               };

				return Task.FromResult(response);
			}
		}

		[NotNull]
		private static FeatureServiceSchemaReader CreateOfflineReader(
			IDictionary<string, string> jsonByUrl)
		{
			var httpClient = new HttpClient(new FakeHttpMessageHandler(jsonByUrl));

			return new FeatureServiceSchemaReader(httpClient);
		}

		[Test]
		public void ReaderProducesProSdkStyleTableNames()
		{
			// The REST path must produce the same table names as the production (Pro SDK) path,
			// so that dataset matching behaves identically. For the Wildfire service, layer 0
			// "Wildfire Response Points" must become name "L0Wildfire_Response_Points" with the
			// raw display name as alias.
			var jsonByUrl = new Dictionary<string, string>
			                {
				                [_fakeWildfireUrl] = _fakeWildfireServiceJson,
				                [$"{_fakeWildfireUrl}/0"] = _fakeWildfireLayer0Json
			                };

			GdbWorkspace workspace = CreateOfflineReader(jsonByUrl).ReadWorkspace(_fakeWildfireUrl);

			var pointLayer = workspace.GetDatasets()
			                          .OfType<GdbFeatureClass>()
			                          .FirstOrDefault(fc => fc.ShapeType ==
			                                                esriGeometryType.esriGeometryPoint);

			Assert.NotNull(pointLayer, "Expected a point feature class (layer 0).");
			Assert.AreEqual("L0Wildfire_Response_Points", pointLayer.Name);
			Assert.AreEqual("Wildfire Response Points", pointLayer.AliasName);
		}

		[Test]
		public void ReaderTwoReadsOfSameServiceAreEqual()
		{
			var jsonByUrl = new Dictionary<string, string>
			                {
				                [_fakeWildfireUrl] = _fakeWildfireServiceJson,
				                [$"{_fakeWildfireUrl}/0"] = _fakeWildfireLayer0Json
			                };

			GdbWorkspace ws1 = CreateOfflineReader(jsonByUrl).ReadWorkspace(_fakeWildfireUrl);
			GdbWorkspace ws2 = CreateOfflineReader(jsonByUrl).ReadWorkspace(_fakeWildfireUrl);

			Assert.IsTrue(ws1.Equals(ws2));
			Assert.AreEqual(ws1.GetHashCode(), ws2.GetHashCode());
			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(ws1, ws2));

			// A read workspace and a minimal one (same URL) must also compare equal,
			// since the identity is the (normalized) service URL:
			GdbWorkspace minimal = GdbWorkspace.CreateForFeatureService(_fakeWildfireUrl);
			Assert.IsTrue(ws1.Equals(minimal));
			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(ws1, minimal));
		}

		[Test]
		public void ReaderDifferentServicesAreNotEqual()
		{
			var jsonByUrl = new Dictionary<string, string>
			                {
				                [_fakeWildfireUrl] = _fakeWildfireServiceJson,
				                [$"{_fakeWildfireUrl}/0"] = _fakeWildfireLayer0Json,
				                [_fakeRedlandsUrl] = _fakeRedlandsServiceJson,
				                [$"{_fakeRedlandsUrl}/0"] = _fakeRedlandsLayer0Json
			                };

			var reader = CreateOfflineReader(jsonByUrl);

			GdbWorkspace wildfire = reader.ReadWorkspace(_fakeWildfireUrl);
			GdbWorkspace redlands = reader.ReadWorkspace(_fakeRedlandsUrl);

			Assert.IsFalse(wildfire.Equals(redlands));
			Assert.IsFalse(WorkspaceUtils.IsSameDatabase(wildfire, redlands));
		}

		#endregion

		#region Online tests (require internet access to the ArcGIS sample servers)

		// Kept as a real network test (rather than moved offline like the tests above):
		// this is the canary that the real sample service's JSON shape still matches our
		// parsing assumptions, which canned JSON can't tell us.
		[Test]
		[Category(TestCategory.Online)]
		public void CanReadServiceSchemaFromRest()
		{
			var reader = new FeatureServiceSchemaReader();

			GdbWorkspace workspace = reader.ReadWorkspace(_redlandsUrl);

			Assert.AreEqual(WorkspaceDbType.FeatureService, workspace.DbType);
			Assert.AreEqual(esriWorkspaceType.esriRemoteDatabaseWorkspace, workspace.Type);
			Assert.AreEqual(_redlandsUrl, workspace.PathName);

			var datasets = workspace.GetDatasets().ToList();
			Assert.IsNotEmpty(datasets, "No datasets read from the feature service.");

			var featureClass = datasets.OfType<GdbFeatureClass>().FirstOrDefault();
			Assert.NotNull(featureClass, "No feature class read from the feature service.");
			Assert.AreNotEqual(esriGeometryType.esriGeometryNull, featureClass.ShapeType);
			Assert.Greater(featureClass.Fields.FieldCount, 0, "Feature class has no fields.");
		}

		#endregion

#endif // !ArcGIS
	}
}
