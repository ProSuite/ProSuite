using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public abstract class TrGeometryTransformDefinition : AlgorithmDefinition
	{
		protected TrGeometryTransformDefinition([NotNull] IFeatureClassSchemaDef fc,
		                                        ProSuiteGeometryType derivedShapeType,
		                                        ISpatialReferenceDef derivedSpatialReference = null)
			: base(new List<ITableSchemaDef> { fc })
		{
			GeometryType = derivedShapeType;
		}

		public ProSuiteGeometryType GeometryType { get; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrGeometryTransform_Attributes))]
		public IList<string> Attributes { get; set; }
	}
}
