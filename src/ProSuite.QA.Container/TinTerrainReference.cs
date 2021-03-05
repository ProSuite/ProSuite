using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	[CLSCompliant(false)]
	public class TinTerrainReference : TerrainReference
	{
		[NotNull]
		public static TinTerrainReference Create([NotNull] IFeatureDataset fds)
		{
			List<IFeatureClass> sources = new List<IFeatureClass>();
			List<esriTinSurfaceType> types = new List<esriTinSurfaceType>();
			IEnumDataset subsets = fds.Subsets;
			subsets.Reset();
			for (IDataset subset = subsets.Next();
			     subset != null;
			     subset = subsets.Next())
			{
				IFeatureClass fc = subset as IFeatureClass;
				if (fc == null)
				{
					continue;
				}

				if (fc.ShapeType == esriGeometryType.esriGeometryMultipoint)
				{
					sources.Add(fc);
					types.Add(esriTinSurfaceType.esriTinMassPoint);
				}

				if (fc.ShapeType == esriGeometryType.esriGeometryPolyline)
				{
					sources.Add(fc);
					types.Add(esriTinSurfaceType.esriTinHardLine);
				}
			}

			return new TinTerrainReference(fds.Name, sources, types);
		}

		[NotNull]
		public static TinTerrainReference Create(
			[NotNull] IList<IFeatureClass> sources,
			[NotNull] List<esriTinSurfaceType> types)
		{
			Assert.AreEqual(sources.Count, types.Count,
			                $"Expected equal number of sources and types, got {sources.Count} sources and {types.Count} types");
			return new TinTerrainReference("dummyname", sources, types);
		}

		private readonly string _name;
		private readonly IList<IFeatureClass> _sources;
		private readonly IList<esriTinSurfaceType> _types;
		private RectangularTilingStructure _tiling;

		private TinTerrainReference([NotNull] string name,
		                            [NotNull] IList<IFeatureClass> sources,
		                            [NotNull] IList<esriTinSurfaceType> types)
		{
			Assert.AreEqual(sources.Count, types.Count,
			                $"Expected equal number of sources and types, got {sources.Count} sources and {types.Count} types");
			_name = name;
			_sources = sources;
			_types = types;
		}

		public override IGeoDataset Dataset => (IGeoDataset) _sources[0];

		//TODO: init correct values
		public override RectangularTilingStructure Tiling =>
			_tiling ?? (_tiling = new RectangularTilingStructure(
				            2460000, 1068000, 800, 400, BorderPointTileAllocationPolicy.BottomRight,
				            spatialReference: null));

		public override string Name => _name;

		public override ITin CreateTin([NotNull] IEnvelope extent, double resolution)
		{
			var tinClass = new TinClass();
			ITinEdit tin = tinClass;
			tin.InitNew(extent);
			ISpatialFilter filter = new SpatialFilterClass();
			filter.Geometry = extent;
			filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
			for (int iSource = 0; iSource < _sources.Count; iSource++)
			{
				IFeatureClass source = _sources[iSource];
				IFeatureCursor crs = source.Search(filter, Recycling: false);
				try
				{
					tin.AddFromFeatureCursor(crs, null, null, _types[iSource]);
				}
				finally
				{
					Marshal.ReleaseComObject(crs);
				}
			}

			return tinClass;
		}

		public override bool EqualsCore([NotNull] TerrainReference terrainReference)
		{
			if (this == terrainReference)
			{
				return true;
			}

			if (! (terrainReference is TinTerrainReference other))
			{
				return false;
			}

			if (_sources.Count != other._sources.Count)
			{
				return false;
			}

			for (int i = 0; i < _sources.Count; i++)
			{
				if (_sources[i] != other._sources[i])
				{
					return false;
				}

				if (_types[i] != other._types[i])
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCodeCore()
		{
			return _sources[0].GetHashCode();
		}
	}
}
