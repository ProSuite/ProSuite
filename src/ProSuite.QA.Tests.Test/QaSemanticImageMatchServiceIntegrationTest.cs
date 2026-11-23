using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Com;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using Path = System.IO.Path;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	/// <summary>
	/// Runs <see cref="QaSemanticImageMatch"/> against a running feature check service.
	/// Set these environment variables:
	/// SEMANTIC_IMAGE_MATCH_GDB: file geodatabase with the polygon feature classes
	/// SEMANTIC_IMAGE_MATCH_FEATURES: name of the checked feature class
	/// SEMANTIC_IMAGE_MATCH_CONTEXT: name of the context feature class (optional)
	/// SEMANTIC_IMAGE_MATCH_IMAGE: GeoTIFF, bands 1 to 3 red, green and blue
	/// SEMANTIC_IMAGE_MATCH_RULE: text file with the rule
	/// SEMANTIC_IMAGE_MATCH_API_URL: optional, default http://localhost:8080
	/// SEMANTIC_IMAGE_MATCH_CACHE: optional cache folder; the second run must cost nothing
	/// </summary>
	[TestFixture]
	public class QaSemanticImageMatchServiceIntegrationTest
	{
		[OneTimeSetUp]
		public void SetupFixture() => TestUtils.InitializeLicense();

		[OneTimeTearDown]
		public void TeardownFixture() => TestUtils.ReleaseLicense();

		[Test]
		[Explicit("Calls the feature check service, which costs about 0.02 USD per feature.")]
		public void CanCheckFeaturesWithTheService()
		{
			string gdbPath = GetRequiredVariable("SEMANTIC_IMAGE_MATCH_GDB");
			string featureClassName = GetRequiredVariable("SEMANTIC_IMAGE_MATCH_FEATURES");
			string imagePath = GetRequiredVariable("SEMANTIC_IMAGE_MATCH_IMAGE");
			string rule = File.ReadAllText(GetRequiredVariable("SEMANTIC_IMAGE_MATCH_RULE"));
			string contextName = Environment.GetEnvironmentVariable("SEMANTIC_IMAGE_MATCH_CONTEXT");
			string apiUrl = Environment.GetEnvironmentVariable("SEMANTIC_IMAGE_MATCH_API_URL") ??
			                "http://localhost:8080";
			string cacheDirectory =
				Environment.GetEnvironmentVariable("SEMANTIC_IMAGE_MATCH_CACHE");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(gdbPath);
			IRasterWorkspace2 rasterWorkspace =
				WorkspaceUtils.OpenRasterWorkspace(Path.GetDirectoryName(imagePath));
			IRasterDataset rasterDataset =
				rasterWorkspace.OpenRasterDataset(Path.GetFileName(imagePath));
			IFeatureClass features = workspace.OpenFeatureClass(featureClassName);
			IFeatureClass context = contextName == null
				                        ? null
				                        : workspace.OpenFeatureClass(contextName);
			try
			{
				var contextClasses = new List<IReadOnlyFeatureClass>();
				if (context != null)
				{
					contextClasses.Add(ReadOnlyTableFactory.Create(context));
				}

				QaContainerTestRunner runner = CreateRunner(
					features, rasterDataset, contextClasses, apiUrl, rule, cacheDirectory);
				runner.Execute();
				WriteIssues(runner.Errors);

				Assert.That(runner.Errors.Any(e => e.IssueCode?.ID ==
				                                   "SemanticImageMatch.FeatureReported"));
				Assert.That(runner.Errors.All(e => e.IssueCode?.ID !=
				                                   "SemanticImageMatch.FeatureCheckJobFailed"));

				if (cacheDirectory != null)
				{
					var repeated = CreateRunner(features, rasterDataset, contextClasses,
					                            apiUrl, rule, cacheDirectory);
					repeated.Execute();
					TestContext.WriteLine("Second run:");
					WriteIssues(repeated.Errors);
					Assert.AreEqual(runner.Errors.Count, repeated.Errors.Count);
				}
			}
			finally
			{
				ComUtils.ReleaseComObject(context);
				ComUtils.ReleaseComObject(features);
				ComUtils.ReleaseComObject(rasterDataset);
				ComUtils.ReleaseComObject(rasterWorkspace);
				ComUtils.ReleaseComObject(workspace);
			}
		}

		private static QaContainerTestRunner CreateRunner(
			IFeatureClass features, IRasterDataset rasterDataset,
			IList<IReadOnlyFeatureClass> contextClasses, string apiUrl, string rule,
			string cacheDirectory)
		{
			object exporter = Activator.CreateInstance(
				typeof(QaSemanticImageMatch).Assembly.GetType(
					"ProSuite.QA.Tests.RasterDatasetTileExporter", true));

			var test = (QaSemanticImageMatch) Activator.CreateInstance(
				typeof(QaSemanticImageMatch),
				BindingFlags.Instance | BindingFlags.NonPublic, null,
				new[]
				{
					ReadOnlyTableFactory.Create(features),
					new RasterDatasetReference(rasterDataset), apiUrl, rule,
					new FeatureCheckClient(), exporter
				}, null);
			test.ContextFeatureClasses = contextClasses;
			test.CacheDirectory = cacheDirectory;

			return new QaContainerTestRunner(500, test) { LogErrors = false };
		}

		private static void WriteIssues(IEnumerable<QaError> issues)
		{
			foreach (QaError issue in issues.OrderBy(e => e.IssueCode?.ID))
			{
				string oid = string.Join(",", issue.InvolvedRows.Select(r => r.OID));
				TestContext.WriteLine($"{issue.IssueCode?.ID} [{oid}] {issue.Description}");
			}
		}

		private static string GetRequiredVariable(string name)
		{
			string value = Environment.GetEnvironmentVariable(name);
			if (string.IsNullOrWhiteSpace(value))
			{
				Assert.Ignore($"Environment variable {name} is not set");
			}

			return value;
		}
	}
}
