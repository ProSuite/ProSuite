using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Tests.Documentation;

// ReSharper disable CheckNamespace
namespace ProSuite.QA.Tests
	// ReSharper restore CheckNamespace
{
	[Obsolete("Replaced by QaSimpleGeometry")]
	public class QaInvalidGeometry : QaSimpleGeometry
	{
		[Obsolete("Replaced by QaSimpleGeometry")]
		[Doc(nameof(DocStrings.QaSimpleGeometry_0))]
		public QaInvalidGeometry(
			[Doc(nameof(DocStrings.QaSimpleGeometry_featureClass))] IFeatureClass featureClass)
			: base(featureClass) { }

		[Obsolete("Replaced by QaSimpleGeometry")]
		[Doc(nameof(DocStrings.QaSimpleGeometry_1))]
		public QaInvalidGeometry(
			[Doc(nameof(DocStrings.QaSimpleGeometry_featureClass))] IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSimpleGeometry_allowNonPlanarLines))]
			bool allowNonPlanarLines)
			: base(featureClass, allowNonPlanarLines) { }
	}
}
