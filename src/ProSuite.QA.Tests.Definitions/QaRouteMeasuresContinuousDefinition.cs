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
	public class QaRouteMeasuresContinuousDefinition : AlgorithmDefinition
	{
		public ICollection<IFeatureClassSchemaDef> PolylineClasses { get; }
		public IEnumerable<string> RouteIdFields { get; }

		[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_0))]
		public QaRouteMeasuresContinuousDefinition(
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_polylineClass))] [NotNull]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_routeIdField))] [NotNull]
			string routeIdField)
			: this(new[] { polylineClass }, new[] { routeIdField }) { }

		[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_1))]
		public QaRouteMeasuresContinuousDefinition(
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_polylineClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef>
				polylineClasses,
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_routeIdFields))] [NotNull]
			IEnumerable<string>
				routeIdFields)
			: base(polylineClasses)
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentNotNull(routeIdFields, nameof(routeIdFields));

			PolylineClasses = new List<IFeatureClassSchemaDef>(polylineClasses);
			RouteIdFields = routeIdFields;
		}
	}
}
