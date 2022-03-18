using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Diagnostics;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaForeignKeyTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _workspace;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaForeignKeyTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanFindDanglingSimpleKey()
		{
			const string key = "KEY1";
			const string fkey = "FKEY1";

			ITable referencedTable = DatasetUtils.CreateTable(
				_workspace,
				string.Format("{0}_key", MethodBase.GetCurrentMethod()?.Name),
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField(key));

			ITable referencingTable = DatasetUtils.CreateTable(
				_workspace,
				string.Format("{0}_fkey", MethodBase.GetCurrentMethod()?.Name),
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField(fkey, 10));

			GdbObjectUtils.CreateRow(referencedTable,
			                         new Dictionary<string, object> {{key, 1000}}).Store();
			GdbObjectUtils.CreateRow(referencedTable,
			                         new Dictionary<string, object> {{key, 2000}}).Store();

			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object> {{fkey, "1000"}}).Store();
			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object> {{fkey, "2000"}}).Store();
			// dangling reference:
			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object> {{fkey, "3000"}}).Store();

			var runner = new QaTestRunner(
				new QaForeignKey(ReadOnlyTableFactory.Create(referencingTable), fkey,
				                 ReadOnlyTableFactory.Create(referencedTable), key));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanFindDanglingTupleKey()
		{
			const string key1 = "KEY1";
			const string key2 = "KEY2";
			const string fkey1 = "FKEY1";
			const string fkey2 = "FKEY2";

			ITable referencedTable = DatasetUtils.CreateTable(
				_workspace,
				string.Format("{0}_keys", MethodBase.GetCurrentMethod()?.Name),
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField(key1),
				FieldUtils.CreateTextField(key2, 10));

			ITable referencingTable = DatasetUtils.CreateTable(
				_workspace,
				string.Format("{0}_fkeys", MethodBase.GetCurrentMethod()?.Name),
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField(fkey1, 10),
				FieldUtils.CreateTextField(fkey2, 10));

			GdbObjectUtils.CreateRow(referencedTable,
			                         new Dictionary<string, object>
			                         {
				                         {key1, 1000},
				                         {key2, "AAA"}
			                         }).Store();
			GdbObjectUtils.CreateRow(referencedTable,
			                         new Dictionary<string, object>
			                         {
				                         {key1, 2000},
				                         {key2, "BBB"}
			                         }).Store();

			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object>
			                         {
				                         {fkey1, "1000"},
				                         {fkey2, "AAA"}
			                         }).Store();
			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object>
			                         {
				                         {fkey1, "2000"},
				                         {fkey2, "BBB"}
			                         }).Store();
			// dangling reference:
			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object>
			                         {
				                         {fkey1, "3000"},
				                         {fkey2, "XXX"}
			                         }).Store();

			var runner = new QaTestRunner(
				new QaForeignKey(ReadOnlyTableFactory.Create(referencingTable),
				                 new[] { fkey1, fkey2 },
				                 ReadOnlyTableFactory.Create(referencedTable),
				                 new[] { key1, key2 }));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanCheckTupleKeysContainingNull()
		{
			const string key1 = "KEY1";
			const string key2 = "KEY2";
			const string fkey1 = "FKEY1";
			const string fkey2 = "FKEY2";

			ITable referencedTable = DatasetUtils.CreateTable(
				_workspace,
				string.Format("{0}_keys", MethodBase.GetCurrentMethod()?.Name),
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField(key1),
				FieldUtils.CreateTextField(key2, 10));

			ITable referencingTable = DatasetUtils.CreateTable(
				_workspace,
				string.Format("{0}_fkeys", MethodBase.GetCurrentMethod()?.Name),
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField(fkey1, 10),
				FieldUtils.CreateTextField(fkey2, 10));

			GdbObjectUtils.CreateRow(referencedTable,
			                         new Dictionary<string, object>
			                         {
				                         {key1, 1000},
				                         {key2, "AAA"}
			                         }).Store();
			GdbObjectUtils.CreateRow(referencedTable,
			                         new Dictionary<string, object>
			                         {
				                         {key1, 2000},
				                         {key2, null}
			                         }).Store();

			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object>
			                         {
				                         {fkey1, "1000"},
				                         {fkey2, "AAA"}
			                         }).Store();
			// valid reference with null key
			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object>
			                         {
				                         {fkey1, "2000"},
				                         {fkey2, null}
			                         }).Store();
			// dangling reference:
			GdbObjectUtils.CreateRow(referencingTable,
			                         new Dictionary<string, object>
			                         {
				                         {fkey1, "3000"},
				                         {fkey2, null}
			                         }).Store();

			var runner = new QaTestRunner(
				new QaForeignKey(ReadOnlyTableFactory.Create(referencingTable),
				                 new[] { fkey1, fkey2 },
				                 ReadOnlyTableFactory.Create(referencedTable),
				                 new[] { key1, key2 }));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void LearningTestSimpleSetWithBoxing()
		{
			var memoryUsageInfo = new MemoryUsageInfo();
			memoryUsageInfo.Refresh();

			TestContainsWithBoxing(1000000);

			memoryUsageInfo.Refresh();
			Console.WriteLine(@"Memory usage: {0}", memoryUsageInfo);

			GC.Collect();
			GC.WaitForPendingFinalizers();

			memoryUsageInfo.Refresh();
			Console.WriteLine(@"Memory usage after GC: {0}", memoryUsageInfo);

			// for 10'000'000 keys:
			// --> boxing makes the contains test about 2x slower (773ms instead of 334ms), 
			//     and it uses much more memory (380 mb instead of 20 mb)
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void TestContainsWithBoxing(int count)
		{
			var set = new HashSet<object>();
			for (var i = 0; i < count; i++)
			{
				set.Add(i);
			}

			var watch = new Stopwatch();
			watch.Start();

			for (var i = 0; i < count; i++)
			{
				Assert.IsTrue(set.Contains(i));
			}

			watch.Stop();

			Console.WriteLine(@"{0} contains tests in {1} ms", count, watch.ElapsedMilliseconds);
		}

		[Test]
		public void LearningTestSimpleSetWithoutBoxing()
		{
			var memoryUsageInfo = new MemoryUsageInfo();
			memoryUsageInfo.Refresh();

			TestContainsWithoutBoxing(1000000);
			memoryUsageInfo.Refresh();
			Console.WriteLine(@"Memory usage: {0}", memoryUsageInfo);

			GC.Collect();
			GC.WaitForPendingFinalizers();

			memoryUsageInfo.Refresh();
			Console.WriteLine(@"Memory usage after GC: {0}", memoryUsageInfo);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void TestContainsWithoutBoxing(int count)
		{
			var set = new HashSet<int>();
			for (var i = 0; i < count; i++)
			{
				set.Add(i);
			}

			var watch = new Stopwatch();
			watch.Start();

			for (var i = 0; i < count; i++)
			{
				Assert.IsTrue(set.Contains(i));
			}

			watch.Stop();

			Console.WriteLine(@"{0} contains tests in {1} ms", count, watch.ElapsedMilliseconds);
		}
	}
}
