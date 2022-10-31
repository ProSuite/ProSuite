using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Com;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Assert = ProSuite.Commons.Essentials.Assertions.Assert;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class GdbQueryMethodProfilingTest
	{
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

		private static void GetFeaturesInLoop(
			int count,
			[NotNull] Func<IFeatureClass, int, IFeature> getFeatureMethod)
		{
			IFeatureWorkspace featureWorkspace =
				(IFeatureWorkspace) TestUtils.OpenSDEWorkspaceOracle();

			IFeatureClass featureClass =
				featureWorkspace.OpenFeatureClass("TOPGIS_TLM.TLM_DTM_MASSENPUNKTE");

			IList<int> oids = GetFeatureIDs(featureClass, count);

			var watch = new Stopwatch();
			var memoryUsage = new MemoryUsageInfo();
			memoryUsage.Refresh();
			watch.Start();

			IGdbTransaction gdbTransaction = new GdbTransaction();
			gdbTransaction.Execute(
				(IWorkspace) featureWorkspace,
				delegate
				{
					foreach (int oid in oids)
					{
						IFeature feature = getFeatureMethod(featureClass, oid);

						ComUtils.ReleaseComObject(feature);
					}

					memoryUsage.Refresh();
					watch.Stop();

					Console.WriteLine("Features read: {0}", oids.Count);
					Console.WriteLine("Memory usage: {0}", memoryUsage);
					Console.WriteLine("Elapsed: {0:N0} ms", watch.ElapsedMilliseconds);
				}, "test");
		}

		private static IFeature GetFeatureUsingGetFeatures(
			[NotNull] IFeatureClass featureClass, int oid)
		{
			var singleOidList = new List<int> {oid};

			IList<IFeature> features = new List<IFeature>(
				GdbQueryUtils.GetFeatures(featureClass, singleOidList, false));
			Assert.AreEqual(1, features.Count, "unexpected feature count");
			return features[0];
		}

		[NotNull]
		private static IList<int> GetFeatureIDs([NotNull] IFeatureClass featureClass,
		                                        int count)
		{
			var result = new List<int>();
			int current = 0;

			IQueryFilter filter = new QueryFilterClass();
			filter.SubFields = featureClass.OIDFieldName;

			foreach (IFeature feature in GdbQueryUtils.GetFeatures(featureClass, filter, true))
			{
				current++;

				if (current > count)
				{
					break;
				}

				result.Add(feature.OID);
			}

			return result;
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void ProfileGetFeature()
		{
			GetFeaturesInLoop(300, (featureClass, oid) => featureClass.GetFeature(oid));
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void ProfileGetFeatures()
		{
			GetFeaturesInLoop(300, GetFeatureUsingGetFeatures);
		}
	}
}
