using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Finds large polygons or polygon parts
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMaxAreaDefinition : AlgorithmDefinition
	{
		public IReadOnlyFeatureClass PolygonClass { get; set; }
		public double Limit { get; set; }
		public bool Perpart { get; set; }

		[Doc(nameof(DocStrings.QaMaxArea_0))]
		public QaMaxAreaDefinition(
				[Doc(nameof(DocStrings.QaMaxArea_polygonClass))]
				IReadOnlyFeatureClass polygonClass,
				[Doc(nameof(DocStrings.QaMaxArea_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, limit, false) { }

		[Doc(nameof(DocStrings.QaMaxArea_1))]
		public QaMaxAreaDefinition(
			[Doc(nameof(DocStrings.QaMaxArea_polygonClass))]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaMaxArea_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMaxArea_perPart))]
			bool perPart)
			: base((ITableSchemaDef) polygonClass)
		{
			PolygonClass = polygonClass;
			Limit = limit;
			Perpart = perPart;
		}
	}
}
