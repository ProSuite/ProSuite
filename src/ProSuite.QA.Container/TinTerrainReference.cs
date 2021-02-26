using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	[CLSCompliant(false)]
	public class TinTerrainReference : TerrainReference
	{
		[NotNull]
		public static TinTerrainReference Create([NotNull] IFeatureDataset fds)
		{
			List<IFeatureClass> masspoints = new List<IFeatureClass>();
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
					masspoints.Add(fc);
				}
			}

			return new TinTerrainReference(fds.Name, masspoints);
		}

		private readonly string _name;
		private readonly IList<IFeatureClass> _massPointFcs;
		private RectangularTilingStructure _tiling;

		private TinTerrainReference([NotNull] string name,
		                            [NotNull] IList<IFeatureClass> massPointFcs)
		{
			_name = name;
			_massPointFcs = new List<IFeatureClass>(massPointFcs);
		}

		public override IGeoDataset Dataset => (IGeoDataset) _massPointFcs[0];

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
			foreach (var featureClass in _massPointFcs)
			{
				IFeatureCursor crs = featureClass.Search(filter, Recycling: false);
				try
				{
					tin.AddFromFeatureCursor(crs, null, null, esriTinSurfaceType.esriTinMassPoint);
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

			if (_massPointFcs.Count != other._massPointFcs.Count)
			{
				return false;
			}

			for (int i = 0; i < _massPointFcs.Count; i++)
			{
				if (_massPointFcs[i] != other._massPointFcs[i])
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCodeCore()
		{
			return _massPointFcs[0].GetHashCode();
		}
	}
}
