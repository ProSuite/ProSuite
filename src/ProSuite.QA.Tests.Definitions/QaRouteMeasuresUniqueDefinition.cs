using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[LinearNetworkTest]
	[MValuesTest]
	public class QaRouteMeasuresUniqueDefinition : AlgorithmDefinition
	{
		public ICollection<IFeatureClassSchemaDef> PolylineClasses { get; }
		public IEnumerable<string> RouteIdFields { get; }

		[Doc(nameof(DocStrings.QaRouteMeasuresUnique_0))]
		public QaRouteMeasuresUniqueDefinition(
			[Doc(nameof(DocStrings.QaRouteMeasuresUnique_polylineClass))] [NotNull]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaRouteMeasuresUnique_routeIdField))] [NotNull]
			string routeIdField)
			: this(new[] { polylineClass }, new[] { routeIdField }) { }

		[Doc(nameof(DocStrings.QaRouteMeasuresUnique_1))]
		public QaRouteMeasuresUniqueDefinition(
			[Doc(nameof(DocStrings.QaRouteMeasuresUnique_polylineClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaRouteMeasuresUnique_routeIdFields))] [NotNull]
			IEnumerable<string> routeIdFields)
			: base(polylineClasses)
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentNotNull(routeIdFields, nameof(routeIdFields));

			PolylineClasses = new List<IFeatureClassSchemaDef>(polylineClasses);
			RouteIdFields = routeIdFields;
		}
	}
}
