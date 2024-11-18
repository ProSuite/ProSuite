using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrProjectDefinition : TrGeometryTransformDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public int TargetSpatialReferenceId { get; }

		[DocTr(nameof(DocTrStrings.TrProject_0))]
		public TrProjectDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrProject_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[DocTr(nameof(DocTrStrings.TrProject_targetSpatialReferenceId))]
			int targetSpatialReferenceId)
			: base(featureClass ?? throw new ArgumentNullException(nameof(featureClass),
				       "FeatureClass cannot be null"), featureClass.ShapeType)
		{
			if (featureClass == null)
			{
				throw new ArgumentNullException(nameof(featureClass),
				                                "FeatureClass cannot be null");
			}

			FeatureClass = featureClass;
			TargetSpatialReferenceId = targetSpatialReferenceId;
		}
	}
}
