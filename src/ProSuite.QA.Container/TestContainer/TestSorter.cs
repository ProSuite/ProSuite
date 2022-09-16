using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	internal class TestSorter
	{
		private readonly IList<ContainerTest> _tests;
		public IDictionary<IReadOnlyTable, IList<ContainerTest>> TestsPerTable { get; }
		public IDictionary<RasterReference, IList<ContainerTest>> TestsPerRaster { get; }
		public IDictionary<TerrainReference, IList<ContainerTest>> TestsPerTerrain { get; }

		public TestSorter([NotNull] IList<ContainerTest> tests)
		{
			_tests = tests;

			TestsPerTable = TestUtils.GetTestsByTable(tests);

			TestsPerRaster = TestUtils.GetTestsByInvolvedType(
				tests, (test) => test.InvolvedRasters);

			TestsPerTerrain = TestUtils.GetTestsByInvolvedType(
				tests, (test) => test.InvolvedTerrains);
		}

		public IList<TerrainRowEnumerable> PrepareTerrains([CanBeNull] ITestProgress testProgress)
			=> PrepareTerrains(TestsPerTerrain, testProgress);

		private IList<TerrainRowEnumerable> PrepareTerrains(
			[NotNull] IDictionary<TerrainReference, IList<ContainerTest>> testsPerTerrain,
			[CanBeNull] ITestProgress testProgress)
		{
			Assert.ArgumentNotNull(testsPerTerrain, nameof(testsPerTerrain));

			var result = new List<TerrainRowEnumerable>();

			{
				foreach (TerrainReference terrainRef in testsPerTerrain.Keys)
				{
					// get the list of tests for this terrain
					IList<ContainerTest> tests = testsPerTerrain[terrainRef];
					Assert.True(tests.Count > 0, "No tests for terrain {0}", terrainRef);

					// determine the lowest terrain tolerance
					double terrainResolution = tests[0].TerrainTolerance;

					foreach (ContainerTest test in tests)
					{
						terrainResolution = Math.Min(terrainResolution, test.TerrainTolerance);
					}

					var terrainRowEnumerable =
						new TerrainRowEnumerable(terrainRef, terrainResolution, testProgress);

					result.Add(terrainRowEnumerable);
				}
			}

			return result;
		}
	}
}
