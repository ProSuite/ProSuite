using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using System.Collections.Generic;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrProject : TrGeometryTransform
	{
		private readonly ISpatialReference _targetSpatialReference;

		public TrProject(IReadOnlyFeatureClass featureClass, int targetSpatialReferenceId)
			: base(featureClass, featureClass.ShapeType)
		{
			_targetSpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(targetSpatialReferenceId);
		}

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid)
		{
			TransformedFeatureClass transformedClass = GetTransformed();
			GdbFeature feature = sourceOid == null
				                     ? CreateFeature()
				                     : (GdbFeature) transformedClass.CreateObject(sourceOid.Value);
			;
			IGeometry target = (IGeometry) ((IClone) source).Clone();

			target.Project(_targetSpatialReference);
			feature.Shape = target;
			yield return feature;
		}
	}
}
