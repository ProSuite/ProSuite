using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests.Transformers
{
	public class TrZAssign : TrGeometryTransform
	{
		public enum AssignOption
		{
			Tile,
			All
		}

		private const AssignOption _defaultZAssignOption = AssignOption.Tile;
		private readonly RasterReference _raster;
		private readonly esriGeometryType _shapeType;

		private ISimpleSurface _searchedSurface;

		public TrZAssign(IReadOnlyFeatureClass featureClass, RasterReference raster)
			: base(featureClass, featureClass.ShapeType)
		{
			_shapeType = featureClass.ShapeType;
			_raster = raster;
		}

		[TestParameter(_defaultZAssignOption)]
		public AssignOption ZAssignOption { get; set; } = _defaultZAssignOption;

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid)
		{
			AssignedFc transformedClass = (AssignedFc)GetTransformed();
			GdbFeature feature = sourceOid == null
									 ? CreateFeature()
									 : (GdbFeature)transformedClass.CreateObject(sourceOid.Value);

			IGeometry transformed;
			if (_searchedSurface != null)
			{
				transformed = _searchedSurface.Drape(source);
			}
			else
			{
				transformed = GeometryFactory.Clone(source);
				((IZAware)transformed).ZAware = true;
				((IZ)transformed).SetConstantZ(0);
			}

			feature.Shape = transformed;
			yield return feature;
		}

		private void SetSearchedExtent(IEnvelope extent)
		{
			_searchedSurface = _raster.CreateSurface(extent);
		}

		private void ClearSearchedExtent()
		{
			_searchedSurface = null;
		}

		protected override TransformedFc InitTransformedFc(IReadOnlyFeatureClass fc, string name)
		{
			AssignedFc transformed = new AssignedFc(fc, this, name);
			return transformed;
		}

		private class AssignedFc : TransformedFc
		{
			public AssignedFc(IReadOnlyFeatureClass fc, TrZAssign transformer, string name)
				: base(fc, fc.ShapeType,
					   (t) =>
					   {
						   var ds = new AssigedDataset((AssignedFc)t, fc);
						   return ds;
					   },
					   transformer, name)
			{
				AddStandardFields(fc);
			}

			protected override IField CreateShapeField(IReadOnlyFeatureClass involvedFc)
			{
				IGeometryDef geomDef =
					involvedFc.Fields.Field[
						involvedFc.Fields.FindField(involvedFc.ShapeFieldName)].GeometryDef;

				return FieldUtils.CreateShapeField(
					involvedFc.ShapeType,
					involvedFc.SpatialReference,
					geomDef.GridSize[0], hasZ: true, geomDef.HasM);
			}
		}

		private class AssigedDataset : TransformedDataset
		{
			public AssigedDataset(
				[NotNull] AssignedFc tfc,
				[NotNull] IReadOnlyFeatureClass t0)
				: base(tfc, t0)
			{ }

			public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
			{
				TrZAssign tr = (TrZAssign)Resulting.Transformer;

				if (filter is IFeatureClassFilter sf)
				{
					tr.SetSearchedExtent(sf.FilterGeometry.Envelope);
				}

				foreach (var searched in base.Search(filter, recycling))
				{
					yield return searched;
				}

				tr.ClearSearchedExtent();
			}
		}
	}
}
