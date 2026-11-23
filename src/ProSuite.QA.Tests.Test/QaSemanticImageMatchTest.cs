using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Com;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Test.TestRunners;
using Path = System.IO.Path;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSemanticImageMatchTest
	{
		private const double _x0 = 2600000;
		private const double _y0 = 1200000;

		private string _directory;
		private IFeatureWorkspace _workspace;
		private IRasterWorkspace2 _rasterWorkspace;
		private IRasterDataset _rasterDataset;
		private IFeatureClass _features;
		private IFeatureClass _context;
		private ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void SetupFixture() => TestUtils.InitializeLicense();

		[OneTimeTearDown]
		public void TeardownFixture() => TestUtils.ReleaseLicense();

		[SetUp]
		public void SetUp()
		{
			_directory = Path.Combine(Path.GetTempPath(), "QaSemanticImageMatchTest",
			                          Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_directory);

			_spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			_workspace = WorkspaceUtils.OpenShapefileWorkspace(_directory);
			_features = CreatePolygonClass("greenhouses");
			_context = CreatePolygonClass("buildings");

			_rasterWorkspace = WorkspaceUtils.OpenRasterWorkspace(_directory);
			IEnvelope extent = GeometryFactory.CreateEnvelope(
				_x0, _y0, _x0 + 300, _y0 + 100, _spatialReference);
			_rasterDataset = _rasterWorkspace.CreateRasterDataset(
				"image.tif", "TIFF", extent.LowerLeft, 300, 100, 1, 1, 3,
				rstPixelType.PT_UCHAR, _spatialReference);
		}

		[TearDown]
		public void TearDown()
		{
			ComUtils.ReleaseComObject(_features);
			ComUtils.ReleaseComObject(_context);
			ComUtils.ReleaseComObject(_rasterDataset);
			ComUtils.ReleaseComObject(_rasterWorkspace);
			ComUtils.ReleaseComObject(_workspace);
		}

		[Test]
		public void ReportsJudgedFeaturesAndReusesCachedResults()
		{
			long reported = AddFeature(_features, 100, 40, 10);
			long accepted = AddFeature(_features, 150, 40, 10);
			long partlyOutside = AddFeature(_features, 290, 40, 15);
			long tooLarge = AddFeature(_features, 10, 60, 450);
			long withoutAnswer = AddFeature(_features, 200, 40, 10);
			AddFeature(_context, 115, 40, 5);

			var client = new FakeFeatureCheckClient
			             {
				             Answers =
				             {
					             [reported] = 85,
					             [accepted] = 20
				             }
			             };
			string cacheDirectory = Path.Combine(_directory, "cache");

			QaContainerTestRunner runner = CreateRunner(client, cacheDirectory);
			runner.Execute(GetTestExtent());

			Assert.AreEqual(1, CountIssues(runner, "FeatureReported"));
			Assert.AreEqual(1, CountIssues(runner, "FeatureOutsideImage"));
			Assert.AreEqual(1, CountIssues(runner, "FeatureTooLarge"));
			Assert.AreEqual(1, CountIssues(runner, "FeatureCheckFailed"));
			Assert.AreEqual(4, runner.Errors.Count);
			StringAssert.StartsWith("plastic_tunnel (85 %)",
			                        GetIssue(runner, "FeatureReported").Description);

			CollectionAssert.AreEquivalent(new[] { reported, accepted, withoutAnswer },
			                               client.SentObjectIds);
			Assert.IsTrue(client.Requests.All(r => r.FeaturesGeoJson.Contains("EPSG:2056")));
			FeatureCheckRequest requestWithReported = client.Requests.Single(
				r => r.FeaturesGeoJson.Contains($"\"prosuite_oid\":{reported}}}"));
			StringAssert.Contains("2600115", requestWithReported.ContextGeoJson);
			Assert.IsTrue(client.Requests.All(r => r.Rule == "Find plastic tunnels."));
			CollectionAssert.DoesNotContain(client.SentObjectIds, partlyOutside);
			CollectionAssert.DoesNotContain(client.SentObjectIds, tooLarge);

			// Second run: only the feature without a cached answer is sent again.
			client.SentObjectIds.Clear();
			runner = CreateRunner(client, cacheDirectory);
			runner.Execute(GetTestExtent());

			CollectionAssert.AreEquivalent(new[] { withoutAnswer }, client.SentObjectIds);
			Assert.AreEqual(1, CountIssues(runner, "FeatureReported"));
			Assert.AreEqual(4, runner.Errors.Count);
		}

		[Test]
		public void UsesResultsPreparedInAReadOnlySharedFolder()
		{
			long reported = AddFeature(_features, 100, 40, 10);
			long accepted = AddFeature(_features, 150, 40, 10);
			string sharedDirectory = Path.Combine(_directory, "shared");

			// A specialist checks the whole area once.
			var specialistClient = new FakeFeatureCheckClient
			                       {
				                       Answers = { [reported] = 85, [accepted] = 20 }
			                       };
			CreateRunner(specialistClient, sharedDirectory).Execute(GetTestExtent());
			Assert.AreEqual(2, specialistClient.SentObjectIds.Count);

			foreach (string file in Directory.GetFiles(sharedDirectory))
			{
				File.SetAttributes(file, FileAttributes.ReadOnly);
			}

			try
			{
				// A user verifies a smaller extent without access to the service.
				var userClient = new FakeFeatureCheckClient { FailWith = "Not reachable" };
				IEnvelope userExtent = GeometryFactory.CreateEnvelope(
					_x0 + 90, _y0 + 30, _x0 + 120, _y0 + 60, _spatialReference);

				QaContainerTestRunner runner = CreateRunner(userClient, sharedDirectory);
				runner.Execute(userExtent);

				Assert.AreEqual(0, userClient.Requests.Count);
				Assert.AreEqual(1, runner.Errors.Count);
				Assert.AreEqual(1, CountIssues(runner, "FeatureReported"));
			}
			finally
			{
				foreach (string file in Directory.GetFiles(sharedDirectory))
				{
					File.SetAttributes(file, FileAttributes.Normal);
				}
			}
		}

		[Test]
		public void EmptyCacheDirectorySendsAllFeaturesOnEveryRun()
		{
			long oid = AddFeature(_features, 100, 40, 10);
			var client = new FakeFeatureCheckClient { Answers = { [oid] = 10 } };

			CreateRunner(client, string.Empty).Execute(GetTestExtent());
			CreateRunner(client, string.Empty).Execute(GetTestExtent());

			Assert.AreEqual(2, client.SentObjectIds.Count);
		}

		[Test]
		public void CacheDirectoryExpandsEnvironmentVariables()
		{
			long oid = AddFeature(_features, 100, 40, 10);
			var client = new FakeFeatureCheckClient { Answers = { [oid] = 10 } };
			const string variable = "QA_SEMANTIC_IMAGE_MATCH_TEST_DIRECTORY";
			Environment.SetEnvironmentVariable(variable, _directory);
			try
			{
				string cacheDirectory = $@"%{variable}%\cache";

				CreateRunner(client, cacheDirectory).Execute(GetTestExtent());
				CreateRunner(client, cacheDirectory).Execute(GetTestExtent());

				Assert.AreEqual(1, client.SentObjectIds.Count);
				Assert.AreEqual(1, Directory.GetFiles(Path.Combine(_directory, "cache")).Length);
			}
			finally
			{
				Environment.SetEnvironmentVariable(variable, null);
			}
		}

		[Test]
		public void ChangedGeometryIsSentAgain()
		{
			long oid = AddFeature(_features, 100, 40, 10);
			var client = new FakeFeatureCheckClient { Answers = { [oid] = 10 } };
			string cacheDirectory = Path.Combine(_directory, "cache");

			CreateRunner(client, cacheDirectory).Execute(GetTestExtent());
			Assert.AreEqual(1, client.SentObjectIds.Count);

			IFeature feature = _features.GetFeature(oid);
			feature.Shape = CreateSquare(101, 40, 10);
			feature.Store();
			ComUtils.ReleaseComObject(feature);

			CreateRunner(client, cacheDirectory).Execute(GetTestExtent());
			Assert.AreEqual(2, client.SentObjectIds.Count);
		}

		[Test]
		public void FailedRequestIsReportedForEachFeature()
		{
			// Both inside one container tile, so they are sent in one request.
			long first = AddFeature(_features, 120, 40, 10);
			long second = AddFeature(_features, 150, 40, 10);
			var client = new FakeFeatureCheckClient { FailWith = "No worker serves it" };

			QaContainerTestRunner runner = CreateRunner(client, null);
			runner.Execute(GetTestExtent());

			Assert.AreEqual(1, client.Requests.Count);
			Assert.AreEqual(2, CountIssues(runner, "FeatureCheckJobFailed"));
			Assert.AreEqual(2, runner.Errors.Count);
			CollectionAssert.AreEquivalent(
				new[] { first, second },
				runner.Errors.Select(e => e.InvolvedRows.Single().OID));
			StringAssert.Contains("No worker serves it", runner.Errors[0].Description);
		}

		[Test]
		public void UnreachableServiceReportsOnlyUncheckedFeatures()
		{
			long reported = AddFeature(_features, 100, 40, 10);
			long accepted = AddFeature(_features, 150, 40, 10);
			string cacheDirectory = Path.Combine(_directory, "cache");
			var client = new FakeFeatureCheckClient
			             {
				             Answers = { [reported] = 85, [accepted] = 20 }
			             };
			CreateRunner(client, cacheDirectory).Execute(GetTestExtent());

			long added = AddFeature(_features, 200, 40, 10);

			// Nothing listens on this port.
			string apiUrl = $"http://localhost:{FeatureCheckClientTest.GetFreeTcpPort()}/";
			QaContainerTestRunner runner =
				CreateRunner(new FeatureCheckClient(), cacheDirectory, apiUrl);
			runner.Execute(GetTestExtent());

			Assert.AreEqual(2, runner.Errors.Count);
			Assert.AreEqual(reported,
			                GetIssue(runner, "FeatureReported").InvolvedRows.Single().OID);
			QaError notChecked = GetIssue(runner, "FeatureCheckJobFailed");
			Assert.AreEqual(added, notChecked.InvolvedRows.Single().OID);
			StringAssert.Contains("job not created", notChecked.Description);
		}

		[Test]
		public void RejectsARuleThatTheDataDictionaryCannotStore()
		{
			string rule = new string('x', QaSemanticImageMatchDefinition.MaximumRuleLength + 1);

			Assert.Throws<ArgumentException>(
				() => new QaSemanticImageMatch(
					ReadOnlyTableFactory.Create(_features),
					new RasterDatasetReference(_rasterDataset), "http://localhost", rule));
		}

		[Test]
		public void RuleParameterIsMarkedAsMultilineText()
		{
			Assert.NotNull(typeof(QaSemanticImageMatchDefinition)
			               .GetProperty(nameof(QaSemanticImageMatchDefinition.Rule))
			               .GetCustomAttribute<MultilineTextAttribute>());
			Assert.NotNull(typeof(QaSemanticImageMatch)
			               .GetProperty(nameof(QaSemanticImageMatch.Rule))
			               .GetCustomAttribute<MultilineTextAttribute>());
		}

		[Test]
		public void OptionalParameterMetadataMatchesDefinition()
		{
			const string name = "MinimumErrorProbability";
			PropertyInfo implementation = typeof(QaSemanticImageMatch).GetProperty(name);
			PropertyInfo definition = typeof(QaSemanticImageMatchDefinition).GetProperty(name);

			Assert.AreEqual(implementation.PropertyType, definition.PropertyType);
			Assert.AreEqual(
				implementation.GetCustomAttribute<TestParameterAttribute>().DefaultValue,
				definition.GetCustomAttribute<TestParameterAttribute>().DefaultValue);
			Assert.AreEqual(implementation.GetCustomAttribute<DocAttribute>().Description,
			                definition.GetCustomAttribute<DocAttribute>().Description);
		}

		[Test]
		public void DefinitionConstructorsMatchTheTest()
		{
			CollectionAssert.AreEqual(GetPublicConstructorParameterNames(
				                          typeof(QaSemanticImageMatchDefinition)),
			                          GetPublicConstructorParameterNames(
				                          typeof(QaSemanticImageMatch)));
			CollectionAssert.AreEqual(GetOptionalParameterNames(
				                          typeof(QaSemanticImageMatchDefinition)),
			                          GetOptionalParameterNames(typeof(QaSemanticImageMatch)));
			CollectionAssert.Contains(GetOptionalParameterNames(typeof(QaSemanticImageMatch)),
			                          nameof(QaSemanticImageMatch.ContextFeatureClasses));
		}

		private static List<string> GetPublicConstructorParameterNames(Type type)
		{
			return type.GetConstructors()
			           .Where(c => c.GetParameters().All(
				                  p => p.ParameterType != typeof(QaSemanticImageMatchDefinition)))
			           .Select(c => string.Join(
				                   ", ", c.GetParameters().Select(p => p.Name)))
			           .ToList();
		}

		private static List<string> GetOptionalParameterNames(Type type)
		{
			return type.GetProperties()
			           .Where(p => p.GetCustomAttribute<TestParameterAttribute>() != null)
			           .Select(p => p.Name)
			           .OrderBy(n => n)
			           .ToList();
		}

		private QaContainerTestRunner CreateRunner(IFeatureCheckClient client,
		                                           string cacheDirectory,
		                                           string apiUrl = "http://localhost")
		{
			object exporter = Activator.CreateInstance(
				typeof(QaSemanticImageMatch).Assembly.GetType(
					"ProSuite.QA.Tests.RasterDatasetTileExporter", true));

			var test = (QaSemanticImageMatch) Activator.CreateInstance(
				typeof(QaSemanticImageMatch),
				BindingFlags.Instance | BindingFlags.NonPublic, null,
				new[]
				{
					ReadOnlyTableFactory.Create(_features),
					new RasterDatasetReference(_rasterDataset),
					apiUrl, "Find plastic tunnels.", client, exporter
				}, null);
			test.CacheDirectory = cacheDirectory;
			test.ContextFeatureClasses =
				new List<IReadOnlyFeatureClass> { ReadOnlyTableFactory.Create(_context) };

			return new QaContainerTestRunner(100, test);
		}

		private IEnvelope GetTestExtent()
		{
			return GeometryFactory.CreateEnvelope(_x0 - 100, _y0 - 100, _x0 + 600, _y0 + 200,
			                                      _spatialReference);
		}

		private static int CountIssues(QaContainerTestRunner runner, string code)
		{
			return runner.Errors.Count(e => e.IssueCode?.ID == "SemanticImageMatch." + code);
		}

		private static QaError GetIssue(QaContainerTestRunner runner, string code)
		{
			return runner.Errors.Single(e => e.IssueCode?.ID == "SemanticImageMatch." + code);
		}

		private IFeatureClass CreatePolygonClass(string name)
		{
			return DatasetUtils.CreateSimpleFeatureClass(
				_workspace, name,
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
					                            _spatialReference)));
		}

		private long AddFeature(IFeatureClass featureClass, double x, double y, double size)
		{
			IFeature feature = featureClass.CreateFeature();
			try
			{
				feature.Shape = CreateSquare(x, y, size);
				feature.Store();
				return feature.OID;
			}
			finally
			{
				ComUtils.ReleaseComObject(feature);
			}
		}

		private IPolygon CreateSquare(double x, double y, double size)
		{
			return GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(
				                                     _x0 + x, _y0 + y, _x0 + x + size,
				                                     _y0 + y + size, _spatialReference));
		}

		private class FakeFeatureCheckClient : IFeatureCheckClient
		{
			private static readonly Regex _objectIdPattern =
				new Regex($"\"{FeatureCheckClient.ObjectIdProperty}\":(\\d+)");

			public Dictionary<long, int> Answers { get; } = new Dictionary<long, int>();
			public List<long> SentObjectIds { get; } = new List<long>();
			public List<FeatureCheckRequest> Requests { get; } = new List<FeatureCheckRequest>();
			public string FailWith { get; set; }

			public IList<FeatureCheckJobResult> CheckFeatures(
				IList<FeatureCheckRequest> requests, string apiUrl, TimeSpan timeout)
			{
				var result = new List<FeatureCheckJobResult>();
				foreach (FeatureCheckRequest request in requests)
				{
					Assert.IsTrue(File.Exists(request.ImagePath));
					Requests.Add(request);

					if (FailWith != null)
					{
						result.Add(FeatureCheckJobResult.Failed(request, "job", FailWith));
						continue;
					}

					var answers = new List<FeatureCheckResult>();
					foreach (Match match in _objectIdPattern.Matches(request.FeaturesGeoJson))
					{
						long oid = long.Parse(match.Groups[1].Value);
						SentObjectIds.Add(oid);

						int probability;
						if (Answers.TryGetValue(oid, out probability))
						{
							answers.Add(new FeatureCheckResult(
								            oid, FeatureCheckStatus.Checked, probability,
								            probability > 50 ? "plastic_tunnel" : "permanent_greenhouse",
								            "Begründung."));
						}
					}

					result.Add(new FeatureCheckJobResult(request, "job", answers, null, 0.02,
					                                     "test-model"));
				}

				return result;
			}
		}
	}
}
