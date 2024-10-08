using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrProject : TrGeometryTransform
	{
		private readonly ISpatialReference _targetSpatialReference;

		public TrProject(IReadOnlyFeatureClass featureClass, int targetSpatialReferenceId)
			: this(featureClass, SpatialReferenceUtils.CreateSpatialReference(targetSpatialReferenceId))
		{ }
		private TrProject(IReadOnlyFeatureClass featureClass, ISpatialReference targetSpatialReference)
			: base(featureClass, featureClass.ShapeType, targetSpatialReference)
		{
			_targetSpatialReference = targetSpatialReference;
		}

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid)
		{
			TransformedFeatureClass transformedClass = GetTransformed();
			GdbFeature feature = sourceOid == null
									 ? CreateFeature()
									 : (GdbFeature)transformedClass.CreateObject(sourceOid.Value);

			IGeometry target = SpatialReferenceUtils.ProjectEx(source, _targetSpatialReference);

			feature.Shape = target;
			yield return feature;
		}

		protected override TransformedFc InitTransformedFc(IReadOnlyFeatureClass fc, string name)
		{
			ProjectedFc transformed = new ProjectedFc(fc, this, name);
			return transformed;
		}

		protected class ProjectedFc : TransformedFc, IHasGeotransformation
		{
			private readonly TrProject _transformer;
			public ProjectedFc(IReadOnlyFeatureClass fc, TrProject transformer, string name)
				: base(fc, fc.ShapeType,
				       (t) =>
				       {
					       var ds = new ProjectedDataset((ProjectedFc) t, fc);
					       t.SpatialReference = transformer._targetSpatialReference;
					       return ds;
				       },
				       transformer, name)
			{
				_transformer = transformer;
				AddStandardFields(fc);
			}

			public new TrProject Transformer => _transformer;


			public override IEnvelope Extent
			{
				get
				{
					IEnvelope extent = GeometryFactory.Clone(base.Extent);
					extent.Project(_transformer._targetSpatialReference);
					return extent;
				}
			}

			public T ProjectEx<T>([NotNull] T geometry) where T : IGeometry
			{
				ISpatialReference targetSpatialReference = null;
				if (geometry.SpatialReference?.FactoryCode ==
				    _transformer._targetSpatialReference.FactoryCode)
				{
					IReadOnlyFeatureClass sourceFc = (IReadOnlyFeatureClass) InvolvedTables[0];
					targetSpatialReference = sourceFc.SpatialReference;
				}

				if (targetSpatialReference == null)
				{
					throw new InvalidOperationException(
						$"unhandles spatial reference {geometry.SpatialReference?.FactoryCode}");
				}

				return SpatialReferenceUtils.ProjectEx(
					geometry, targetSpatialReference);
			}

			protected override IField CreateShapeField(IReadOnlyFeatureClass involvedFc)
			{
				IGeometryDef geomDef =
					involvedFc.Fields.Field[
						involvedFc.Fields.FindField(involvedFc.ShapeFieldName)].GeometryDef;

				return FieldUtils.CreateShapeField(
					involvedFc.ShapeType,
					_transformer._targetSpatialReference,
					geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM);
			}
		}

		protected class ProjectedDataset : TransformedDataset
		{

			private readonly ProjectedFc _projectedFc;

			public ProjectedDataset([NotNull] ProjectedFc projectedFc,
			                        [NotNull] IReadOnlyFeatureClass t0) : base(projectedFc, t0)
			{
				_projectedFc = projectedFc;
			}
		}
	}
}
