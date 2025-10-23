using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Finds small polygons or polygon parts
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinAreaDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PolygonClass { get; }
		public double Limit { get; }
		public bool PerPart { get; }

		[Doc(nameof(DocStrings.QaMinArea_0))]
		public QaMinAreaDefinition(
				[Doc(nameof(DocStrings.QaMinArea_polygonClass))]
				IFeatureClassSchemaDef polygonClass,
				[Doc(nameof(DocStrings.QaMinArea_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, limit, false) { }

		[Doc(nameof(DocStrings.QaMinArea_1))]
		public QaMinAreaDefinition(
			[Doc(nameof(DocStrings.QaMinArea_polygonClass))]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaMinArea_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinArea_perPart))]
			bool perPart)
			: base(polygonClass)
		{
			PolygonClass = polygonClass;
			Limit = limit;
			PerPart = perPart;
		}
	}
}
