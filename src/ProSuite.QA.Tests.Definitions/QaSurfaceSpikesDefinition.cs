using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[TerrainTest]
	[NotNull]
	[UsedImplicitly]
	public class QaSurfaceSpikesDefinition : AlgorithmDefinition
	{
		public ITerrainDef Terrain { get; }
		public double TerrainTolerance { get; }
		public double MaxSlopeDegrees { get; }
		public double MaxDeltaZ { get; }

		[Doc(nameof(DocStrings.QaTerrainSpikes_0))]
		public QaSurfaceSpikesDefinition(
			[Doc(nameof(DocStrings.QaTerrainSpikes_terrain))] [NotNull]
			ITerrainDef terrain,
			[Doc(nameof(DocStrings.QaTerrainSpikes_terrainTolerance))]
			double terrainTolerance,
			[Doc(nameof(DocStrings.QaTerrainSpikes_maxSlopeDegrees))]
			double maxSlopeDegrees,
			[Doc(nameof(DocStrings.QaTerrainSpikes_maxDeltaZ))]
			double maxDeltaZ)
			: base(new ITableSchemaDef[] { })
		{
			Terrain = terrain;
			TerrainTolerance = terrainTolerance;
			MaxSlopeDegrees = maxSlopeDegrees;
			MaxDeltaZ = maxDeltaZ;
		}
	}
}
