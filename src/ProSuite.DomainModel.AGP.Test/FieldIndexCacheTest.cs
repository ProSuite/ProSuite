using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.DomainModel.AGP.DataModel;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.DomainModel.AGP.Test
{
	[TestFixture]
	public class FieldIndexCacheTest
	{
		private const int RandomSeed = 3;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void PerformanceTestFGDB()
		{
			const int numberOfRuns = 30;
			const int numberOfRepetitionsPerRun = 25;

			// Test with file gdb
			const string gdbPath = @"C:\data\swisstopo\k2\WU226_Rapperswil\WU226_Rapperswil.gdb";
			using var gdb = WorkspaceUtils.OpenFileGeodatabase(gdbPath);
			ExecutePerformanceTest(gdb, "FGDB \"Rapperswil\"", "DKM50_STRASSE", numberOfRuns,
			                       numberOfRepetitionsPerRun);
			ExecutePerformanceTest(gdb, "FGDB \"Rapperswil\"", "DKM50_GEBAEUDE", numberOfRuns,
			                       numberOfRepetitionsPerRun);
		}

		[Test]
		public void PerformanceTestSDE()
		{
			const int numberOfRuns = 15;
			const int numberOfRepetitionsPerRun = 1;

			// Test with local SDE database
			DatabaseConnectionProperties connectionProperties =
				new DatabaseConnectionProperties(EnterpriseDatabaseType.Oracle)
				{
					AuthenticationMode = AuthenticationMode.OSA,
					Instance = "dkm50k2",
					User = "dkm50k2_manager",
					Password = "dkm50k2_manager",
					Database = "dkm50k2"
				};
			using var gdb = (ArcGIS.Core.Data.Geodatabase) WorkspaceUtils.OpenDatastore(connectionProperties);

			ExecutePerformanceTest(gdb, "Local Oracle SDE", "DKM50K2_MANAGER.DKM50_STRASSE",
			                       numberOfRuns, numberOfRepetitionsPerRun);
			ExecutePerformanceTest(gdb, "Local Oracle SDE", "DKM50K2_MANAGER.DKM50_GEBAEUDE",
			                       numberOfRuns, numberOfRepetitionsPerRun);
		}

		private void ExecutePerformanceTest(ArcGIS.Core.Data.Geodatabase gdb, string gdbName, string datasetName,
		                                    int numberOfRuns, int numberOfRepetitionsPerRun)
		{
			Console.Out.WriteLine($"Testing {gdbName} with dataset \"{datasetName}\"");

			var watchTotalTime = new Stopwatch();
			watchTotalTime.Start();

			using var featureClass = gdb.OpenDataset<FeatureClass>(datasetName);

			Dictionary<string, Action<FeatureClass, List<Row>, IReadOnlyList<Field>, Random,
				List<string>>> testFunctions = [];
			testFunctions["with fieldIndexCache"] = TestWithFieldIndexCache;
			testFunctions["without fieldIndexCache by index"] = TestWithoutFieldIndexCache;
			testFunctions["without fieldIndexCache by pre calc index"] =
				TestWithoutFieldIndexCachePreCalc;
			testFunctions["without fieldIndexCache by name"] = TestWithoutFieldIndexCacheByName;

			QueryFilter filter = new QueryFilter();
			var rows = GdbQueryUtils.GetRows<Row>(featureClass, filter, false).ToList();
			var fields = featureClass.GetDefinition().GetFields();

			Console.Out.WriteLine(
				"Number of index calls: {0} rows repeated {1} times -> {2} index queries",
				rows.Count, numberOfRepetitionsPerRun, numberOfRepetitionsPerRun * rows.Count);

			List<List<string>> results = [];
			Dictionary<string, List<long>> durations = [];
			for (int j = 0; j < numberOfRuns; ++j)
			{
				foreach (var pair in testFunctions)
				{
					Random random = new Random(RandomSeed);

					List<string> result = [];

					var watch = new Stopwatch();
					watch.Start();
					for (int i = 0; i < numberOfRepetitionsPerRun; ++i)
					{
						pair.Value(featureClass, rows, fields, random, result);
					}

					watch.Stop();

					results.Add(result);

					if (durations.TryGetValue(pair.Key, out var list))
					{
						list.Add(watch.ElapsedMilliseconds);
					}
					else
					{
						durations[pair.Key] = [watch.ElapsedMilliseconds];
					}
				}
			}

			for (int i = 1; i < results.Count; ++i)
			{
				Assert.True(results[0].SequenceEqual(results[i]), $"Different results in test {i}");
			}

			foreach (var pair in durations)
			{
				Console.Out.WriteLine(
					"Test {0}: Average {1:F1} ms over {2} runs with standard deviation {3:F1}",
					pair.Key,
					pair.Value.Average(), numberOfRuns, GetStandardDeviation(pair.Value));
			}

			watchTotalTime.Stop();
			Console.Out.Write("Total time: {0} ms", watchTotalTime.ElapsedMilliseconds);

			Console.Out.Write("\n");
		}

		private static double GetStandardDeviation(List<long> values)
		{
			double standardDeviation = 0;

			if (values.Count > 0)
			{
				double avg = values.Average();
				double sum = values.Sum(d => Math.Pow(d - avg, 2));
				standardDeviation = Math.Sqrt((sum) / values.Count);
			}

			return standardDeviation;
		}

		private static void TestWithFieldIndexCache(FeatureClass featureClass, List<Row> rows,
		                                            IReadOnlyList<Field> fields, Random random,
		                                            List<string> results)
		{
			var cache = new FieldIndexCache();
			foreach (var row in rows)
			{
				string fieldName = fields[random.Next(fields.Count)].Name;

				int index = cache.GetFieldIndex(featureClass, fieldName);
				var item = row[index];
				if (item != null)
				{
					results.Add(item.ToString());
				}
			}
		}

		private static void TestWithoutFieldIndexCache(FeatureClass featureClass, List<Row> rows,
		                                               IReadOnlyList<Field> fields, Random random,
		                                               List<string> results)
		{
			foreach (var row in rows)
			{
				string fieldName = fields[random.Next(fields.Count)].Name;

				int index = row.FindField(fieldName);
				var item = row[index];
				if (item != null)
				{
					results.Add(item.ToString());
				}
			}
		}

		private static void TestWithoutFieldIndexCachePreCalc(
			FeatureClass featureClass, List<Row> rows,
			IReadOnlyList<Field> fields, Random random,
			List<string> results)
		{
			Dictionary<string, int> indices = [];
			Row firstRow = rows.First();
			foreach (var field in fields)
			{
				indices[field.Name] = firstRow.FindField(field.Name);
			}

			foreach (var row in rows)
			{
				string fieldName = fields[random.Next(fields.Count)].Name;

				int index = indices[fieldName];
				var item = row[index];
				if (item != null)
				{
					results.Add(item.ToString());
				}
			}
		}

		private static void TestWithoutFieldIndexCacheByName(
			FeatureClass featureClass, List<Row> rows, IReadOnlyList<Field> fields, Random random,
			List<string> results)
		{
			foreach (var row in rows)
			{
				string fieldName = fields[random.Next(fields.Count)].Name;

				var item = row[fieldName];
				if (item != null)
				{
					results.Add(item.ToString());
				}
			}
		}
	}
}
