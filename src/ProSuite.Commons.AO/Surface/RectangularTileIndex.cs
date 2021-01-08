namespace ProSuite.Commons.AO.Surface
{
	public readonly struct RectangularTileIndex
	{
		public static RectangularTileIndex Undefined { get; } = new RectangularTileIndex(-1, -1);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="RectangularTileIndex"/> struct.
		/// </summary>
		/// <param name="indexEasting">The easting index.</param>
		/// <param name="indexNorthing">The northing index.</param>
		public RectangularTileIndex(int indexEasting, int indexNorthing)
		{
			East = indexEasting;
			North = indexNorthing;
		}

		#endregion

		public int East { get; }

		public int North { get; }

		public override bool Equals(object obj)
		{
			if (! (obj is RectangularTileIndex))
			{
				return false;
			}

			var rectangularTileIndex = (RectangularTileIndex) obj;
			return East == rectangularTileIndex.East && North == rectangularTileIndex.North;
		}

		public override int GetHashCode()
		{
			return East + 29 * North;
		}

		public override string ToString()
		{
			return $"East: {East} / North: {North}";
		}
	}
}
