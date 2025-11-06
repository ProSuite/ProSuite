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
		[NotNull]
		public IFeatureClassSchemaDef FeatureClass { get; }

		public ProSuiteGeometryType DerivedShapeType { get; }
		public ISpatialReferenceDef DerivedSpatialReference { get; }

		protected TrGeometryTransformDefinition([NotNull] IFeatureClassSchemaDef featureClass,
		                                        ProSuiteGeometryType derivedShapeType,
		                                        ISpatialReferenceDef derivedSpatialReference = null)
			: base(new List<ITableSchemaDef> { featureClass })
		{
			FeatureClass = featureClass;
			DerivedShapeType = derivedShapeType;
			DerivedSpatialReference = derivedSpatialReference;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrGeometryTransform_Attributes))]
		public IList<string> Attributes { get; set; }
	}
}
