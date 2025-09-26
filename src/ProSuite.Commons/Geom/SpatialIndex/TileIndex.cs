using System;
using ProSuite.Commons.Misc;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public struct TileIndex
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TileIndex"/> struct.
		/// </summary>
		/// <param name="indexEasting">The easting index.</param>
		/// <param name="indexNorthing">The northing index.</param>
		public TileIndex(int indexEasting, int indexNorthing)
		{
			East = indexEasting;
			North = indexNorthing;
		}

		public int East { get; }

		public int North { get; }

		public override bool Equals(object obj)
		{
			if (! (obj is TileIndex))
			{
				return false;
			}

			var rectangularTileIndex = (TileIndex) obj;
			return East == rectangularTileIndex.East &&
			       North == rectangularTileIndex.North;
		}

		public override int GetHashCode()
		{
			return East + 29 * North;
		}

		public override string ToString()
		{
			return string.Format("East: {0} / North: {1}", East, North);
		}
	}
}
