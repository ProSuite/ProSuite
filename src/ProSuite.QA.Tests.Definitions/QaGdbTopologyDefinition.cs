using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaGdbTopologyDefinition : AlgorithmDefinition
	{
		[CanBeNull]
		public ITopologyDef Topology { get; }

		[NotNull]
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; } =
			new List<IFeatureClassSchemaDef>();

		[Doc(nameof(DocStrings.QaGdbTopology_0))]
		public QaGdbTopologyDefinition(
			[Doc(nameof(DocStrings.QaGdbTopology_topology))] [NotNull]
			ITopologyDef topology)
			: base(new List<ITableSchemaDef>())
		{
			Topology = topology;
		}

		[Doc(nameof(DocStrings.QaGdbTopology_1))]
		public QaGdbTopologyDefinition(
			[Doc(nameof(DocStrings.QaGdbTopology_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
		}
	}
}
