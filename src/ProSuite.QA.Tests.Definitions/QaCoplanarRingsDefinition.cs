using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using System.Collections.Generic;
using System;
using ProSuite.Commons.Geom;
using System.Linq;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports non-linear polycurve segments as errors
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaCoplanarRingsDefinition : AlgorithmDefinition
	{
		// Define FeatureClasses property to access the feature classes
		//public IEnumerable<IFeatureClassSchemaDef> FeatureClasses { get; }
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double CoplanarityTolerance { get; } 
		public bool IncludeAssociatedParts { get; } 

		//[UsedImplicitly]
		[Doc(nameof(DocStrings.QaCoplanarRings_0))]
		public QaCoplanarRingsDefinition(
			[Doc(nameof(DocStrings.QaCoplanarRings_featureClass))] //[NotNull]
			//IEnumerable<IFeatureClassSchemaDef> featureClasses,
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaCoplanarRings_coplanarityTolerance))]
			double coplanarityTolerance,
			[Doc(nameof(DocStrings.QaCoplanarRings_includeAssociatedParts))]
			bool includeAssociatedParts)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			CoplanarityTolerance = coplanarityTolerance;
			IncludeAssociatedParts = includeAssociatedParts;
		}
	}
}
