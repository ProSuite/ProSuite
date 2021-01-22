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
	[CLSCompliant(false)]
	[UsedImplicitly]
	[Obsolete("Use QaRouteMeasuresContinuous")]
	[LinearNetworkTest]
	[MValuesTest]
	public class QaRouteMeasures : QaRouteMeasuresContinuous
	{
		[Doc("QaRouteMeasuresContinuous_0")]
		public QaRouteMeasures(
			[Doc("QaRouteMeasuresContinuous_polylineClass")] [NotNull]
			IFeatureClass
				polylineClass,
			[Doc("QaRouteMeasuresContinuous_routeIdField")] [NotNull]
			string routeIdField)
			: base(polylineClass, routeIdField) { }

		[Doc("QaRouteMeasuresContinuous_1")]
		public QaRouteMeasures(
			[Doc("QaRouteMeasuresContinuous_polylineClasses")] [NotNull]
			ICollection<IFeatureClass> polylineClasses,
			[Doc("QaRouteMeasuresContinuous_routeIdFields")] [NotNull]
			IEnumerable<string>
				routeIdFields)
			: base(polylineClasses, routeIdFields) { }
	}
}
