using System;
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
	[GeometryTest]
	[Obsolete("Use QaMpFootprintHoles")]
	public class QaMpFootprintHasInnerRing : QaMpFootprintHoles
	{
		[Doc("QaMpFootprintHoles_0")]
		public QaMpFootprintHasInnerRing(
			[Doc("QaMpFootprintHoles_multiPatchClass")] [NotNull]
			IFeatureClass
				multiPatchClass,
			[Doc("QaMpFootprintHoles_innerRingHandling")]
			InnerRingHandling
				innerRingHandling)
			: base(multiPatchClass, innerRingHandling) { }
	}
}
