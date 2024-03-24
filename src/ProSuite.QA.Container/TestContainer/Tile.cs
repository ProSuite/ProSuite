using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Geom;
using System.Collections.Generic;

namespace ProSuite.QA.Container.TestContainer
{
	public class Tile
	{
		private readonly Box _box;
		private readonly ISpatialReference _spatialReference;
		private readonly int _totalTileCount;


		private IEnvelope _filterEnvelope;
		private IFeatureClassFilter _filter;

		public Tile(double tileXMin, double tileYMin, double tileXMax, double tileYMax,
					ISpatialReference spatialReference, int totalTileCount)
		{
			_box = new Box(new Pnt2D(tileXMin, tileYMin), new Pnt2D(tileXMax, tileYMax));
			_spatialReference = spatialReference;
			_totalTileCount = totalTileCount;
		}

		public Box Box => _box;
		public IEnvelope FilterEnvelope => _filterEnvelope ?? (_filterEnvelope = GetFilterEnvelope());
		public IFeatureClassFilter SpatialFilter => _filter ?? (_filter = GetFilter());

		private IEnvelope GetFilterEnvelope()
		{
			IEnvelope env = new EnvelopeClass();
			env.PutCoords(_box.Min.X, _box.Min.Y, _box.Max.X, _box.Max.Y);
			env.SpatialReference = _spatialReference;
			return env;
		}

		private IFeatureClassFilter GetFilter()
		{
			var rel = _totalTileCount == 1
						  ? esriSpatialRelEnum.esriSpatialRelIntersects
						  : esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			IFeatureClassFilter filter = new AoFeatureClassFilter(FilterEnvelope, rel);

			return filter;
		}

		#region Overrides of Object

		public override string ToString()
		{
			return $"Tile {_box.Min.X} {_box.Min.Y} {_box.Max.X} {_box.Max.Y}";
		}

		#endregion

		public class TileComparer : IEqualityComparer<Tile>
		{
			private readonly Box.BoxComparer _cmp = new Box.BoxComparer();

			public bool Equals(Tile x, Tile y)
			{
				if (x == null || y == null) return false;

				if (x._spatialReference != y._spatialReference)
				{
					return false;
				}

				if (x._totalTileCount != y._totalTileCount)
				{
					return false;
				}

				return _cmp.Equals(x.Box, y.Box);
			}

			public int GetHashCode(Tile obj)
			{
				return _cmp.GetHashCode(obj.Box);
			}
		}

	}
}
