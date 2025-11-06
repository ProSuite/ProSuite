using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers
{
	[GeometryTransformer]
	[ZValuesTest]
	public class TrZAssign : TrGeometryTransform
	{
		private const AssignOption _defaultZAssignOption = AssignOption.Tile;
		private readonly RasterReference _raster;

		private ISimpleSurface _searchedSurface;
		private bool _surfaceToDispose;

		private ISimpleSurface _domainSurface;
		private IPolygon _searchedDomain;

		private readonly ArrayProvider<WKSPointZ> _wksPointArrayProvider =
			new ArrayProvider<WKSPointZ>();

		[DocTr(nameof(DocTrStrings.TrZAssign_0))]
		public TrZAssign(
			[DocTr(nameof(DocTrStrings.TrZAssign_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[DocTr(nameof(DocTrStrings.TrZAssign_raster))]
			RasterDatasetReference raster)
			: base(featureClass, featureClass.ShapeType)
		{
			_raster = raster;
		}

		[DocTr(nameof(DocTrStrings.TrZAssign_0))]
		public TrZAssign(
			[DocTr(nameof(DocTrStrings.TrZAssign_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[DocTr(nameof(DocTrStrings.TrZAssign_rasterMosaic))]
			MosaicRasterReference rasterMosaic)
			: base(featureClass, featureClass.ShapeType)
		{
			_raster = rasterMosaic;
		}

		[InternallyUsedTest]
		public TrZAssign(
			[NotNull] TrZAssignDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       (RasterDatasetReference) definition.Raster)
		{
			ZAssignOption = (AssignOption) definition.ZAssignOption;
		}

		[TestParameter(_defaultZAssignOption)]
		[DocTr(nameof(DocTrStrings.TrZAssign_ZAssignOption))]
		public AssignOption ZAssignOption { get; set; } = _defaultZAssignOption;

		private IRelationalOperator SearchedDomain
		{
			get
			{
				if (_domainSurface != _searchedSurface)
				{
					_searchedDomain = null;
				}

				if (_searchedDomain == null && _searchedSurface != null)
				{
					_searchedDomain = _searchedSurface.GetDomain();
					_domainSurface = _searchedSurface;
				}

				return (IRelationalOperator) _searchedDomain;
			}
		}

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid)
		{
			AssignedFc transformedClass = (AssignedFc) GetTransformed();

			IGeometry transformed = GeometryFactory.Clone(source);
			((IZAware) transformed).ZAware = true;
			if (transformed is IPoint p)
			{
				p.Z = double.NaN;
			}
			else if (transformed is IZ iz)
			{
				iz.SetConstantZ(double.NaN);
			}
			else if (transformed is IPointCollection4 pointCollection)
			{
				int pointCount = pointCollection.PointCount;
				WKSPointZ[] wksPointZs = _wksPointArrayProvider.GetArray(pointCount);
				GeometryUtils.QueryWKSPointZs(pointCollection, wksPointZs, 0, pointCount);
				for (int i = 0; i < pointCount; i++)
				{
					WKSPointZ pt = wksPointZs[i];
					pt.Z = double.NaN;
					wksPointZs[i] = pt;
				}

				GeometryUtils.SetWKSPointZs(pointCollection, wksPointZs, pointCount);
			}
			else
			{
				throw new NotImplementedException(
					$"Unhandled geometry type '{transformed.GeometryType}'");
			}

			GdbFeature feature = ZAssignOption == AssignOption.All && sourceOid != null
				                     ? (GdbFeature) transformedClass.CreateObject(sourceOid.Value)
				                     : CreateFeature();

			if (_searchedSurface != null)
			{
				transformed = _searchedSurface.SetShapeVerticesZ(transformed);
			}

			feature.Shape = transformed;
			yield return feature;
		}

		private void SetSearchedExtent(IEnvelope extent, [CanBeNull] IDataContainer dataContainer)
		{
			_searchedSurface = null;
			_surfaceToDispose = false;
			_searchedSurface = dataContainer?.GetSimpleSurface(
				_raster, extent, unassignedZValueHandling: UnassignedZValueHandling.IgnoreVertex);

			if (_searchedSurface == null)
			{
				_searchedSurface =
					_raster.CreateSurface(
						extent, unassignedZValueHandling: UnassignedZValueHandling.IgnoreVertex);
				_surfaceToDispose = true;
			}
		}

		private void ClearSearchedExtent()
		{
			if (_surfaceToDispose)
			{
				_searchedSurface.Dispose();
			}

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
					       var ds = new AssigedDataset((AssignedFc) t, fc);
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
				: base(tfc, t0) { }

			public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
			{
				TrZAssign tr = (TrZAssign) Resulting.Transformer;

				if (filter is IFeatureClassFilter sf)
				{
					tr.SetSearchedExtent(sf.FilterGeometry.Envelope, Resulting.DataContainer);
				}

				foreach (var searched in base.Search(filter, recycling))
				{
					yield return searched;
				}

				tr.ClearSearchedExtent();
			}

			protected override void CompleteRawFeatures(IList<TransformedFeature> rawFeatures)
			{
				TrZAssign tr = (TrZAssign) Resulting.Transformer;
				if (tr.ZAssignOption == AssignOption.Tile)
				{
					return;
				}

				Dictionary<Tile, IList<TransformedFeature>> tileFeatures =
					new Dictionary<Tile, IList<TransformedFeature>>(new Tile.TileComparer());

				foreach (TransformedFeature f in rawFeatures)
				{
					if (tr.SearchedDomain.Contains(f.Shape))
					{
						continue;
					}

					foreach (var tile in Resulting.DataContainer.EnumInvolvedTiles(f.Shape))
					{
						if (! tileFeatures.TryGetValue(
							    tile, out IList<TransformedFeature> features))
						{
							features = new List<TransformedFeature>();
							tileFeatures.Add(tile, features);
						}

						features.Add(f);
					}
				}

				foreach (var pair in tileFeatures)
				{
					Tile tile = pair.Key;

					tile.FilterEnvelope.SpatialReference =
						((IGeometry) tr.SearchedDomain).SpatialReference;

					if (GeometryUtils.InteriorIntersects(((IGeometry) tr.SearchedDomain).Envelope,
					                                     tile.FilterEnvelope))
					{
						continue;
					}

					IList<TransformedFeature> features = pair.Value;

					ISimpleSurface surface = Resulting.DataContainer.GetSimpleSurface(
						tr._raster, tile.FilterEnvelope,
						unassignedZValueHandling: UnassignedZValueHandling.IgnoreVertex);

					foreach (var feature in features)
					{
						feature.Shape = surface.SetShapeVerticesZ(feature.Shape);
					}
				}
			}
		}
	}
}
