using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	[IntersectionParameterTest]
	public class QaMinIntersectDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PolygonClasses { get; set; }

		public double Limit { get; }

		private readonly double _limit;

		[CanBeNull] private static TestIssueCodes _codes;

		[Doc(nameof(DocStrings.QaMinIntersect_0))]
		public QaMinIntersectDefinition(
			[Doc(nameof(DocStrings.QaMinIntersect_polygonClasses))]
			IList<IFeatureClassSchemaDef> polygonClasses,
			[Doc(nameof(DocStrings.QaMinIntersect_limit))]
			double limit)
			: base(polygonClasses)
		{
			_limit = limit;
			PolygonClasses = polygonClasses;
			Limit = limit;
		}

		[Doc(nameof(DocStrings.QaMinIntersect_1))]
		public QaMinIntersectDefinition(
			[Doc(nameof(DocStrings.QaMinIntersect_polygonClass))]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaMinIntersect_limit))]
			double limit)
			: this(new[] { polygonClass }, limit) { }
	}
}
