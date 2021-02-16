using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.Commons.Essentials.CodeAnnotations;

// ReSharper disable CheckNamespace
namespace ProSuite.QA.Tests
	// ReSharper restore CheckNamespace
{
	[UsedImplicitly]
	[Obsolete("Use QaRouteMeasuresContinuous")]
	[LinearNetworkTest]
	[MValuesTest]
	public class QaRouteMeasures : QaRouteMeasuresContinuous
	{
		[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_0))]
		public QaRouteMeasures(
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_polylineClass))] [NotNull]
			IFeatureClass
				polylineClass,
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_routeIdField))] [NotNull]
			string routeIdField)
			: base(polylineClass, routeIdField) { }

		[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_1))]
		public QaRouteMeasures(
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_polylineClasses))] [NotNull]
			ICollection<IFeatureClass> polylineClasses,
			[Doc(nameof(DocStrings.QaRouteMeasuresContinuous_routeIdFields))] [NotNull]
			IEnumerable<string>
				routeIdFields)
			: base(polylineClasses, routeIdFields) { }
	}
}
