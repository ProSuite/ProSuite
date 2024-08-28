using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrIntersectDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef Intersected { get; }

		public IFeatureClassSchemaDef Intersecting { get; }

		[DocTr(nameof(DocTrStrings.TrIntersect_0))]
		public TrIntersectDefinition(
			[NotNull, DocTr(nameof(DocTrStrings.TrIntersect_intersected))]
			IFeatureClassSchemaDef intersected,
			[NotNull, DocTr(nameof(DocTrStrings.TrIntersect_intersecting))]
			IFeatureClassSchemaDef intersecting)
			: base(new List<ITableSchemaDef> { intersected, intersecting })
		{
			Intersected = intersecting;
			Intersecting = intersected;
		}
	}
}
