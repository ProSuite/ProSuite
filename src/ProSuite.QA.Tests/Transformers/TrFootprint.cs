using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry.CreateFootprint;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrFootprint : TrGeometryTransform
	{
		private const double _defaultToleranceValue = -1;

		[DocTr(nameof(DocTrStrings.TrFootprint_0))]
		public TrFootprint(
			[NotNull] [DocTr(nameof(DocTrStrings.TrFootprint_multipatchClass))]
			IReadOnlyFeatureClass multipatchClass)
			: base(multipatchClass, esriGeometryType.esriGeometryPolygon) { }

		[InternallyUsedTest]
		public TrFootprint(
			[NotNull] TrFootprintDefinition definition)
			: this((IReadOnlyFeatureClass) definition.MultipatchClass)
		{
			Tolerance = definition.Tolerance;
		}

		[TestParameter(_defaultToleranceValue)]
		[DocTr(nameof(DocTrStrings.TrFootprint_Tolerance))]
		public double Tolerance { get; set; }

		protected override IEnumerable<GdbFeature> Transform(IGeometry source,
		                                                     long? sourceOid)
		{
			IMultiPatch multipatch = (IMultiPatch) source;

			TransformedFeatureClass transformedClass = GetTransformed();

			GdbFeature feature = sourceOid == null
				                     ? CreateFeature()
				                     : (GdbFeature) transformedClass.CreateObject(sourceOid.Value);

			double? tolerance = Tolerance < 0 ? (double?) null : Tolerance;

			IPolygon result = CreateFootprintUtils.GetFootprint(multipatch, tolerance);

			feature.Shape = result;

			yield return feature;
		}
	}
}
