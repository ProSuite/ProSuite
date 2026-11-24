using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class SemanticImageMatchGroupingTest
	{
		[Test]
		public void SmallFeatureGetsTheMinimumWindowOf30Units()
		{
			PlanarExtent window = SemanticImageMatchGrouping.GetCheckWindow(
				new PlanarExtent(100, 200, 110, 204));

			Assert.AreEqual(90, window.XMin, 1e-9);
			Assert.AreEqual(187, window.YMin, 1e-9);
			Assert.AreEqual(120, window.XMax, 1e-9);
			Assert.AreEqual(217, window.YMax, 1e-9);
		}

		[Test]
		public void LargeFeatureGetsAWindowOf2Point5TimesItsLargerSide()
		{
			PlanarExtent window = SemanticImageMatchGrouping.GetCheckWindow(
				new PlanarExtent(0, 0, 40, 20));

			Assert.AreEqual(-30, window.XMin, 1e-9);
			Assert.AreEqual(-40, window.YMin, 1e-9);
			Assert.AreEqual(70, window.XMax, 1e-9);
			Assert.AreEqual(60, window.YMax, 1e-9);
		}

		[Test]
		public void FeatureLargerThan400UnitsIsTooLarge()
		{
			Assert.IsFalse(SemanticImageMatchGrouping.IsTooLarge(new PlanarExtent(0, 0, 400, 10)));
			Assert.IsTrue(SemanticImageMatchGrouping.IsTooLarge(new PlanarExtent(0, 0, 10, 401)));
		}

		[Test]
		public void GroupsByTheCellOfTheEnvelopeCentre()
		{
			var envelopes = new Dictionary<long, PlanarExtent>
			                {
				                // centre in cell (0, 0) although the feature reaches cell (1, 0)
				                { 1, new PlanarExtent(490, 10, 505, 20) },
				                { 2, new PlanarExtent(10, 10, 20, 20) },
				                { 3, new PlanarExtent(600, 10, 610, 20) }
			                };

			IList<FeatureCheckGroup> groups = SemanticImageMatchGrouping.Group(envelopes);

			Assert.AreEqual(2, groups.Count);
			CollectionAssert.AreEqual(new long[] { 1, 2 }, groups[0].ObjectIds);
			CollectionAssert.AreEqual(new long[] { 3 }, groups[1].ObjectIds);
		}

		[Test]
		public void ImageExtentCoversTheWindowsOfAllFeaturesOfTheGroup()
		{
			var envelopes = new Dictionary<long, PlanarExtent>
			                {
				                { 1, new PlanarExtent(10, 10, 20, 20) },
				                { 2, new PlanarExtent(300, 400, 340, 420) }
			                };

			FeatureCheckGroup group = SemanticImageMatchGrouping.Group(envelopes).Single();

			foreach (PlanarExtent envelope in envelopes.Values)
			{
				Assert.IsTrue(group.ImageExtent.Contains(
					              SemanticImageMatchGrouping.GetCheckWindow(envelope)));
			}

			Assert.AreEqual(0, group.ImageExtent.XMin, 1e-9);
			Assert.AreEqual(370, group.ImageExtent.XMax, 1e-9);
		}

		[Test]
		public void SplitsACellWithMoreFeaturesThanAllowedPerRequest()
		{
			var envelopes = new Dictionary<long, PlanarExtent>();
			for (var oid = 1; oid <= 5; oid++)
			{
				envelopes.Add(oid, new PlanarExtent(oid * 20, 10, oid * 20 + 5, 15));
			}

			IList<FeatureCheckGroup> groups = SemanticImageMatchGrouping.Group(
				envelopes, maximumFeaturesPerGroup: 2);

			Assert.AreEqual(3, groups.Count);
			CollectionAssert.AreEqual(new long[] { 1, 2, 3, 4, 5 },
			                          groups.SelectMany(g => g.ObjectIds));
		}
	}

	[TestFixture]
	public class SemanticImageMatchCacheTest
	{
		private string _directory;

		[SetUp]
		public void SetUp()
		{
			_directory = Path.Combine(Path.GetTempPath(), "SemanticImageMatchCacheTest",
			                          Path.GetRandomFileName());
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_directory))
			{
				Directory.Delete(_directory, recursive: true);
			}
		}

		[Test]
		public void ReturnsAStoredResultWhileTheGeometryIsUnchanged()
		{
			var writer = new SemanticImageMatchCache(_directory, "buildings|image|rule");
			writer.Add(new[]
			           {
				           Pair(new FeatureCheckResult(7, FeatureCheckStatus.Checked, 85,
				                                       "plastic_tunnel", "Folientunnel."),
				                "geometry-a")
			           });

			var reader = new SemanticImageMatchCache(_directory, "buildings|image|rule");

			FeatureCheckResult result;
			Assert.IsTrue(reader.TryGet(7, "geometry-a", out result));
			Assert.AreEqual(85, result.ErrorProbability);
			Assert.AreEqual("plastic_tunnel", result.Category);
			Assert.AreEqual("Folientunnel.", result.Reason);

			Assert.IsFalse(reader.TryGet(7, "geometry-b", out result));
			Assert.IsFalse(reader.TryGet(8, "geometry-a", out result));
		}

		[Test]
		public void AnotherRuleUsesAnotherFile()
		{
			var cache = new SemanticImageMatchCache(_directory, "buildings|image|rule");
			cache.Add(new[]
			          {
				          Pair(new FeatureCheckResult(7, FeatureCheckStatus.Checked, 10, "ok", ""),
				               "geometry-a")
			          });

			var otherRule = new SemanticImageMatchCache(_directory, "buildings|image|other");

			Assert.AreNotEqual(cache.FilePath, otherRule.FilePath);
			FeatureCheckResult result;
			Assert.IsFalse(otherRule.TryGet(7, "geometry-a", out result));
		}

		[Test]
		public void KeepsResultsThatAnotherVerificationWroteMeanwhile()
		{
			var first = new SemanticImageMatchCache(_directory, "scope");
			var second = new SemanticImageMatchCache(_directory, "scope");

			FeatureCheckResult result;
			Assert.IsFalse(first.TryGet(1, "g", out result));

			second.Add(new[]
			           {
				           Pair(new FeatureCheckResult(2, FeatureCheckStatus.Checked, 5, "ok", ""),
				                "g")
			           });
			first.Add(new[]
			          {
				          Pair(new FeatureCheckResult(1, FeatureCheckStatus.Checked, 5, "ok", ""),
				               "g")
			          });

			var reader = new SemanticImageMatchCache(_directory, "scope");
			Assert.IsTrue(reader.TryGet(1, "g", out result));
			Assert.IsTrue(reader.TryGet(2, "g", out result));
		}

		[Test]
		public void DoesNotStoreUncheckedFeatures()
		{
			var cache = new SemanticImageMatchCache(_directory, "scope");
			cache.Add(new[]
			          {
				          Pair(new FeatureCheckResult(1, FeatureCheckStatus.OutsideImage,
				                                      null, null, null), "g"),
				          Pair(new FeatureCheckResult(2, FeatureCheckStatus.Failed,
				                                      null, null, null), "g")
			          });

			FeatureCheckResult result;
			var reader = new SemanticImageMatchCache(_directory, "scope");
			Assert.IsFalse(reader.TryGet(1, "g", out result));
			Assert.IsFalse(reader.TryGet(2, "g", out result));
		}

		private static KeyValuePair<FeatureCheckResult, string> Pair(
			FeatureCheckResult result, string geometryFingerprint)
		{
			return new KeyValuePair<FeatureCheckResult, string>(result, geometryFingerprint);
		}
	}
}
