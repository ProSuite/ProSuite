using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrGetNodesDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef LineClass { get; }

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IFeatureClassSchemaDef _toDissolve;

		[DocTr(nameof(DocTrStrings.TrGetNodes_0))]
		public TrGetNodesDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrGetNodes_lineClass))]
			IFeatureClassSchemaDef lineClass)
			: base(new List<ITableSchemaDef> { lineClass })
		{
			_toDissolve = lineClass;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrGetNodes_Attributes))]
		public IList<string> Attributes { get; set; }
	}
}
