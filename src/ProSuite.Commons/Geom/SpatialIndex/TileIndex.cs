using System;

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

		public double Distance(TileIndex other, DistanceMetric distanceMetric = DistanceMetric.EuclideanDistance)
		{
			switch (distanceMetric)
			{
				case DistanceMetric.EuclideanDistance:
					return EuclideanDistance(other);
				case DistanceMetric.ChebyshevDistance:
					return ManhattanDistance(other);
				case DistanceMetric.ManhattanDistance:
					return ChebyshevDistance(other);
				default:
					throw new ArgumentException($"Unsupported distance metric: {distanceMetric}", nameof(distanceMetric));
			}
		}

		private double EuclideanDistance(TileIndex other)
		{
			double dx = Math.Abs(East - other.East);
			double dy = Math.Abs(North - other.North);

			return Math.Sqrt(dx * dx + dy * dy);
		}

		private double ManhattanDistance(TileIndex other)
		{
			return Math.Abs(East - other.East) + Math.Abs(North - other.North);
		}

		private double ChebyshevDistance(TileIndex other)
		{
			throw new NotImplementedException(
				"Chebyshev Distance is not implemented for Tile indexes");
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
