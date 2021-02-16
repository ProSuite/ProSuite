using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.Commons.Essentials.CodeAnnotations;

// ReSharper disable CheckNamespace

namespace ProSuite.QA.Tests
	// ReSharper restore CheckNamespace
{
	[UsedImplicitly]
	[GeometryTest]
	[Obsolete("Use QaMpFootprintHoles")]
	public class QaMpFootprintHasInnerRing : QaMpFootprintHoles
	{
		[Doc(nameof(DocStrings.QaMpFootprintHoles_0))]
		public QaMpFootprintHasInnerRing(
			[Doc(nameof(DocStrings.QaMpFootprintHoles_multiPatchClass))] [NotNull]
			IFeatureClass
				multiPatchClass,
			[Doc(nameof(DocStrings.QaMpFootprintHoles_innerRingHandling))]
			InnerRingHandling
				innerRingHandling)
			: base(multiPatchClass, innerRingHandling) { }
	}
}
