using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class
		TrSpatialJoinDefinition : AlgorithmDefinition //TrGeometryTransformDefinition
	{
		public IFeatureClassSchemaDef T0 { get; }
		public IFeatureClassSchemaDef T1 { get; }

		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		[DocTr(nameof(DocTrStrings.TrSpatialJoin_0))]
		public TrSpatialJoinDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrSpatialJoin_t0))]
			IFeatureClassSchemaDef t0,
			[NotNull] [DocTr(nameof(DocTrStrings.TrSpatialJoin_t1))]
			IFeatureClassSchemaDef t1)
			: base(new List<ITableSchemaDef> { t0, t1 })
		{
			T0 = t0;
			T1 = t1;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_Constraint))]
		public string Constraint { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_OuterJoin))]
		public bool OuterJoin { get; set; }

		[TestParameter(_defaultSearchOption)]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_NeighborSearchOption))]
		public SearchOption NeighborSearchOption { get; set; }

		// Remark: Grouped must come in Code before T1Attributes !
		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_Grouped))]
		public bool Grouped { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_T0Attributes))]
		public IList<string> T0Attributes { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_T1Attributes))]
		public IList<string> T1Attributes { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_T1CalcAttributes))]
		public IList<string> T1CalcAttributes { get; set; }
	}
}
