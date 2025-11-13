using System;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public readonly struct TileIndex : IEquatable<TileIndex>
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

		public bool Equals(TileIndex other)
		{
			return East == other.East && North == other.North;
		}

		public override bool Equals(object obj)
		{
			return obj is TileIndex other && Equals(other);
		}

		public override int GetHashCode()
		{
			// TODO: Consider HashCode.Combine(East, North); for better collision avoidance
			// but this requires .NET Standard 2.1 or .NET Core
			return East + 29 * North;
		}

		public override string ToString()
		{
			return string.Format("East: {0} / North: {1}", East, North);
		}
	}
}
