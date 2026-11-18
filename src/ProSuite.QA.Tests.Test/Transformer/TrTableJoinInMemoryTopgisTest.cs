using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	/// <summary>
	/// Runs the quality conditions TLMQA-455 (Strassenroute) and TLMQA-695 (Stromtrasse) on the
	/// real TOPGIST data, configured as in TOPGIST_DDX:
	/// TrMakeTable (bridge table) -> TrTableJoinInMemory (m:n) -> QaDuplicateGeometrySelf.
	/// The same area is verified twice with different tile sizes; the issues must not change.
	/// Connection file: PROSUITE_TEST_TOPGIST_CONNECTION_FILE, or the ArcGIS Pro favourite
	/// "Oracle-TOPGIST(OSA).sde". Details are written to PROSUITE_TEST_LOG_FILE.
	/// </summary>
	[TestFixture]
	[Explicit("Needs the TOPGIST Oracle database")]
	public class TrTableJoinInMemoryTopgisTest
	{
		private static readonly string _connectionFile =
			Environment.GetEnvironmentVariable("PROSUITE_TEST_TOPGIST_CONNECTION_FILE") ??
			System.IO.Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				@"Esri\ArcGISPro\Favorites\Oracle-TOPGIST(OSA).sde");

		private static readonly string _logFile =
			Environment.GetEnvironmentVariable("PROSUITE_TEST_LOG_FILE") ??
			@"C:\Temp\TrTableJoinInMemoryTopgisTest.txt";

		// Copied from TOPGIST_DDX, quality condition 8563542 (TLMQA-455):
		private const string _strassenrouteConstraint =
			"(G1.TLM_STRASSENROUTE_OBJEKTART = 0 AND G2.TLM_STRASSENROUTE_OBJEKTART <> -1) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 1 AND G2.TLM_STRASSENROUTE_OBJEKTART <> -1) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 2 AND G2.TLM_STRASSENROUTE_OBJEKTART <> 3) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 3 AND G2.TLM_STRASSENROUTE_OBJEKTART <> 2) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 4 AND G2.TLM_STRASSENROUTE_OBJEKTART <> -1) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 5 AND G2.TLM_STRASSENROUTE_OBJEKTART <> 10) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 6 AND G2.TLM_STRASSENROUTE_OBJEKTART <> 8) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 7 AND G2.TLM_STRASSENROUTE_OBJEKTART <> 8) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 8 AND G2.TLM_STRASSENROUTE_OBJEKTART NOT IN (6, 7, 10)) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 9 AND G2.TLM_STRASSENROUTE_OBJEKTART <> -1) OR (G1.TLM_STRASSENROUTE_OBJEKTART = 10 AND G2.TLM_STRASSENROUTE_OBJEKTART NOT IN (5, 8))";

		// Copied from TOPGIST_DDX, quality condition 8782423 (TLMQA-695):
		private const string _stromtrasseConstraint =
			"(G1.NETZEBENE = 1 AND G2.NETZEBENE IN (3,5,7)) OR (G1.NETZEBENE = 3 AND G2.NETZEBENE IN (1,5,7)) OR (G1.NETZEBENE = 5 AND G2.NETZEBENE IN (1,3,7)) OR (G1.NETZEBENE = 7 AND G2.NETZEBENE IN (1,3,5))";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void StrassenrouteIssuesDoNotDependOnTiles()
		{
			// 20 x 20 km around Bern
			IEnvelope area = GeometryFactory.CreateEnvelope(2590000, 1190000, 2610000, 1210000);

			AssertSameIssuesWithDifferentTiles(
				area, 5000, "TOPGIS_TLM.TLM_STRASSE", "TOPGIS_TLM.TLM_STRASSENROUTE",
				"TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE", "TLM_STRASSE_UUID",
				"TLM_STRASSENROUTE_UUID", JoinType.InnerJoin, _strassenrouteConstraint);
		}

		[Test]
		public void StromtrasseIssuesDoNotDependOnTiles()
		{
			// The whole of Switzerland
			IEnvelope area = GeometryFactory.CreateEnvelope(2480000, 1070000, 2840000, 1300000);

			AssertSameIssuesWithDifferentTiles(
				area, 20000, "TOPGIS_TLM.TLM_LEITUNG", "TOPGIS_TLM.TLM_STROMTRASSE",
				"TOPGIS_TLM.TLM_LEITUNG_STROMTRASSE", "TLM_LEITUNG_UUID",
				"TLM_STROMTRASSE_UUID", JoinType.LeftJoin, _stromtrasseConstraint);
		}

		private static void AssertSameIssuesWithDifferentTiles(
			IEnvelope area, double tileSize,
			string lineClassName, string otherTableName, string bridgeTableName,
			string bridgeLineKey, string bridgeOtherKey, JoinType joinType,
			string validDuplicateConstraint)
		{
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(_connectionFile);

			List<string> RunWithTileSize(double size, string label)
			{
				IReadOnlyFeatureClass lineClass =
					ReadOnlyTableFactory.Create(ws.OpenFeatureClass(lineClassName));
				IReadOnlyTable otherTable =
					ReadOnlyTableFactory.Create(ws.OpenTable(otherTableName));

				var bridge = new TrMakeTable(lineClass, bridgeTableName);
				((ITableTransformer) bridge).TransformerName = "Tr_MakeTable";

				var join = new TrTableJoinInMemory(lineClass, otherTable, "UUID", "UUID", joinType)
				           {
					           ManyToManyTable = bridge.GetTransformed(),
					           ManyToManyTableLeftKey = bridgeLineKey,
					           ManyToManyTableRightKey = bridgeOtherKey
				           };
				((ITableTransformer) join).TransformerName = "Tr_JoinInMemory";

				var test = new QaDuplicateGeometrySelf(
					(IReadOnlyFeatureClass) join.GetTransformed(), validDuplicateConstraint,
					false);

				var runner = new QaContainerTestRunner(size, test);

				area.SpatialReference = lineClass.SpatialReference;

				DateTime start = DateTime.Now;
				runner.Execute(area);

				Log($"{lineClassName} {label}: tile size {size}: {runner.Errors.Count} issues " +
				    $"in {DateTime.Now - start}");

				return runner.Errors.Select(Describe).OrderBy(s => s).ToList();
			}

			List<string> issuesA = RunWithTileSize(tileSize, "A");
			// Same area, other tile boundaries:
			List<string> issuesB = RunWithTileSize(tileSize * 0.73, "B");

			foreach (string issue in issuesA.Except(issuesB)) Log($"only A: {issue}");
			foreach (string issue in issuesB.Except(issuesA)) Log($"only B: {issue}");

			Assert.AreEqual(issuesA.Count, issuesB.Count,
			                $"Different issue counts, see {_logFile}");
			CollectionAssert.AreEquivalent(issuesA, issuesB);
		}

		private static string Describe(QaError error)
		{
			string rows = string.Join(
				";", error.InvolvedRows.Select(r => $"{r.TableName}:{r.OID}").OrderBy(s => s));
			return $"{rows} | {error.Description}";
		}

		private static void Log(string line)
		{
			File.AppendAllText(_logFile, line + Environment.NewLine);
		}
	}
}
