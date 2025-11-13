using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrDissolveDefinition : AlgorithmDefinition
	{

		public IFeatureClassSchemaDef FeatureClass { get; }

		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IFeatureClassSchemaDef _toDissolve;

		[DocTr(nameof(DocTrStrings.TrDissolve_0))]
		public TrDissolveDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrDissolve_featureClass))]
			IFeatureClassSchemaDef featureClass)
			: base(new List<ITableSchemaDef> { featureClass })
		{
			FeatureClass = featureClass;
			NeighborSearchOption = _defaultSearchOption;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_SearchDistance))]
		public new double Search { get; set; }

		[TestParameter(_defaultSearchOption)]
		[DocTr(nameof(DocTrStrings.TrDissolve_NeighborSearchOption))]
		public SearchOption NeighborSearchOption { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_Attributes))]
		public IList<string> Attributes { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_GroupBy))]
		public IList<string> GroupBy { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_Constraint))]
		public string Constraint { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_CreateMultipartFeatures))]
		public bool CreateMultipartFeatures { get; set; }
	}
}
