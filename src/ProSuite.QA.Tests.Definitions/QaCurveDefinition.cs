using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports non-linear polycurve segments as errors
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaCurveDefinition : AlgorithmDefinition
	{

		[Doc(nameof(DocStrings.QaCurve_0))]
		public QaCurveDefinition(
			[Doc(nameof(DocStrings.QaCurve_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			//	_shapeFieldName = featureClass.ShapeFieldName;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaCurve_AllowedNonLinearSegmentTypes))]
		public IList<NonLinearSegmentType> AllowedNonLinearSegmentTypes { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaCurve_GroupIssuesBySegmentType))]
		public bool GroupIssuesBySegmentType { get; set; }
		public IFeatureClassSchemaDef FeatureClass { get; private set; }
	//	public object AllowedNonLinearSegmentTypes { get; set; }
	}
}
