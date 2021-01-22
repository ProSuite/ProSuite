using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Tests.Documentation;

// ReSharper disable CheckNamespace
namespace ProSuite.QA.Tests
	// ReSharper restore CheckNamespace
{
	[Obsolete("Replaced by QaSimpleGeometry")]
	[CLSCompliant(false)]
	public class QaInvalidGeometry : QaSimpleGeometry
	{
		[Obsolete("Replaced by QaSimpleGeometry")]
		[Doc("QaSimpleGeometry_0")]
		public QaInvalidGeometry(
			[Doc("QaSimpleGeometry_featureClass")] IFeatureClass featureClass)
			: base(featureClass) { }

		[Obsolete("Replaced by QaSimpleGeometry")]
		[Doc("QaSimpleGeometry_1")]
		public QaInvalidGeometry(
			[Doc("QaSimpleGeometry_featureClass")] IFeatureClass featureClass,
			[Doc("QaSimpleGeometry_allowNonPlanarLines")]
			bool allowNonPlanarLines)
			: base(featureClass, allowNonPlanarLines) { }
	}
}
