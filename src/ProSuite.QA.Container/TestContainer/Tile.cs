using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.TestContainer
{
	public class Tile
	{
		private readonly IEnvelope _filterEnvelope;
		private readonly Box _box;
		private readonly ISpatialFilter _filter;

		public Tile(double tileXMin, double tileYMin, double tileXMax, double tileYMax,
		            ISpatialReference spatialReference, int totalTileCount)
		{
			_filterEnvelope = new EnvelopeClass();
			_filterEnvelope.PutCoords(tileXMin, tileYMin, tileXMax, tileYMax);
			_filterEnvelope.SpatialReference = spatialReference;

			_box = new Box(new Pnt2D(tileXMin, tileYMin), new Pnt2D(tileXMax, tileYMax));

			_filter = new SpatialFilterClass();
			_filter.Geometry = _filterEnvelope;
			_filter.SpatialRel = totalTileCount == 1
				                     ? esriSpatialRelEnum.esriSpatialRelIntersects
				                     : esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
		}

		public Box Box => _box;
		public IEnvelope FilterEnvelope => _filterEnvelope;
		public ISpatialFilter SpatialFilter => _filter;

		#region Overrides of Object

		public override string ToString()
		{
			return $"Tile {_box.Min.X} {_box.Min.Y} {_box.Max.X} {_box.Max.Y}";
		}

		#endregion
	}
}