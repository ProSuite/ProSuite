using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if the lakes and islands are pointing in the right direction
	/// This test should be run after an intersecting lines test
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	[PolygonNetworkTest]
	public class QaBorderSenseDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PolylineClasses { get; }
		public bool Clockwise { get; }

		[Doc(nameof(DocStrings.QaBorderSense_0))]
		public QaBorderSenseDefinition(
			[Doc(nameof(DocStrings.QaBorderSense_polylineClass))]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaBorderSense_clockwise))]
			bool clockwise)
			: this(new[] { polylineClass }, clockwise) { }

		[Doc(nameof(DocStrings.QaBorderSense_1))]
		public QaBorderSenseDefinition(
			[Doc(nameof(DocStrings.QaBorderSense_polylineClasses))]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaBorderSense_clockwise))]
			bool clockwise)
			: base(polylineClasses)
		{
			PolylineClasses = polylineClasses;
			Clockwise = clockwise;
		}
	}
}
